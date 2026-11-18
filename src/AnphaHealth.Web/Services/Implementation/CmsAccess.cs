using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using AnphaHealth.Web.Services.Models;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class CmsAccess(AuthenticationStateProvider authentication, IAuthorizationService authorization)
{
    public const string Policy = "CmsAdmin";
    public const string Role = "Administrator";

    public async Task RequireAsync()
    {
        var state = await authentication.GetAuthenticationStateAsync();
        if (!(await authorization.AuthorizeAsync(state.User, Policy)).Succeeded)
            throw new CmsException("Bạn cần đăng nhập với quyền quản trị để thực hiện thao tác này.");
    }
}
