using System.Security.Claims;
using System.Text.Encodings.Web;
using AnphaHealth.Web.Services.Implementation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AnphaHealth.Web.Security;

internal static class CmsEndpoints
{
    public static void MapCmsSession(this WebApplication app)
    {
        app.MapGet("/admin/login", LoginPage).AllowAnonymous();
        app.MapPost("/admin/login", SignInAsync).AllowAnonymous().RequireRateLimiting("cms-login");
        app.MapPost("/admin/logout", SignOutAsync).RequireAuthorization(CmsAccess.Policy);
    }

    private static IResult LoginPage(HttpContext context, IAntiforgery antiforgery)
    {
        if (context.User.IsInRole(CmsAccess.Role))
            return Results.Redirect("/admin");

        var token = antiforgery.GetAndStoreTokens(context);
        var encode = HtmlEncoder.Default;
        var next = encode.Encode(LocalAdminPath(context.Request.Query["returnUrl"].ToString()));
        var error = context.Request.Query.ContainsKey("error")
            ? "<p class=\"login-error\" role=\"alert\">Tên đăng nhập hoặc mật khẩu chưa đúng.</p>" : "";
        return Results.Content($$"""
            <!doctype html>
            <html lang="vi">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="robots" content="noindex, nofollow">
                <title>Đăng nhập quản trị · Anpha Health</title>
                <link rel="icon" type="image/png" href="/media/29276710c566-logo-120px.png">
                <link rel="stylesheet" href="/fonts/fonts.css">
                <link rel="stylesheet" href="/cms-login.css">
            </head>
            <body>
                <main class="login-shell">
                    <section class="login-cover" aria-label="Anpha Health">
                        <a href="/" class="cover-brand"><img src="/media/29276710c566-logo-120px.png" alt="" width="43" height="43"> Anpha Health</a>
                        <div class="cover-copy"><h2>Quản trị website Anpha Health</h2><p>Cập nhật nội dung, sản phẩm và hình ảnh ở một nơi.</p></div>
                        <img class="cover-image" src="/media/826f6f4bd5bd-bigstock-Medicine-Doctor-Hand-Working-W-44541469_opt.jpg" alt="">
                    </section>
                    <section class="login-panel">
                        <div class="login-form">
                            <h1>Đăng nhập</h1>
                            <p>Sử dụng tài khoản quản trị để tiếp tục.</p>
                            {{error}}
                            <form method="post" action="/admin/login">
                                <input type="hidden" name="{{encode.Encode(token.FormFieldName)}}" value="{{encode.Encode(token.RequestToken!)}}">
                                <input type="hidden" name="returnUrl" value="{{next}}">
                                <label for="userName">Tên đăng nhập</label>
                                <input id="userName" name="userName" autocomplete="username" required maxlength="80" autofocus>
                                <label for="password">Mật khẩu</label>
                                <input type="password" id="password" name="password" autocomplete="current-password" required maxlength="256">
                                <button type="submit">Đăng nhập <span aria-hidden="true">→</span></button>
                            </form>
                            <a class="public-link" href="/">← Về website</a>
                        </div>
                    </section>
                </main>
            </body>
            </html>
            """, "text/html; charset=utf-8");
    }

    private static async Task<IResult> SignInAsync(HttpContext context, IAntiforgery antiforgery,
        CmsCredentials credentials, CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var next = LocalAdminPath(form["returnUrl"].ToString());
        var user = await credentials.VerifyAsync(form["userName"].ToString(), form["password"].ToString(), cancellationToken);
        if (user is null)
            return Results.Redirect("/admin/login?error=1&returnUrl=" + Uri.EscapeDataString(next));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, CmsAccess.Role)
        };
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Results.LocalRedirect(next);
    }

    private static async Task<IResult> SignOutAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/admin/login");
    }

    private static string LocalAdminPath(string value) =>
        (value == "/admin" || value.StartsWith("/admin/", StringComparison.Ordinal))
        && !value.Contains('\\') && !value.Any(char.IsControl)
        && !value.StartsWith("/admin/login", StringComparison.OrdinalIgnoreCase) ? value : "/admin";
}
