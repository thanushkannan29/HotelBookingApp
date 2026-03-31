using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingAppWebApi.Tests.Controllers;

internal static class ControllerTestHelper
{
    internal static ControllerContext BuildControllerContext(Guid userId, string role = "Admin")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "TestUser")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
    }
}
