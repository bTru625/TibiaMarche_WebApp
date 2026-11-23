using TibiaMarche_WebApp.Models;

namespace TibiaMarche_WebApp.Services
{
    /// <summary>
    /// What UserRepository gives back after a login or a register:
    /// either the User (it worked) or an error message to show on the page.
    /// (In Android this was the AuthCallback with onSuccess / onError.)
    /// </summary>
    public class AuthResult
    {
        public User? User { get; private set; }
        public string? Error { get; private set; }

        public bool Success
        {
            get { return User != null; }
        }

        public static AuthResult Ok(User user)
        {
            return new AuthResult { User = user };
        }

        public static AuthResult Fail(string error)
        {
            return new AuthResult { Error = error };
        }
    }
}