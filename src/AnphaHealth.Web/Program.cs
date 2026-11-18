using System.Threading.RateLimiting;
using AnphaHealth.Web.Components;
using AnphaHealth.Web.Data;
using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Data.Repository;
using AnphaHealth.Web.Security;
using AnphaHealth.Web.Services.Contracts;
using AnphaHealth.Web.Services.Implementation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var createAdmin = args.Length == 2 && args[0] == "--create-admin";
var builder = WebApplication.CreateBuilder(createAdmin ? [] : args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.ReturnUrlParameter = "returnUrl";
        options.Cookie.Name = "AnphaHealth.Cms";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(CmsAccess.Policy, policy => policy.RequireAuthenticatedUser().RequireRole(CmsAccess.Role));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("cms-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
});

var connection = builder.Configuration.GetConnectionString("AnphaHealth")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:AnphaHealth before starting the app.");
builder.Services.AddDbContextFactory<AnphaHealthDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddSingleton<StaticMediaLibrary>();
builder.Services.AddScoped<CmsAccess>();
builder.Services.AddScoped<CmsCredentials>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddScoped<IPublicWebsiteService, PublicWebsiteService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IInquiryService, InquiryService>();

var app = builder.Build();
if (createAdmin)
{
    await CmsCredentials.CreateFromConsoleAsync(app, args[1]);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

var uploadRoot = MediaService.UploadDirectory(app.Environment, app.Configuration);
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = "/media/uploads",
    OnPrepareResponse = context =>
    {
        var headers = context.Context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        if (Path.GetExtension(context.File.Name).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            headers["Content-Disposition"] = "attachment";
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapCmsSession();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();
