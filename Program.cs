using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InfoPoint.Data;
using InfoPoint.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for development
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5167");
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5167); // HTTP only in development
    });
}

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add separate DbContext for timetable data (SQL Server)
builder.Services.AddDbContext<InfoPoint.Data.TimetableDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TimetableConnection"), 
        sqlOptions => sqlOptions.CommandTimeout(300))); // 5 minute timeout

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure cookie policy before authentication
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.Name = ".AspNetCore.InfoPoint";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in dev
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["GoogleAuthentication:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["GoogleAuthentication:ClientSecret"] ?? "";
        options.CallbackPath = "/signin-google";
        
        // Request additional scopes
        options.Scope.Add("profile");
        options.Scope.Add("email");
        
        // Save tokens for further API calls if needed
        options.SaveTokens = true;
        
        // Configure correlation cookie for HTTP development
        options.CorrelationCookie.Name = ".AspNetCore.Correlation.Google";
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP
        options.CorrelationCookie.IsEssential = true;
    });

builder.Services.AddControllersWithViews();

// Add custom services
builder.Services.AddScoped<InfoPoint.Services.StaffSyncService>();

// Add session for state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".InfoPoint.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP
});

// Add data protection for cookie encryption
builder.Services.AddDataProtection();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Temporarily disable HTTPS redirection for debugging
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

// Add cookie policy middleware
app.UseCookiePolicy();

// Add session middleware
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Configure Areas routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
