using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace PaymentExecutionService.UnitTests;

public static class TestUtilities
{
    public static string GenerateBearerTokenWithClaims(List<Claim>? claims)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "fake-identity-provider"
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var stringToken = tokenHandler.WriteToken(token);
        var bearerTokenString = new AuthenticationHeaderValue("Bearer", stringToken).ToString();

        return bearerTokenString;
    }
}
