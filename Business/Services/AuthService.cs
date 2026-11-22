using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Core.Abstracts.IServices;
using Core.Concretes.DTOs.Auth;
using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Utilities._585.Data.UnitOfWorks;
using Utilities._585.Models;

namespace Business.Services;

public class AuthService(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
    : IAuthService
{
    private TokenResponseDto generateTokens(User user)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey doesn't defined!");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        IEnumerable<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName}  {user.LastName}"),
            new(ClaimTypes.Role, user.Role.ToString()),
        ];
        var expMinutes = int.TryParse(jwtSettings["AccessTokenExpirationMinutes"], out var mins)
            ? mins
            : 60;
        var expiration = DateTime.UtcNow.AddMinutes(expMinutes);

        var token = new JwtSecurityToken(
            jwtSettings["Issuer"],
            jwtSettings["Audience"],
            claims,
            expires: expiration,
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenResponseDto(accessToken, refreshToken, expiration);
    }

    private ClaimsPrincipal getPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey doesn't defined!");
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = false,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(
            token,
            tokenValidationParameters,
            out var securityToken
        );
        if (
            securityToken is not JwtSecurityToken jwtSecurityToken
            || !jwtSecurityToken.Header.Alg.Equals(
                SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase
            )
        )
        {
            throw new InvalidOperationException("Unidentified Token Algorithm!");
        }

        return principal;
    }

    public async Task<Reply<TokenResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var repo = unitOfWork.GetRepository<User>();
        if (!await repo.AnyAsync(x => x.Email.Equals(dto.Email)))
        {
            var user = mapper.Map<User>(dto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var tokenResponse = generateTokens(user);
            user.RefreshToken = tokenResponse.RefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await repo.CreateAsync(user);
            var reply = await unitOfWork.CommitAsync();

            if (reply.IsSuccess)
            {
                return Reply<TokenResponseDto>.Success(tokenResponse);
            }

            return Reply<TokenResponseDto>.Fail(reply.Errors!);
        }

        return Reply<TokenResponseDto>.Fail("This  email address already registered!");
    }

    public async Task<Reply<TokenResponseDto>> LoginAsync(LoginDto dto)
    {
        var repo = unitOfWork.GetRepository<User>();
        var user = await repo.GetAll(true).FirstOrDefaultAsync(x => x.Email.Equals(dto.Email));
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Reply<TokenResponseDto>.Fail("E-mail or Password incorrect!");
        }

        if (!user.Active)
        {
            return Reply<TokenResponseDto>.Fail("Your account disabled!");
        }

        var tokenResponse = generateTokens(user);
        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        repo.Update(user);
        var reply = await unitOfWork.CommitAsync();

        if (reply.IsSuccess)
        {
            return Reply<TokenResponseDto>.Success(tokenResponse);
        }

        return Reply<TokenResponseDto>.Fail(reply.Errors!);
    }

    public async Task<Reply<TokenResponseDto>> RefreshTokenAsync(RefreshTokenDto dto)
    {
        var principal = getPrincipalFromExpiredToken(dto.AccessToken);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Reply<TokenResponseDto>.Fail("Unauthorized Access, Token not valid!");
        }

        var repo = unitOfWork.GetRepository<User>();
        var user = await repo.GetAll(true).FirstOrDefaultAsync(x => x.Id == userId);

        if (
            user == null
            || !user.Active
            || user.RefreshToken != dto.RefreshToken
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow
        )
        {
            return Reply<TokenResponseDto>.Fail("Expired or not valid Refresh Token!");
        }

        var tokenResponse = generateTokens(user);
        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        repo.Update(user);
        var reply = await unitOfWork.CommitAsync();

        if (reply.IsSuccess)
        {
            return Reply<TokenResponseDto>.Success(tokenResponse);
        }

        return Reply<TokenResponseDto>.Fail(reply.Errors!);
    }

    public async Task<Reply> RevokeTokenAsync(Guid userId)
    {
        var repo = unitOfWork.GetRepository<User>();
        var user = await repo.GetAll(true).FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
        {
            return Reply.Fail("User not found!");
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        repo.Update(user);
        var reply = await unitOfWork.CommitAsync();

        if (reply.IsSuccess)
        {
            return Reply.Success();
        }

        return Reply.Fail(reply.Errors!);
    }
}
