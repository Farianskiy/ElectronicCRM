using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ElectronicService.Core.Abstractions;
using ElectronicService.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ElectronicService.Web.Auth;

public sealed class JwtTokenProvider : IJwtTokenProvider
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenProvider(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Role, user.Type.ToString()),
            new("user_type", user.Type.ToString()),
            new("user_status", user.Status.ToString())
        };

        if (user.Email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email.Value));
        }

        claims.Add(new Claim(
            "created_at_utc",
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)));

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}