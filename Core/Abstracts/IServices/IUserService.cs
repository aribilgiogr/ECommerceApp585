using Core.Concretes.DTOs.Auth;
using Core.Concretes.Enums;
using Utilities._585.Models;

namespace Core.Abstracts.IServices;

public interface IUserService
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId);
    Task<IEnumerable<UserProfileDto>> GetAllUsersAsync();
    Task<Reply> UpdateUserRoleAsync(Guid userId, UserRole newRole);
    Task<Reply> DeactivateUserAsync(Guid userId);
}