# Struttura della soluzione

```
Sample.sln
├─ ProductLibrary/                    (Class Library, i controller vivono QUI)
│  ├─ Contracts/                      IIdentifiable<TKey>, IHasDisplayName, IHasPrice, IHasCategory
│  ├─ Dtos/                           ProductDto<TKey>  (non sealed, estendibile)
│  ├─ Mapping/                        ProductProfileBase<TEntity, TKey>  (mapping "contrattuale")
│  ├─ Controllers/                    ProductsCrudController<TContext,TEntity,TDto,TKey>  (generico APERTO)
│  │                                  ProductsCategorySearchController<...>  (generico APERTO, opzionale)
│  └─ Infrastructure/                 GenericControllerRegistry, FeatureProvider, RouteConvention
│                                      (il meccanismo che chiude i generici e li espone come veri controller)
│
├─ ConsumerA.Api/                     (chiave int; ZERO classi Controller)
│  ├─ Entities/                       ProductEntity : IIdentifiable<int>, IHasDisplayName, IHasPrice
│  ├─ Dtos/                           ProductDtoA : ProductDto<int>  (+ Sku)
│  ├─ Mapping/                        ProductProfileA (mapping diretto + reverse)
│  └─ Program.cs                      registry.AddCrudController<AppDbContext, ProductEntity, ProductDtoA, int>(...)
│
└─ ConsumerB.Api/                     (chiave string; ZERO classi Controller)
   ├─ Entities/                       ProductEntity : IIdentifiable<string>, IHasDisplayName, IHasPrice, IHasCategory
   ├─ Dtos/                           ProductDtoB : ProductDto<string>  (+ Category)
   ├─ Mapping/                        ProductProfileB (mapping diretto + reverse)
   └─ Program.cs                      registry.AddCrudController<...>(...).AddCategorySearchController<...>(...)
```

## Come funzionano i controller generici in libreria

ASP.NET Core, di default, **esclude dal discovery i tipi generici aperti**
(non potrebbe istanziarli comunque). Per "chiuderli" ed esporli come veri
controller servono tre pezzi, tutti nella libreria (`Infrastructure/`):

1. **`GenericControllerRegistry`** — il consumer, in `Program.cs`, chiama
   `.AddCrudController<TContext, TEntity, TDto, TKey>("api/products")`: questo
   costruisce a runtime (`MakeGenericType`) il tipo chiuso
   `ProductsCrudController<AppDbContext, ProductEntity, ProductDtoA, int>` e lo
   registra in una lista.
2. **`GenericControllerFeatureProvider`** — un `IApplicationFeatureProvider<ControllerFeature>`
   che inietta esplicitamente quei tipi chiusi nella pipeline di discovery MVC
   (è la tecnica documentata da Microsoft per i "generic controller").
3. **`GenericControllerRouteConvention`** — un `IControllerModelConvention` che
   assegna a ciascun controller chiuso scoperto la rotta dichiarata nel
   registry (i tipi generici non possono avere un `[Route]` statico sensato
   per ogni istanziazione).

`AddLibraryGenericControllers(registry)` collega i tre pezzi con una riga sola
nel consumer.

## Conseguenza architetturale della scelta "tutto in libreria"

