using ConsumerA.Api.Dtos;
using ConsumerA.Api.Entities;
using ProductLibrary.Dtos;
using ProductLibrary.Mapping;

namespace ConsumerA.Api.Mapping;

/// <summary>
/// Mapping concreto ProductEntity(A) -> ProductDtoA.
/// Eredita da ProductProfileBase (definito nella libreria) per il mapping
/// "contrattuale" (Id/DisplayName/Price), e qui in più registra il CreateMap
/// concreto necessario a ProjectTo per generare SQL corretto, aggiungendo
/// il mapping del campo specifico Sku.
/// </summary>
public class ProductProfileA : ProductProfileBase<ProductEntity, int>
{
    public ProductProfileA()
    {
        CreateMap<ProductEntity, ProductDtoA>()
            .IncludeBase<ProductEntity, ProductDto<int>>()
            .ForMember(d => d.Sku, o => o.MapFrom(s => s.Sku))
            .ForMember(d => d.PriceTier, o => o.Ignore()); // valorizzata da ProductPriceTierProcessor, non dal mapping

        // Reverse map DTO -> Entity: ora necessaria perché il controller CRUD
        // generico (nella libreria) non conosce il naming locale delle colonne
        // (Denominazione/Prezzo) e delega interamente ad AutoMapper sia in
        // lettura sia in scrittura.
        CreateMap<ProductDtoA, ProductEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Denominazione, o => o.MapFrom(s => s.DisplayName))
            .ForMember(d => d.Prezzo, o => o.MapFrom(s => s.Price))
            .ForMember(d => d.Sku, o => o.MapFrom(s => s.Sku));
    }
}
