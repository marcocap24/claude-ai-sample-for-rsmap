using ProductLibrary.Contracts;
using ConsumerA.Api.Dtos;

namespace ConsumerA.Api.Services;

/// <summary>
/// Esempio di servizio "post-projection": valorizza PriceTier in base a Price,
/// su tutta la lista di DTO già materializzata. Qui la logica è banale, ma lo
/// stesso schema regge casi che leggono più proprietà tra loro correlate,
/// o persino confrontano elementi diversi della stessa lista.
/// </summary>
public class ProductPriceTierProcessor : IPostProjectionProcessor<ProductDtoA>
{
    public Task ProcessAsync(IReadOnlyList<ProductDtoA> items, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            string tier = item.Price switch
            {
                < 10 => "Economy",
                < 100 => "Standard",
                _ => "Premium"
            };

            item.PriceTier = tier; // proprietà tipizzata, nota a compile time

            // Proprietà dinamica: il NOME della chiave dipende dal valore
            // calcolato a runtime (qui il tier stesso), cosa impossibile con
            // una proprietà C# dichiarata staticamente. Finisce comunque
            // "appiattita" nel JSON grazie a [JsonExtensionData] su ExtensionData.
            item.ExtensionData[$"is{tier}Eligible"] = true;
            item.ExtensionData["priceWithVat"] = Math.Round(item.Price * 1.22m, 2);
        }

        return Task.CompletedTask;
    }
}
