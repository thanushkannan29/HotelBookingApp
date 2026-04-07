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
            new Claim("nameid", userId.ToString()),
            new Claim("role",   role),
            new Claim("unique_name", "TestUser")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
    }
}
