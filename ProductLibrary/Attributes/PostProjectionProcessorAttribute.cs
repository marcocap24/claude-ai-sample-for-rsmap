namespace ProductLibrary.Attributes;

/// <summary>
/// Applicato su una classe DTO, dichiara quale processor deve essere invocato
/// dopo che le istanze sono state materializzate (che siano arrivate da
/// ProjectTo+ToListAsync o da una Map "normale"). Nasce per coprire il caso
/// in cui, usando ProjectTo, AutoMapper non passa mai per il pipeline di
/// Map() e quindi ignora .AfterMap(...): qui il post-processing è esplicito
/// e non dipende da come i dati sono stati materializzati.
///
/// Supporta due modalità, a seconda del tipo passato al costruttore:
///
/// 1) TIPO CONCRETO — es. typeof(StorePriceTierProcessor). Deve implementare
///    IPostProjectionProcessor&lt;TDto&gt; per lo stesso TDto su cui è
///    applicato l'attributo, e DEVE essere registrato nel container DI del
///    consumer: se manca, il filtro lancia un'eccezione (l'hai dichiarato tu
///    esplicitamente su quel DTO, quindi la mancata registrazione è un
///    errore di configurazione).
///
/// 2) INTERFACCIA GENERICA APERTA — typeof(IPostProjectionProcessor&lt;&gt;).
///    Pensata per essere dichiarata UNA VOLTA SOLA sul DTO base della
///    libreria (ProductDto&lt;TKey&gt;) e quindi ereditata da ogni DTO
///    derivato (l'attributo è Inherited = true). Per ciascun TDto concreto,
///    il filtro prova a risolvere da DI l'interfaccia CHIUSA
///    IPostProjectionProcessor&lt;TDto&gt;: se un consumer l'ha registrata
///    (es. AddScoped&lt;IPostProjectionProcessor&lt;ProductDtoA&gt;,
///    ProductPriceTierProcessor&gt;()), viene eseguita; altrimenti non
///    succede NIENTE, senza eccezioni. È un "aggancio" sempre disponibile,
///    silenzioso finché nessuno lo registra per quello specifico DTO.
/// </summary>
/// <example>
/// <code>
/// // Modalità 1: esplicita, per un DTO specifico
/// [PostProjectionProcessor(typeof(StorePriceTierProcessor))]
/// public class StoreDto : ProductDto&lt;int&gt;
/// {
///     public string? PriceTier { get; set; } // valorizzata dal processor, non dal mapping
/// }
///
/// // Modalità 2: sul DTO base della libreria (già fatto in ProductDto&lt;TKey&gt;)
/// [PostProjectionProcessor(typeof(IPostProjectionProcessor&lt;&gt;))]
/// public class ProductDto&lt;TKey&gt; { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public sealed class PostProjectionProcessorAttribute : Attribute
{
    public PostProjectionProcessorAttribute(Type processorType)
    {
        ProcessorType = processorType;
    }

    public Type ProcessorType { get; }
}
