using ProductLibrary.Contracts;

namespace ConsumerB.Api.Entities;

/// <summary>
/// Entità reale del DB "B": chiave string (es. codice prodotto), e in questo
/// caso il naming locale coincide già con quello del contratto (Name/Cost),
/// quindi qui l'adattamento è quasi "gratuito" — implementiamo comunque
/// esplicitamente per restare consistenti e disaccoppiati dal contratto.
/// </summary>
public class ProductEntity : IIdentifiable<string>, IHasDisplayName, IHasPrice, IHasCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Category { get; set; } = string.Empty;

    string IHasDisplayName.DisplayName => Name;
    decimal IHasPrice.Price => Cost;
}
