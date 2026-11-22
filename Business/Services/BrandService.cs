using AutoMapper;
using Core.Abstracts.IServices;
using Core.Concretes.DTOs.Brand;
using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;
using Utilities._585.Data.UnitOfWorks;
using Utilities._585.Models;

namespace Business.Services;

public class BrandService(IUnitOfWork unitOfWork, IMapper mapper) : IBrandService
{
    public async Task<IEnumerable<BrandResponseDto>> GetAllAsync()
    {
        var repo = unitOfWork.GetRepository<Brand>();
        var brands = await repo.GetAll().OrderBy(b => b.Name).ToListAsync();

        return mapper.Map<IEnumerable<BrandResponseDto>>(brands);
    }

    public async Task<BrandResponseDto?> GetByIdAsync(Guid id)
    {
        var repo = unitOfWork.GetRepository<Brand>();
        var brand = await repo.GetAll().FirstOrDefaultAsync(b => b.Id == id);

        return brand == null ? null : mapper.Map<BrandResponseDto>(brand);
    }

    public async Task<Reply<BrandResponseDto>> CreateAsync(BrandCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Reply<BrandResponseDto>.Fail("Brand name is required!");
        }

        var normalizedName = dto.Name.Trim();
        var normalizedLower = normalizedName.ToLowerInvariant();
        var repo = unitOfWork.GetRepository<Brand>();

        if (await repo.AnyAsync(b => b.Name.ToLower() == normalizedLower))
        {
            return Reply<BrandResponseDto>.Fail("A brand with this name already exists!");
        }

        var brand = mapper.Map<Brand>(dto);
        brand.Name = normalizedName;
        brand.LogoUrl = dto.LogoUrl?.Trim();
        brand.Description = dto.Description?.Trim();

        await repo.CreateAsync(brand);
        var reply = await unitOfWork.CommitAsync();

        return reply.IsSuccess
            ? Reply<BrandResponseDto>.Success(mapper.Map<BrandResponseDto>(brand))
            : Reply<BrandResponseDto>.Fail(reply.Errors!);
    }

    public async Task<Reply<BrandResponseDto>> UpdateAsync(BrandUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Reply<BrandResponseDto>.Fail("Brand name is required!");
        }

        var repo = unitOfWork.GetRepository<Brand>();
        var brand = await repo.GetAll(true).FirstOrDefaultAsync(b => b.Id == dto.Id);

        if (brand == null)
        {
            return Reply<BrandResponseDto>.Fail("Brand not found!");
        }

        var normalizedName = dto.Name.Trim();
        var normalizedLower = normalizedName.ToLowerInvariant();

        if (await repo.AnyAsync(b => b.Id != dto.Id && b.Name.ToLower() == normalizedLower))
        {
            return Reply<BrandResponseDto>.Fail("A brand with this name already exists!");
        }

        brand.Name = normalizedName;
        brand.LogoUrl = dto.LogoUrl?.Trim();
        brand.Description = dto.Description?.Trim();
        brand.Active = dto.Active;
        brand.UpdatedAt = DateTime.UtcNow;

        repo.Update(brand);
        var reply = await unitOfWork.CommitAsync();

        return reply.IsSuccess
            ? Reply<BrandResponseDto>.Success(mapper.Map<BrandResponseDto>(brand))
            : Reply<BrandResponseDto>.Fail(reply.Errors!);
    }

    public async Task<Reply> DeleteAsync(Guid id)
    {
        var repo = unitOfWork.GetRepository<Brand>();
        var brand = await repo.GetAll(true).FirstOrDefaultAsync(b => b.Id == id);

        if (brand == null)
        {
            return Reply.Fail("Brand not found!");
        }

        var productRepo = unitOfWork.GetRepository<Product>();
        if (await productRepo.AnyAsync(p => p.BrandId == id))
        {
            return Reply.Fail("Cannot delete a brand that has associated products!");
        }

        brand.Deleted = true;
        brand.Active = false;
        brand.UpdatedAt = DateTime.UtcNow;

        repo.Update(brand);
        var reply = await unitOfWork.CommitAsync();

        return reply.IsSuccess ? Reply.Success() : Reply.Fail(reply.Errors!);
    }
}
