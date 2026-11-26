using System.Net.Http.Json;
using System.Text.Json;
using TibiaMarche_WebApp.Models;

namespace TibiaMarche_WebApp.Services
{
    
    public class UserRepository
    {
        private const string AuthBaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _databaseUrl;

        public UserRepository(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Firebase:ApiKey"]
                ?? throw new InvalidOperationException("Firebase:ApiKey is missing in appsettings.json");
            _databaseUrl = (config["Firebase:DatabaseUrl"]
                ?? throw new InvalidOperationException("Firebase:DatabaseUrl is missing in appsettings.json"))
                .TrimEnd('/');
        }

        // ---------------------------------------------------------------- public methods

        /// <summary>Checks email + password with Firebase, then reads users/{uid} to know the role.</summary>
        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            try
            {
                AuthResponse auth = await CallAuthAsync("signInWithPassword", new
                {
                    email = User.NormalizeEmail(email),
                    password = password,
                    returnSecureToken = true
                });
                return await LoadProfileAsync(auth.LocalId, auth.IdToken);
            }
            catch (FirebaseException e)
            {
                return AuthResult.Fail(e.Message);
            }
            catch (HttpRequestException)
            {
                return AuthResult.Fail("No internet connection. Try again.");
            }
        }

        //Creates the account in Firebase Auth, then saves users/{uid} with role BUYER.
        public async Task<AuthResult> RegisterBuyerAsync(string email, string password)
        {
            string cleanEmail = User.NormalizeEmail(email);
            try
            {
                AuthResponse auth = await CallAuthAsync("signUp", new
                {
                    email = cleanEmail,
                    password = password,
                    returnSecureToken = true
                });

                // Same 3 fields as the Android app writes
                var profile = new Dictionary<string, object>
                {
                    ["email"] = cleanEmail,
                    ["role"] = Roles.Buyer,
                    ["createdAt"] = new Dictionary<string, string> { [".sv"] = "timestamp" }  // = ServerValue.TIMESTAMP
                };

                HttpResponseMessage response = await _http.PutAsJsonAsync(UserUrl(auth.LocalId, auth.IdToken), profile);
                if (!response.IsSuccessStatusCode)
                {
                    string code = await ReadErrorCodeAsync(response);
                    return AuthResult.Fail("Your account was created but the profile could not be saved: " + MessageFor(code));
                }

                return AuthResult.Ok(new Buyer(auth.LocalId, cleanEmail));
            }
            catch (FirebaseException e)
            {
                return AuthResult.Fail(e.Message);
            }
            catch (HttpRequestException)
            {
                return AuthResult.Fail("No internet connection. Try again.");
            }
        }

        //  Firebase sends the reset email. Returns null if OK, or an error message.
        public async Task<string?> SendPasswordResetAsync(string email)
        {
            try
            {
                await CallAuthAsync("sendOobCode", new
                {
                    requestType = "PASSWORD_RESET",
                    email = User.NormalizeEmail(email)
                });
                return null;
            }
            catch (FirebaseException e)
            {
                // Don't tell strangers which emails have an account: "not found" counts as OK.
                if (e.Code.StartsWith("EMAIL_NOT_FOUND"))
                {
                    return null;
                }
                return e.Message;
            }
            catch (HttpRequestException)
            {
                return "No internet connection. Try again.";
            }
        }

        // ---------------------------------------------------------------- private helpers

