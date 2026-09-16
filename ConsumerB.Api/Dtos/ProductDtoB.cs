using ProductLibrary.Dtos;

namespace ConsumerB.Api.Dtos;

/// <summary>
/// Consumer B estende il DTO canonico (chiave string) con una proprietà
/// diversa da quella di Consumer A (Category invece di Sku): ogni consumer
/// è libero di estendere il DTO base con i propri campi.
/// </summary>
public class ProductDtoB : ProductDto<string>
{
    public string Category { get; set; } = string.Empty;
}
