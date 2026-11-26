using Microsoft.Extensions.Options;
using TibiaMarche_WebApp.Services.Auth;
using TibiaMarche_WebApp.Services.Data;
using TibiaMarche_WebApp.Services.ExchangeRates;
using TibiaMarche_WebApp.Services.Firebase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ===== Firebase Realtime Database (REST) =====
// DatabaseUrl comes from appsettings.json; ServiceAccountKeyPath from user-secrets only.
builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection(FirebaseOptions.SectionName));
builder.Services.AddHttpClient(RealtimeDatabaseClient.HttpClientName);
builder.Services.AddSingleton<RealtimeDatabaseClient>();

// ===== Exchange rates (Frankfurter -> Realtime Database exchangeRates/latest) =====
builder.Services.Configure<ExchangeRateOptions>(builder.Configuration.GetSection(ExchangeRateOptions.SectionName));
builder.Services.AddHttpClient(FrankfurterClient.HttpClientName, (services, http) =>
{
    ExchangeRateOptions options = services.GetRequiredService<IOptions<ExchangeRateOptions>>().Value;
    http.BaseAddress = new Uri(options.FrankfurterBaseUrl.TrimEnd('/') + "/");
    http.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<FrankfurterClient>();
builder.Services.AddSingleton<ExchangeRateService>();
builder.Services.AddHostedService<ExchangeRateRefreshService>();

// ===== Admin items, requests, account (B's cards) =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<ItemRepository>();
builder.Services.AddSingleton<RequestRepository>();
builder.Services.AddSingleton<SettingsRepository>();
builder.Services.AddSingleton<IUserProfileService, UserProfileService>();
// TODO(B - S5 "Swap Account stubs..."): register the real Firebase Auth service instead of the stub.
builder.Services.AddSingleton<IFirebaseAuthService, StubFirebaseAuthService>();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