        /// <summary>Reads users/{uid} and builds an Admin or a Buyer.</summary>
        private async Task<AuthResult> LoadProfileAsync(string uid, string idToken)
        {
            HttpResponseMessage response = await _http.GetAsync(UserUrl(uid, idToken));
            if (!response.IsSuccessStatusCode)
            {
                string code = await ReadErrorCodeAsync(response);
                return AuthResult.Fail("Could not read your profile: " + MessageFor(code));
            }

            string json = await response.Content.ReadAsStringAsync();
            UserProfile? profile = JsonSerializer.Deserialize<UserProfile>(json, JsonOptions);  // "null" -> null
            if (profile == null)
            {
                return AuthResult.Fail("This account has no profile. Contact the administrator.");
            }

            User user;
            if (string.Equals(profile.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                user = new Admin(uid, profile.Email);
            }
            else
            {
                user = new Buyer(uid, profile.Email);
            }
            return AuthResult.Ok(user);
        }

        /// <summary>POST to Firebase Auth (signInWithPassword, signUp, sendOobCode).</summary>
        private async Task<AuthResponse> CallAuthAsync(string action, object body)
        {
            HttpResponseMessage response = await _http.PostAsJsonAsync($"{AuthBaseUrl}{action}?key={_apiKey}", body);
            if (!response.IsSuccessStatusCode)
            {
                throw new FirebaseException(await ReadErrorCodeAsync(response));
            }
            return await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions)
                ?? throw new FirebaseException("EMPTY_RESPONSE");
        }

        /// <summary>https://...firebaseio.com/users/{uid}.json?auth={idToken}</summary>
        private string UserUrl(string uid, string idToken)
        {
            return $"{_databaseUrl}/users/{uid}.json?auth={Uri.EscapeDataString(idToken)}";
        }

        /// <summary>
        /// Firebase errors look like this:
        ///   Auth:     { "error": { "code": 400, "message": "EMAIL_EXISTS" } }
        ///   Database: { "error": "Permission denied" }
        /// This returns the text part ("EMAIL_EXISTS", "Permission denied"...).
        /// </summary>
        private static async Task<string> ReadErrorCodeAsync(HttpResponseMessage response)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                JsonElement error = doc.RootElement.GetProperty("error");
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString() ?? "";
                }
                return error.GetProperty("message").GetString() ?? "";
            }
            catch (Exception)
            {
                return $"HTTP {(int)response.StatusCode}";
            }
        }

        /// <summary>Turns a Firebase error code into a message for the user (like messageFor in Android).</summary>
        private static string MessageFor(string code)
        {
            if (code.StartsWith("EMAIL_EXISTS"))
                return "An account with this email already exists";
            if (code.StartsWith("INVALID_LOGIN_CREDENTIALS") || code.StartsWith("EMAIL_NOT_FOUND")
                || code.StartsWith("INVALID_PASSWORD"))
                return "Wrong email or password";
            if (code.StartsWith("WEAK_PASSWORD"))
                return "Password must have at least 6 characters";
            if (code.StartsWith("INVALID_EMAIL"))
                return "This is not a valid email";
            if (code.StartsWith("USER_DISABLED"))
                return "This account was disabled. Contact the administrator.";
            if (code.StartsWith("TOO_MANY_ATTEMPTS_TRY_LATER"))
                return "Too many attempts. Wait a few minutes and try again.";
            if (code.StartsWith("CONFIGURATION_NOT_FOUND") || code.StartsWith("OPERATION_NOT_ALLOWED"))
                return "Email/Password login is not enabled in Firebase Authentication.";
            if (code.StartsWith("API key not valid"))
                return "The Firebase ApiKey in appsettings.json is wrong.";
            if (code.StartsWith("Permission denied"))
                return "The database refused it (check the Realtime Database rules).";
            return "Something went wrong: " + code;
        }

        // ---------------------------------------------------------------- small private classes

        /// <summary>The parts of Firebase Auth's answer that we use.</summary>
        private class AuthResponse
        {
            public string LocalId { get; set; } = "";   // the uid
            public string IdToken { get; set; } = "";   // proves who we are to the Realtime Database
        }

        /// <summary>users/{uid} in the Realtime Database (createdAt is ignored here).</summary>
        private class UserProfile
        {
            public string Email { get; set; } = "";
            public string Role { get; set; } = "";
        }

        /// <summary>A Firebase error: Code = what Firebase said, Message = what we show the user.</summary>
        private class FirebaseException : Exception
        {
            public string Code { get; }

            public FirebaseException(string code) : base(MessageFor(code))
            {
                Code = code;
            }
        }
    }
}