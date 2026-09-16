using ProductLibrary.Contracts;

namespace ConsumerA.Api.Entities;

/// <summary>
/// Entità reale del DB "A": chiave int, colonne con naming locale
/// (Denominazione invece di Name, Prezzo invece di Price).
/// Implementa i contratti della libreria in modo ESPLICITO per adattare
/// il naming locale al contratto, senza sporcare le proprietà pubbliche
/// dell'entità (che restano quelle "vere" del DB A).
/// </summary>
public class ProductEntity : IIdentifiable<int>, IHasDisplayName, IHasPrice
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Denominazione { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }

    // Adattamento esplicito ai contratti della libreria
    string IHasDisplayName.DisplayName => Denominazione;
    decimal IHasPrice.Price => Prezzo;
}