Prima, la divergenza tra Consumer A e B (es. l'endpoint `GetByCategory` di B)
si esprimeva scrivendo due classi Controller diverse nei rispettivi progetti.

Ora che **non esistono più classi Controller nei consumer**, quella
divergenza si esprime in un altro modo: `ProductsCategorySearchController<...>`
è un **secondo controller generico, opzionale, che vive anch'esso nella
libreria**, vincolato a `TEntity : IHasCategory`. Solo Consumer B lo registra
(perché solo la sua entità implementa quel contratto); Consumer A no.

In pratica: la "personalizzazione per consumer" non è più a livello di codice
controller (che non esiste più fuori dalla libreria), ma a livello di **quali
controller generici vengono registrati e con quali tipi concreti**. Se in
futuro un terzo consumer avrà bisogno di un comportamento realmente nuovo
(non riconducibile a un controller generico esistente), quel comportamento
andrà scritto come **un nuovo controller generico nella libreria**, non nel
progetto consumer — è il trade-off diretto della scelta fatta.

## Principi applicati

1. **Chiave generica, non `object`/`dynamic`**
   `ProductDto<TKey>` con vincolo `IEquatable<TKey>`. Type-safety mantenuta,
   niente boxing né ambiguità di equality, e le conversioni scalari restano
   esplicite e verificabili a compile time.

2. **Contratti nella libreria, implementazione nei consumer**
   `IIdentifiable<TKey>`, `IHasDisplayName`, `IHasPrice` vivono nella libreria
   e dettano "la forma minima" che un'entità deve avere per essere mappabile.
   Le entità concrete (diverse per ogni DB) le implementano **esplicitamente**,
   così il naming locale (Denominazione/Name, Prezzo/Cost...) resta libero e
   viene adattato al contratto senza sporcare le proprietà pubbliche reali.

3. **Mapping riusabile ma comunque traducibile da EF (`ProjectTo`)**
   `ProductProfileBase<TEntity, TKey>` centralizza nella libreria la logica di
   mapping comune, ma è astratto e vincolato all'interfaccia: da solo non
   basta per `ProjectTo`, perché EF Core deve conoscere il tipo concreto per
   generare SQL. Ogni consumer eredita da questa classe e in più dichiara un
   `CreateMap<EntitàConcreta, DtoConcreto>` esplicito (con `IncludeBase` per
   riusare il mapping ereditato): questo è il mapping che EF traduce.

4. **DTO estendibile dai consumer**
   `ProductDto<TKey>` non è sealed e ha proprietà con setter pubblici apposta
   per permettere l'ereditarietà (`ProductDtoA`, `ProductDtoB`), ognuno con
   campi propri (`Sku` vs `Category`) che il contratto della libreria non
   conosce e non deve conoscere.

5. **Controller generici, interamente in libreria**
   `ProductsCrudController<TContext,TEntity,TDto,TKey>` è scritto una sola
   volta, nella libreria, e usa `EF.Property<TKey>(e, "Id")` (non `e.Id` tramite
   interfaccia) per restare traducibile in SQL da `ProjectTo`/`Where`. I
   consumer non scrivono classi Controller: si limitano a registrare i tipi
   concreti in `Program.cs`. La divergenza tra consumer (endpoint extra) si
   ottiene registrando controller generici aggiuntivi solo dove servono
   (vedi sezione dedicata più sotto), non scrivendo sottoclassi.

## Rotta opzionale sui controller generici

`AddCrudController<...>` e `AddCategorySearchController<...>` accettano ora un
`routeTemplate` **facoltativo** (`string? routeTemplate = null`). Se omesso,
resta valida la rotta di default dichiarata staticamente con `[Route(...)]`
sulla classe generica aperta, in libreria (es. `"api/products"` su
`ProductsCrudController`). La `GenericControllerRouteConvention` interviene
solo quando il consumer passa esplicitamente una stringa; altrimenti non
tocca il `Selector`, lasciando che MVC legga l'attributo come farebbe per
qualunque controller "normale". Serve solo specificare la rotta quando un
consumer ne vuole una diversa dal default (es. per evitare collisioni se
registra più CRUD controller sulla stessa entità con rotte diverse).

## Post-projection processing (equivalente di AfterMap per ProjectTo) — automatico

`AutoMapper.AfterMap()` non viene mai eseguito quando si usa `ProjectTo`,
perché la proiezione è tradotta direttamente in SQL e non passa dal motore di
`Map()`. Per coprire lo stesso bisogno (valorizzare proprietà derivate su un
DTO dopo che è stato materializzato) la libreria espone:

- **`IPostProjectionProcessor<TDto>`** — contratto che un processor deve
  implementare: riceve l'intera lista di DTO già pronta (anche una lista di
  un solo elemento, per un DTO singolo) e la modifica in place.
- **`[PostProjectionProcessor(typeof(...))]`** — attributo sul DTO, in due
  modalità:
  - **tipo concreto** (`typeof(MioProcessor)`): dichiarato su un DTO
    specifico, quel tipo *deve* essere registrato in DI — se manca, il
    filtro lancia un'eccezione (l'hai richiesto esplicitamente tu).
  - **interfaccia generica aperta** (`typeof(IPostProjectionProcessor<>)`):
    dichiarata **una sola volta sul DTO base della libreria**
    (`ProductDto<TKey>`) e quindi ereditata da ogni DTO derivato. Per
    ciascun DTO concreto il filtro prova a risolvere da DI l'interfaccia
    **chiusa** (es. `IPostProjectionProcessor<ProductDtoA>`): se un
    consumer l'ha registrata, la esegue; altrimenti non fa nulla, senza
    eccezioni. È il meccanismo usato di default: nessun consumer deve
    ricordarsi di mettere l'attributo su ciascun DTO, è già lì; conta solo
    se il consumer registra o no un'implementazione per quel DTO specifico.
