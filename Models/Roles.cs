namespace TibiaMarche_WebApp.Models
{
    /// <summary>
    /// Role values stored in users/{uid}.role and put in the auth cookie as a
    /// ClaimTypes.Role claim. Lowercase to match J's Firebase data model.
    /// Always use these constants: IsInRole is case-sensitive, so "Admin"
    /// would never match "admin".
    /// </summary>
    public static class Roles
    {
        public const string Admin = "admin";
        public const string Buyer = "buyer";
    }
}
