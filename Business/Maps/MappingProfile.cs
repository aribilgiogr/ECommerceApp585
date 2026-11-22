using AutoMapper;
using Core.Concretes.DTOs.Auth;
using Core.Concretes.DTOs.Brand;
using Core.Concretes.DTOs.Cart;
using Core.Concretes.DTOs.Category;
using Core.Concretes.DTOs.Order;
using Core.Concretes.DTOs.Product;
using Core.Concretes.Entities;

namespace Business.Maps;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User & Auth
        CreateMap<RegisterDto, User>();
        CreateMap<User, UserProfileDto>();

        // Category (Recursive)
        CreateMap<CategoryCreateDto, Category>();
        CreateMap<CategoryUpdateDto, Category>();
        CreateMap<Category, CategoryResponseDto>();

        // Brand
        CreateMap<BrandCreateDto, Brand>();
        CreateMap<BrandUpdateDto, Brand>();
        CreateMap<Brand, BrandResponseDto>();

        // Product, Attributes & Images
        CreateMap<ProductAttributeDto, ProductAttribute>().ReverseMap();
        CreateMap<ProductImageDto, ProductImage>().ReverseMap();
        CreateMap<ProductCreateDto, Product>();
        CreateMap<ProductUpdateDto, Product>()
            .ForMember(dest => dest.Attributes, opt => opt.Ignore())
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<Product, ProductResponseDto>()
            .ForCtorParam("CategoryName", opt =>
                opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty)
            )
            .ForCtorParam("BrandName", opt =>
                opt.MapFrom(src => src.Brand != null ? src.Brand.Name : string.Empty)
            );

        // Cart & CartItem
        CreateMap<CartItem, CartItemResponseDto>()
            .ForCtorParam("ProductName", opt => opt.MapFrom(src => src.Product.Name))
            .ForCtorParam("CoverImageUrl", opt => opt.MapFrom(src => src.Product.CoverImageUrl))
            .ForCtorParam("UnitPrice", opt => opt.MapFrom(src => src.Product.Price))
            .ForCtorParam("DiscountRate", opt => opt.MapFrom(src => src.Product.DiscountRate))
            .ForCtorParam("DiscountedUnitPrice", opt =>
                opt.MapFrom(src => src.Product.DiscountedPrice)
            )
            .ForCtorParam("TaxRate", opt => opt.MapFrom(src => src.Product.TaxRate))
            .ForCtorParam("FinalUnitPriceWithTax", opt =>
                opt.MapFrom(src => src.Product.FinalPriceWithTax)
            )
            .ForCtorParam("LineTotalWithTax", opt =>
                opt.MapFrom(src => src.Product.FinalPriceWithTax * src.Quantity)
            );

        // Order & OrderItem
        CreateMap<OrderItem, OrderItemResponseDto>()
            .ForCtorParam("ProductName", opt => opt.MapFrom(src => src.Product.Name));

        CreateMap<Order, OrderResponseDto>();
    }
}