- **`PostProjectionProcessingFilter`** — un `IAsyncResultFilter` registrato
  **globalmente** da `AddLibraryGenericControllers`. Intercetta il valore
  prodotto da QUALUNQUE action (i controller generici della libreria, ma
  anche un controller scritto a mano nel consumer) subito prima della
  serializzazione: se il tipo restituito (o il tipo degli elementi, se è una
  collezione) porta l'attributo — direttamente o per ereditarietà — prova a
  risolvere ed eseguire il processor corrispondente.

Il controller CRUD generico non contiene più alcuna chiamata esplicita al
meccanismo: **non sa nemmeno che esiste**. Per attivarlo su un DTO basta
registrare un'implementazione di `IPostProjectionProcessor<TDto>` in DI —
niente attributi da ripetere DTO per DTO.

Esempio funzionante nello scheletro: `ConsumerA.Api.Dtos.ProductDtoA` ha una
proprietà `PriceTier` **non mappata da AutoMapper** (`.Ignore()` esplicito nel
profilo). Consumer A registra
`AddScoped<IPostProjectionProcessor<ProductDtoA>, ProductPriceTierProcessor>()`
in `Program.cs`: per questo la proprietà viene valorizzata. Consumer B, che
non registra nulla per `IPostProjectionProcessor<ProductDtoB>`, non subisce
alcun post-processing — stesso attributo ereditato dalla stessa base, nessun
effetto in assenza di registrazione — senza che
`ProductsCrudController` o `Program.cs` facciano nulla di esplicito a riguardo
oltre a registrare il processor in DI.

## Proprietà dinamiche via ExtensionData

`PriceTier` è una proprietà tipizzata: nome e tipo noti a compile time, il
processor si limita a valorizzarla. Per il caso in cui nome/numero delle
proprietà da aggiungere NON sono noti a compile time (variano per tenant,
locale, regola di business...), `ProductDto<TKey>` espone anche:

```csharp
[JsonExtensionData]
public Dictionary<string, object?> ExtensionData { get; set; } = new();
```

`[JsonExtensionData]` (di `System.Text.Json`) fa sì che ogni coppia
chiave/valore inserita in questo dizionario venga "appiattita" nel JSON di
output come se fosse una proprietà dichiarata staticamente sul DTO — non un
oggetto annidato. Un `IPostProjectionProcessor` può scriverci dentro con
qualunque chiave calcolata a runtime:

```csharp
item.ExtensionData[$"is{tier}Eligible"] = true;
item.ExtensionData["priceWithVat"] = Math.Round(item.Price * 1.22m, 2);
```

e il JSON risultante per un prodotto in fascia "Premium" sarà:

```json
{
  "id": 1,
  "displayName": "...",
  "price": 150,
  "sku": "...",
  "priceTier": "Premium",
  "isPremiumEligible": true,
  "priceWithVat": 183.00
}
```

`isPremiumEligible` non esiste come proprietà C# da nessuna parte: il nome
della chiave è deciso interamente a runtime dal processor. Nella catena di
mapping, `ExtensionData` è ignorata esplicitamente sia nel profilo base della
libreria (`ProductProfileBase`) sia — implicitamente, perché AutoMapper
ignora di default i membri sorgente senza corrispondenza — nel mapping
inverso DTO→Entity: è pensata per essere valorizzata solo dal processor, mai
dal mapping.

Per rendere lo scheletro auto-contenuto, entrambi i Consumer usano
`Microsoft.EntityFrameworkCore.InMemory` invece di un DB reale: nel progetto
reale basta sostituire `UseInMemoryDatabase(...)` con `UseSqlServer(...)`,
`UseNpgsql(...)` ecc. — nessun altro pezzo dell'architettura cambia.

Per compilare/eseguire (richiede .NET 9 SDK e ripristino pacchetti NuGet,
non eseguito in questo ambiente):

```bash
dotnet restore
dotnet run --project ConsumerA.Api
dotnet run --project ConsumerB.Api
```
