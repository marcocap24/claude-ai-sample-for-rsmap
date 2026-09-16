namespace ProductLibrary.Contracts;

/// <summary>
/// Contratto del servizio "post-projection" per un DTO: riceve l'intera lista
/// già materializzata (anche una lista con un solo elemento, per GetById) e la
/// modifica in place. Lavora sulla lista, non sul singolo elemento, per poter
/// coprire anche casi che richiedono di guardare più elementi insieme (es. un
/// calcolo comparativo tra righe), pur restando adatto al caso comune "una
/// proprietà derivata dalle altre dello stesso DTO".
/// </summary>
public interface IPostProjectionProcessor<TDto>
{
    Task ProcessAsync(IReadOnlyList<TDto> items, CancellationToken cancellationToken = default);
}
