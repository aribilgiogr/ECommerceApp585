using System.Globalization;
using AutoMapper;
using Core.Abstracts.IServices;
using Core.Concretes.DTOs.Category;
using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;
using Utilities._585.Data.UnitOfWorks;
using Utilities._585.Models;

namespace Business.Services;

public class CategoryService(IUnitOfWork unitOfWork, IMapper mapper) : ICategoryService
{
    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
    {
        var repo = unitOfWork.GetRepository<Category>();
        var categories = await repo.GetAll().OrderBy(c => c.Name).ToListAsync();

        return mapper.Map<IEnumerable<CategoryResponseDto>>(categories);
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetCategoryTreeAsync()
    {
        var repo = unitOfWork.GetRepository<Category>();
        var categories = repo.GetAll().OrderBy(c => c.Name);

        return await BuildTree(categories).ToListAsync();
    }

    private static IQueryable<CategoryResponseDto> BuildTree(IQueryable<Category> categories, Guid? parentId = null)
    {
        return categories
            .Where(c => c.ParentCategoryId == parentId)
            .Select(c =>
                new CategoryResponseDto(c.Id,
                    c.Name,
                    c.Description,
                    c.ParentCategoryId,
                    BuildTree(categories, parentId))
            );
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(Guid id)
    {
        var repo = unitOfWork.GetRepository<Category>();
        var category = await repo.GetAll()
            .Include(c => c.SubCategories.OrderBy(sc => sc.Name))
            .FirstOrDefaultAsync(c => c.Id == id);

        return category == null ? null : mapper.Map<CategoryResponseDto>(category);
    }

    public async Task<Reply<CategoryResponseDto>> CreateAsync(CategoryCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Reply<CategoryResponseDto>.Fail("Category name is required!");
        }

        var normalizedName = dto.Name.Trim();
        var normalizedLower = normalizedName.ToLower(CultureInfo.InvariantCulture);
        var repo = unitOfWork.GetRepository<Category>();

        if (dto.ParentCategoryId.HasValue)
        {
            var parentExists = await repo.AnyAsync(c => c.Id == dto.ParentCategoryId.Value);
            if (!parentExists)
            {
                return Reply<CategoryResponseDto>.Fail("Parent category not found!");
            }
        }

        var duplicateExists = await repo.AnyAsync(c =>
            c.ParentCategoryId == dto.ParentCategoryId && c.Name.ToLower() == normalizedLower
        );
        if (duplicateExists)
        {
            return Reply<CategoryResponseDto>.Fail("A category with this name already exists!");
        }

        var category = mapper.Map<Category>(dto);
        category.Name = normalizedName;
        category.Description = dto.Description?.Trim();

        await repo.CreateAsync(category);
        var reply = await unitOfWork.CommitAsync();

        if (!reply.IsSuccess)
        {
            return Reply<CategoryResponseDto>.Fail(reply.Errors!);
        }

        var created = await GetByIdAsync(category.Id);
        return Reply<CategoryResponseDto>.Success(
            created ?? mapper.Map<CategoryResponseDto>(category)
        );
    }

    public async Task<Reply<CategoryResponseDto>> UpdateAsync(CategoryUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Reply<CategoryResponseDto>.Fail("Category name is required!");
        }

        var repo = unitOfWork.GetRepository<Category>();
        var category = await repo.GetAll(true).FirstOrDefaultAsync(c => c.Id == dto.Id);

        if (category == null)
        {
            return Reply<CategoryResponseDto>.Fail("Category not found!");
        }

        if (dto.ParentCategoryId == dto.Id)
        {
            return Reply<CategoryResponseDto>.Fail("A category cannot be its own parent!");
        }

        if (dto.ParentCategoryId.HasValue)
        {
            var allCategories = await repo.GetAll()
                .Select(c => new { c.Id, c.ParentCategoryId })
                .ToDictionaryAsync(c => c.Id, c => c.ParentCategoryId);

            if (!allCategories.ContainsKey(dto.ParentCategoryId.Value))
            {
                return Reply<CategoryResponseDto>.Fail("Parent category not found!");
            }

            if (CreatesCycle(dto.Id, dto.ParentCategoryId.Value, allCategories))
            {
                return Reply<CategoryResponseDto>.Fail(
                    "Circular category hierarchy is not allowed!"
                );
            }
        }

        var normalizedName = dto.Name.Trim();
        var normalizedLower = normalizedName.ToLower();
        var duplicateExists = await repo.AnyAsync(c =>
            c.Id != dto.Id
            && c.ParentCategoryId == dto.ParentCategoryId
            && c.Name.ToLower() == normalizedLower
        );
        if (duplicateExists)
        {
            return Reply<CategoryResponseDto>.Fail("A category with this name already exists!");
        }

        category.Name = normalizedName;
        category.Description = dto.Description?.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;
        category.Active = dto.Active;
        category.UpdatedAt = DateTime.UtcNow;

        repo.Update(category);
        var reply = await unitOfWork.CommitAsync();

        if (!reply.IsSuccess)
        {
            return Reply<CategoryResponseDto>.Fail(reply.Errors!);
        }

        var updated = await GetByIdAsync(category.Id);
        return Reply<CategoryResponseDto>.Success(
            updated ?? mapper.Map<CategoryResponseDto>(category)
        );
    }

    public async Task<Reply> DeleteAsync(Guid id)
    {
        var repo = unitOfWork.GetRepository<Category>();
        var category = await repo.GetAll(true).FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return Reply.Fail("Category not found!");
        }

        if (await repo.AnyAsync(c => c.ParentCategoryId == id))
        {
            return Reply.Fail("Cannot delete a category that has subcategories!");
        }

        var productRepo = unitOfWork.GetRepository<Product>();
        if (await productRepo.AnyAsync(p => p.CategoryId == id))
        {
            return Reply.Fail("Cannot delete a category that has associated products!");
        }

        category.Deleted = true;
        category.Active = false;
        category.UpdatedAt = DateTime.UtcNow;

        repo.Update(category);
        var reply = await unitOfWork.CommitAsync();

        return reply.IsSuccess ? Reply.Success() : Reply.Fail(reply.Errors!);
    }


    private static bool CreatesCycle(
        Guid categoryId,
        Guid newParentId,
        IReadOnlyDictionary<Guid, Guid?> parentMap
    )
    {
        var visited = new HashSet<Guid>();
        Guid? current = newParentId;

        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == categoryId)
            {
                return true;
            }

            current = parentMap.TryGetValue(current.Value, out var nextParent) ? nextParent : null;
        }

        return false;
    }
}
