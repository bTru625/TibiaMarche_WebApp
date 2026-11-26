namespace TibiaMarche_WebApp.Services.Auth
{
    /// <summary>Result of an account change. Message is shown to the user as is, so keep it friendly.</summary>
    public record AuthResult(bool Success, string Message, bool RequiresEmailVerification = false);

    /// <summary>
    /// Password and email changes for the signed-in user (Account page).
    ///
    /// TODO(B - S5 "Swap Account stubs for real Firebase and currency services"): replace
    /// <see cref="StubFirebaseAuthService"/> with the real Firebase Auth REST implementation:
    /// sign in again with the old password (signInWithPassword), then accounts:update; for the email,
    /// fall back to accounts:sendOobCode VERIFY_AND_CHANGE_EMAIL. Reuse J's S2 sign-in code.
    /// Only Program.cs registration and the stub class need to change; the page talks to this interface.
    /// </summary>
    public interface IFirebaseAuthService
    {
        Task<AuthResult> ChangePasswordAsync(string email, string oldPassword, string newPassword);
        Task<AuthResult> ChangeEmailAsync(string email, string password, string newEmail);
    }

    /// <summary>
    /// STUB. Does NOT talk to Firebase: nothing is changed anywhere. It only checks the input and then
    /// pretends it worked, and ONLY in the Development environment; elsewhere it refuses, so a stub can
    /// never silently "succeed" in a real deployment. It accepts any current password.
    /// TODO(B - S5): delete this class.
    /// </summary>
    public class StubFirebaseAuthService : IFirebaseAuthService
    {
        private const string NotAvailable = "Changing account details is not available yet.";
        private readonly IWebHostEnvironment _environment;

        public StubFirebaseAuthService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public Task<AuthResult> ChangePasswordAsync(string email, string oldPassword, string newPassword)
        {
            if (!_environment.IsDevelopment())
            {
                return Task.FromResult(new AuthResult(false, NotAvailable));
            }
            if (newPassword.Length < 6)
            {
                return Task.FromResult(new AuthResult(false, "The new password must be at least 6 characters."));
            }
            return Task.FromResult(new AuthResult(true, "Password changed. (Stub: nothing was actually changed.)"));
        }

        public Task<AuthResult> ChangeEmailAsync(string email, string password, string newEmail)
        {
            if (!_environment.IsDevelopment())
            {
                return Task.FromResult(new AuthResult(false, NotAvailable));
            }
            return Task.FromResult(new AuthResult(true,
                "We sent a confirmation link to the new address. Your email changes after you confirm it. (Stub: nothing was sent.)",
                RequiresEmailVerification: true));
        }
    }
}
