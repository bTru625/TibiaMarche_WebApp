using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace TibiaMarche_WebApp.Services.Firebase
{
    /// <summary>
    /// Minimal Realtime Database client over the official REST API.
    ///
    /// The Firebase Admin SDK for .NET has no Realtime Database support, so this class sends
    /// plain HTTP requests to {DatabaseUrl}/{path}.json, signed with an OAuth access token
    /// from the service account. Requests made this way have admin rights and are NOT limited
    /// by the database security rules, so only server code may use this class.
    ///
    /// Reusable for later features (items, orders, requests): GetAsync / PutAsync any path.
    /// Registered as a singleton so the access token is cached for the whole app; each call
    /// takes a fresh HttpClient from IHttpClientFactory.
    /// </summary>
    public class RealtimeDatabaseClient
    {
        /// <summary>Name of the HttpClient registered in Program.cs.</summary>
        public const string HttpClientName = "RealtimeDatabase";

        // Scopes required by the Realtime Database REST API for service-account tokens.
        private static readonly string[] Scopes =
        {
            "https://www.googleapis.com/auth/firebase.database",
            "https://www.googleapis.com/auth/userinfo.email"
        };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FirebaseOptions _options;
        private readonly Lazy<ITokenAccess?> _credential;

        public RealtimeDatabaseClient(IHttpClientFactory httpClientFactory, IOptions<FirebaseOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _credential = new Lazy<ITokenAccess?>(LoadCredential);
        }

        /// <summary>
        /// True when both the database URL and an existing service account key file are configured.
        /// When false, the app still runs; features that need the database report it instead.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_options.DatabaseUrl)
            && !string.IsNullOrWhiteSpace(_options.ServiceAccountKeyPath)
            && File.Exists(_options.ServiceAccountKeyPath);

        /// <summary>Reads the value at <paramref name="path"/>. Returns default when nothing is stored there.</summary>
        public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Get, path, cancellationToken);
            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, path, cancellationToken);

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            // The REST API answers the literal "null" when the path is empty.
            if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null")
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }

        /// <summary>Writes (replaces) the value at <paramref name="path"/>.</summary>
        public async Task PutAsync<T>(string path, T value, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Put, path, cancellationToken);
            string json = JsonSerializer.Serialize(value, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, path, cancellationToken);
        }

        /// <summary>Merges the given fields into the node at <paramref name="path"/> (HTTP PATCH). Other fields are kept.</summary>
        public async Task PatchAsync<T>(string path, T fields, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Patch, path, cancellationToken);
            request.Content = new StringContent(JsonSerializer.Serialize(fields, JsonOptions), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, path, cancellationToken);
        }

        /// <summary>Adds a new child with a generated key under <paramref name="path"/> (HTTP POST) and returns that key.</summary>
        public async Task<string> PushAsync<T>(string path, T value, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Post, path, cancellationToken);
            request.Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, path, cancellationToken);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return doc.RootElement.GetProperty("name").GetString() ?? string.Empty;
        }

        /// <summary>Removes the node at <paramref name="path"/>.</summary>
        public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Delete, path, cancellationToken);
            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, path, cancellationToken);
        }

        // ===== Conditional writes (the "transaction" for the one-of-a-kind rule) =====
        // The REST API has no transactions. Instead: read the value together with its ETag, decide, then
        // write with an If-Match header. If anyone changed the node in between, the server answers
        // 412 Precondition Failed and the write is refused; the caller re-reads and retries.

        /// <summary>Reads the value at <paramref name="path"/> together with its ETag (needed for the conditional writes).</summary>
        public async Task<(T? Value, string? ETag)> GetWithETagAsync<T>(string path, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Get, path, cancellationToken);
            request.Headers.TryAddWithoutValidation("X-Firebase-ETag", "true");
            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, path, cancellationToken);

            string? etag = response.Headers.NonValidated.TryGetValues("ETag", out var values) ? values.ToString() : null;
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            T? value = string.IsNullOrWhiteSpace(body) || body.Trim() == "null"
                ? default
                : JsonSerializer.Deserialize<T>(body, JsonOptions);
            return (value, etag);
        }

        /// <summary>Writes the value only if the node still has <paramref name="etag"/>. Returns false when it changed meanwhile.</summary>
        public async Task<bool> PutIfMatchAsync<T>(string path, T value, string etag, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Put, path, cancellationToken);
            request.Headers.TryAddWithoutValidation("if-match", etag);
            request.Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                return false;
            }
            await EnsureSuccessAsync(response, path, cancellationToken);
            return true;
        }

        /// <summary>Deletes the node only if it still has <paramref name="etag"/>. Returns false when it changed meanwhile.</summary>
        public async Task<bool> DeleteIfMatchAsync(string path, string etag, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = await CreateRequestAsync(HttpMethod.Delete, path, cancellationToken);
            request.Headers.TryAddWithoutValidation("if-match", etag);

            using HttpResponseMessage response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                return false;
            }
            await EnsureSuccessAsync(response, path, cancellationToken);
            return true;
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, CancellationToken cancellationToken)
        {
            ITokenAccess credential = _credential.Value
                ?? throw new InvalidOperationException(
                    "Realtime Database is not configured. Set Firebase:DatabaseUrl in appsettings.json and " +
                    "Firebase:ServiceAccountKeyPath with dotnet user-secrets (path to an existing key file).");

            // The library caches the token and refreshes it shortly before it expires.
            string token = await credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);

            string url = $"{_options.DatabaseUrl.TrimEnd('/')}/{path.Trim('/')}.json";
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        private ITokenAccess? LoadCredential()
        {
            if (!IsConfigured)
            {
                return null;
            }
            ServiceAccountCredential serviceAccount =
                CredentialFactory.FromFile<ServiceAccountCredential>(_options.ServiceAccountKeyPath!);
            return GoogleCredential.FromServiceAccountCredential(serviceAccount).CreateScoped(Scopes);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string path, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Realtime Database {response.RequestMessage?.Method} '{path}' failed with " +
                $"{(int)response.StatusCode} {response.StatusCode}: {body}",
                null,
                response.StatusCode);
        }
    }
}
