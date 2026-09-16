using ConsumerB.Api.Dtos;
using ConsumerB.Api.Entities;
using ProductLibrary.Dtos;
using ProductLibrary.Mapping;

namespace ConsumerB.Api.Mapping;

public class ProductProfileB : ProductProfileBase<ProductEntity, string>
{
    public ProductProfileB()
    {
        CreateMap<ProductEntity, ProductDtoB>()
            .IncludeBase<ProductEntity, ProductDto<string>>()
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category));

        // Reverse map DTO -> Entity, richiesta dal controller CRUD generico.
        CreateMap<ProductDtoB, ProductEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.DisplayName))
            .ForMember(d => d.Cost, o => o.MapFrom(s => s.Price))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category));
    }
}
