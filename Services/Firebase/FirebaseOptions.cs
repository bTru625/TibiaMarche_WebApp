namespace TibiaMarche_WebApp.Services.Firebase
{
    /// <summary>
    /// Settings for talking to the shared Firebase project (bound from the "Firebase" config section).
    ///
    /// DatabaseUrl lives in appsettings.json (it is not a secret: the Android app ships it too).
    /// ServiceAccountKeyPath is a SECRET path and must only be set with user-secrets, e.g.
    ///   dotnet user-secrets set "Firebase:ServiceAccountKeyPath" "C:\keys\tibimarche-firebase-adminsdk.json"
    /// Keep the key file outside the repository.
    /// </summary>
    public class FirebaseOptions
    {
        public const string SectionName = "Firebase";

        /// <summary>Realtime Database root, e.g. https://tibimarche-default-rtdb.firebaseio.com</summary>
        public string DatabaseUrl { get; set; } = string.Empty;

        /// <summary>Full path to the service account JSON key (user-secrets only).</summary>
        public string? ServiceAccountKeyPath { get; set; }
    }
}
