using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace FishShop.API.Shared;

public static class TokenGenerator
{
    public static (string jwt, DateTime expires) GenerateJwtToken(IConfigurationSection jwtSettings,
        IList<Claim> claims)
    {
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(1);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.WriteToken(token);
        return (jwt, expires);
    }

    public static string GenerateRefreshToken(ISecureDataFormat<AuthenticationTicket> refreshTokenProtector,
        ClaimsPrincipal principal, TimeProvider timeProvider)
    {
        return refreshTokenProtector.Protect(new AuthenticationTicket(principal,
            new AuthenticationProperties
            {
                ExpiresUtc = timeProvider.GetUtcNow().AddDays(14)
            },
            IdentityConstants.BearerScheme));
    }
}