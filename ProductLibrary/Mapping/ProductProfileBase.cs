using AutoMapper;
using ProductLibrary.Contracts;
using ProductLibrary.Dtos;

namespace ProductLibrary.Mapping;

/// <summary>
/// Profilo base astratto: centralizza nella libreria la logica di mapping
/// "contrattuale" (Id, DisplayName, Price) comune a tutti i consumer.
///
/// IMPORTANTE per ProjectTo/IQueryable: questa classe NON registra da sola
/// un mapping traducibile in SQL, perché il generic è vincolato a un'interfaccia
/// (TEntity : IIdentifiable<TKey>...) e EF Core non sa tradurre query basate
/// su interfacce. Serve quindi che ogni consumer erediti da questa classe e
/// dichiari esplicitamente CreateMap&lt;EntitàConcreta, DtoConcreto&gt;, così
/// EF Core "vede" i tipi reali e riesce a generare l'SQL corretto.
///
/// Il vantaggio: la logica comune (quali proprietà si mappano e come) si scrive
/// una sola volta qui, i consumer la riusano con IncludeBase.
/// </summary>
public abstract class ProductProfileBase<TEntity, TKey> : Profile
    where TEntity : class, IIdentifiable<TKey>, IHasDisplayName, IHasPrice
    where TKey : IEquatable<TKey>
{
    protected ProductProfileBase()
    {
        CreateMap<TEntity, ProductDto<TKey>>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName))
            .ForMember(d => d.Price, o => o.MapFrom(s => s.Price))
            .ForMember(d => d.ExtensionData, o => o.Ignore()); // valorizzata (se serve) da un IPostProjectionProcessor, mai dal mapping
    }
}
