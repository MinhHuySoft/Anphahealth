using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Data.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Security;

internal sealed class CmsCredentials(IRepository<AdminUser> users, IPasswordHasher<AdminUser> hasher)
{
    public async Task<AdminUser?> VerifyAsync(string userName, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName) || userName.Length > 80 || password.Length is 0 or > 256)
            return null;

        var user = await users.ReadAsync((rows, token) =>
            rows.SingleOrDefaultAsync(row => row.UserName == userName.Trim() && row.IsActive, token),
            cancellationToken);
        if (user is null)
            return null;
        return hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed
            ? null : user;
    }

    public async Task CreateAsync(string userName, string password)
    {
        userName = userName.Trim();
        if (userName.Length is 0 or > 80 || userName.Any(char.IsControl) || password.Length is < 12 or > 256)
            throw new InvalidOperationException("Tên tài khoản tối đa 80 ký tự. Mật khẩu cần từ 12 đến 256 ký tự.");

        await users.WriteAsync(async (_, rows, token) =>
        {
            if (await rows.AnyAsync(user => user.UserName == userName, token))
                throw new InvalidOperationException("Tài khoản này đã tồn tại.");

            var user = new AdminUser { UserName = userName, IsActive = true };
            user.PasswordHash = hasher.HashPassword(user, password);
            rows.Add(user);
            return true;
        });
    }

    public static async Task CreateFromConsoleAsync(WebApplication app, string userName)
    {
        string? password;
        if (Console.IsInputRedirected)
            password = await Console.In.ReadLineAsync();
        else
        {
            Console.Write("Mật khẩu CMS (tối thiểu 12 ký tự): ");
            var chars = new List<char>();
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                if (key.Key == ConsoleKey.Backspace && chars.Count > 0)
                    chars.RemoveAt(chars.Count - 1);
                else if (!char.IsControl(key.KeyChar) && chars.Count < 256)
                    chars.Add(key.KeyChar);
            }
            Console.WriteLine();
            password = new string(chars.ToArray());
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CmsCredentials>().CreateAsync(userName, password ?? "");
        Console.WriteLine("Đã tạo tài khoản quản trị.");
    }
}
