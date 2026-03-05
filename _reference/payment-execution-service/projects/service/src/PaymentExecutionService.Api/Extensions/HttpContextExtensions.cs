using System.IdentityModel.Tokens.Jwt;

namespace PaymentExecutionService.Extensions;

public static class HttpContextExtensions
{
    public static string GetClientIdFromJwtToken(this HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var clientId = jwtToken.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value;

            if (string.IsNullOrEmpty(clientId))
            {
                return string.Empty;
            }

            return clientId;
        }
        catch
        {
            return string.Empty;
        }
    }
}
