using ProductLibrary.Dtos;

namespace ConsumerA.Api.Dtos;

/// <summary>
/// Consumer A estende il DTO canonico (chiave int) con una proprietà propria (Sku)
/// non prevista dal contratto della libreria. È il caso "consumer aggiunge campi".
///
/// PriceTier dimostra il meccanismo di post-projection processing: non viene
/// valorizzata da AutoMapper (nessun ForMember per questa proprietà nel
/// profilo). Il post-processing avviene grazie all'attributo
/// [PostProjectionProcessor(typeof(IPostProjectionProcessor&lt;&gt;))]
/// ereditato dal DTO base (vedi ProductLibrary.Dtos.ProductDto&lt;TKey&gt;):
/// non serve più ripetere alcun attributo qui. Basta che, in Program.cs,
/// questo consumer registri un'implementazione per
/// IPostProjectionProcessor&lt;ProductDtoA&gt; — cosa che infatti fa.
/// Consumer B, che non registra nulla per ProductDtoB, semplicemente non
/// subisce alcun post-processing: stesso attributo ereditato, nessun effetto.
/// </summary>
public class ProductDtoA : ProductDto<int>
{
    public string Sku { get; set; } = string.Empty;

    /// <summary>Derivata da Price a runtime da ProductPriceTierProcessor, non dal mapping.</summary>
    public string? PriceTier { get; set; }
}
