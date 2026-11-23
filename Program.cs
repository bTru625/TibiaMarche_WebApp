using Microsoft.AspNetCore.Authentication.Cookies;
using TibiaMarche_WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Login cookie. After Firebase says the email + password are good, we save the user's
// uid, email and role in an encrypted cookie. Then, on every page:
//   User.Identity.IsAuthenticated  -> is someone logged in?
//   User.IsInRole(Roles.Admin)     -> is it the admin?
//   [Authorize(Roles = Roles.Admin)] on a controller -> only admins can open it
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";   // not logged in -> login page (it adds ?ReturnUrl=...)
        options.AccessDeniedPath = "/";         // logged in but not allowed (a buyer on an admin page) -> home
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;       // the 8 hours restart while the user is active
        options.Cookie.HttpOnly = true;         // JavaScript cannot read the cookie
    });

// UserRepository talks to Firebase (Auth + Realtime Database) with HttpClient (REST API).
builder.Services.AddHttpClient<UserRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();   // reads the login cookie (must be BEFORE UseAuthorization)
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();