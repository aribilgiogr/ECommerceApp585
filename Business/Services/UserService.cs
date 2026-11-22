using AutoMapper;
using Core.Abstracts.IServices;
using Core.Concretes.DTOs.Auth;
using Core.Concretes.Entities;
using Core.Concretes.Enums;
using Microsoft.EntityFrameworkCore;
using Utilities._585.Data.UnitOfWorks;
using Utilities._585.Models;

namespace Business.Services;

public class UserService(IUnitOfWork unitOfWork, IMapper mapper) : IUserService
{
    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var repo = unitOfWork.GetRepository<User>();
        var user = await repo.GetByKeyAsync(userId);

        return user == null ? null : mapper.Map<UserProfileDto>(user);
    }

    public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
    {
        var repo = unitOfWork.GetRepository<User>();
        return mapper.Map<IEnumerable<UserProfileDto>>(await repo.GetAll().ToListAsync());
    }

    public async Task<Reply> UpdateUserRoleAsync(Guid userId, UserRole newRole)
    {
        var repo = unitOfWork.GetRepository<User>();
        var user = await repo.GetByKeyAsync(userId);

        if (user == null)
        {
            return Reply.Fail("User not found!");
        }

        user.Role = newRole;

        repo.Update(user);
        var reply = await unitOfWork.CommitAsync();

        return reply.IsSuccess ? Reply.Success() : Reply.Fail(reply.Errors!);
    }

    public async Task<Reply> DeactivateUserAsync(Guid userId)
    {
        var repo = unitOfWork.GetRepository<User>();
        var user = await repo.GetByKeyAsync(userId);

        if (user == null)
        {
            return Reply.Fail("User not found!");
        }

        user.Active = false;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        repo.Update(user);
        var reply = await unitOfWork.CommitAsync();

        return reply.IsSuccess ? Reply.Success() : Reply.Fail(reply.Errors!);
    }
}