using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Skillforge.Domain;

namespace Skillforge.Service;
/// <summary>
/// Service responsible for generating and managing JSON Web Tokens (JWT) and Refresh Tokens.
/// It handles identity encoding, security signing, and cryptographic token creation.
/// </summary>

public class JWTProviderService : IJWTProviderService
{
    /// <summary>
    /// Generates a signed JWT Access Token containing user-specific claims.
    /// The token includes the user's ID, Email (Subject), Unique Identifier (JTI), and Role 
    /// to facilitate stateless authorization across the API.
    /// </summary>
    /// <param name="user">The User entity containing the data to be encoded into the token.</param>
    /// <returns>A base64-encoded JWT string signed with HMAC-SHA256.</returns>
    /// <exception cref="Exception">Thrown if the secret key is missing or if the token signing process fails.</exception>
    public string GenerateJwtToken(User user)
    {
        try
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentails = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim("id", user.UserID.ToString()),
                new Claim("name", user.Name ?? string.Empty),
                new Claim("role", user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "Skillforge",
                audience: "SkillForgeUsers",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentails
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (System.Exception ex)
        {

            throw new Exception("Error while creating JWT. " + ex.Message);
        }
    }

}
