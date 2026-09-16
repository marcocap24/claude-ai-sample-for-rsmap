namespace ProductLibrary.Contracts;

/// <summary>
/// Contratto minimo: qualunque entità mappabile a un ProductDto&lt;TKey&gt;
/// deve esporre una chiave di questo tipo.
/// TKey è vincolato a IEquatable&lt;TKey&gt; per garantire confronti/equality sensati
/// (niente object/dynamic: qui vogliamo type-safety, non genericità totale).
/// </summary>
public interface IIdentifiable<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; }
}

/// <summary>
/// Contratto per un nome "visualizzabile". Ogni DB potrà avere una colonna diversa
/// (Nome, Denominazione, Name...) ma l'entità deve saperlo esporre come DisplayName,
/// tipicamente tramite implementazione esplicita dell'interfaccia.
/// </summary>
public interface IHasDisplayName
{
    string DisplayName { get; }
}

/// <summary>
/// Contratto di prezzo. Stesso discorso: la colonna reale può chiamarsi
/// Prezzo, Cost, UnitPrice ecc., ma l'entità deve sapersi "adattare" al contratto.
/// </summary>
public interface IHasPrice
{
    decimal Price { get; }
}

/// <summary>
/// Contratto opzionale: solo le entità che espongono una categoria
/// potranno essere servite dal controller generico di ricerca per categoria
/// (vedi Controllers/ProductsCategorySearchController).
/// </summary>
public interface IHasCategory
{
    string Category { get; }
}
