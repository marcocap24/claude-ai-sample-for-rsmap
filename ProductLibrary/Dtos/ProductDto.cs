using System.Text.Json.Serialization;
using ProductLibrary.Attributes;
using ProductLibrary.Contracts;

namespace ProductLibrary.Dtos;

/// <summary>
/// DTO canonico esposto dalla libreria. Generico sulla chiave (TKey) perché
/// i vari consumer hanno chiavi di tipo diverso (int, string, Guid...).
/// Non sealed e con setter pubblici: i consumer devono poter derivare da questa
/// classe per aggiungere proprietà specifiche (vedi ConsumerA/B.Dtos).
///
/// [PostProjectionProcessor(typeof(IPostProjectionProcessor&lt;&gt;))] è
/// dichiarato qui, una volta sola: grazie a Inherited = true sull'attributo,
/// OGNI DTO derivato (ProductDtoA, ProductDtoB, un domani StoreDto...) lo
/// eredita automaticamente. Non serve più ripetere l'attributo su ciascun
/// DTO: per ognuno, il filtro prova a risolvere da DI l'interfaccia chiusa
/// IPostProjectionProcessor&lt;QuelDto&gt; e la esegue solo se un consumer
/// l'ha effettivamente registrata (vedi ConsumerA.Api/Program.cs). Nessuna
/// registrazione => nessun effetto, silenziosamente.
/// </summary>
[PostProjectionProcessor(typeof(IPostProjectionProcessor<>))]
public class ProductDto<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
    public string DisplayName { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>
    /// Proprietà dinamiche: [JsonExtensionData] fa sì che System.Text.Json
    /// "appiattisca" le coppie chiave/valore qui dentro come se fossero
    /// proprietà normali del DTO nel JSON di output (non un oggetto
    /// annidato). Utile quando un IPostProjectionProcessor deve aggiungere
    /// proprietà il cui nome, numero o presenza non sono noti a compile
    /// time (es. variano per tenant, locale, o regola di business) — a
    /// differenza di una proprietà come PriceTier, che invece è tipizzata e
    /// nota in anticipo. Vuoto di default: non aggiunge nulla al JSON finché
    /// un processor non ci scrive dentro.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?> ExtensionData { get; set; } = new();
}
