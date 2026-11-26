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
