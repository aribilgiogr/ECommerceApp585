using Core.Concretes.DTOs.Auth;
using Utilities._585.Models;

namespace Core.Abstracts.IServices;

public interface IAuthService
{
    Task<Reply<TokenResponseDto>> RegisterAsync(RegisterDto dto);
    Task<Reply<TokenResponseDto>> LoginAsync(LoginDto dto);
    Task<Reply<TokenResponseDto>> RefreshTokenAsync(RefreshTokenDto dto);
    Task<Reply> RevokeTokenAsync(Guid userId);
}