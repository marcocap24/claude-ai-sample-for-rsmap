using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductLibrary.Attributes;
using ProductLibrary.Contracts;
using System.Collections;

namespace ProductLibrary.Filters;

/// <summary>
/// Result filter globale: intercetta il valore prodotto da QUALSIASI action
/// (non solo dai controller generici della libreria) subito prima che venga
/// serializzato, e — se il tipo del DTO restituito (o il tipo degli elementi,
/// se è una collezione) è decorato con [PostProjectionProcessor] — risolve il
/// processor da DI e lo invoca.
///
/// Essendo un IAsyncResultFilter, gira DOPO che l'action ha prodotto il
/// risultato ma PRIMA della scrittura in risposta: è il punto giusto per
/// modificare il DTO "in place" prima della serializzazione, senza che il
/// controller (generico o scritto a mano) debba fare nulla di esplicito.
///
/// Registrato una sola volta, globalmente, da AddLibraryGenericControllers
/// (vedi Infrastructure/ServiceCollectionExtensions.cs).
/// </summary>
public sealed class PostProjectionProcessingFilter : IAsyncResultFilter
{
	public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
	{
		if (context.Result is ObjectResult { Value: not null } objectResult)
		{
			await ApplyAsync(objectResult.Value, context.HttpContext.RequestServices, context.HttpContext.RequestAborted);
		}

		await next();
	}

	private static async Task ApplyAsync(object value, IServiceProvider services, CancellationToken cancellationToken)
	{
		var valueType = value.GetType();

		// Caso collezione: IEnumerable<TDto> (es. il risultato di GetAll).
		var enumerableInterface = valueType != typeof(string)
			? Array.Find(valueType.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			: null;

		if (enumerableInterface is not null)
		{
			var itemType = enumerableInterface.GetGenericArguments()[0];
			if (!HasProcessorAttributes(itemType))
			{
				return;
			}

			// ToListAsync() restituisce già una List<TDto>, che implementa
			// IReadOnlyList<TDto>: non serve copiarla. In generale però il
			// valore potrebbe essere un IEnumerable<TDto> qualunque, quindi
			// materializziamo con Enumerable.ToList<T> se non lo è già.
			var readOnlyListType = typeof(IReadOnlyList<>).MakeGenericType(itemType);
			var items = readOnlyListType.IsInstanceOfType(value)
				? value
				: typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(itemType).Invoke(null, [value])!;

			await InvokeProcessorsAsync(itemType, items, services, cancellationToken);
			return;
		}

		// Caso singolo DTO (es. il risultato di GetById/Create).
		if (!HasProcessorAttributes(valueType))
		{
			return;
		}

		var listType = typeof(List<>).MakeGenericType(valueType);
		var singleItemList = (IList)Activator.CreateInstance(listType)!;
		singleItemList.Add(value);

		await InvokeProcessorsAsync(valueType, singleItemList, services, cancellationToken);
	}

	private static bool HasProcessorAttributes(Type dtoType) =>
		dtoType.IsDefined(typeof(PostProjectionProcessorAttribute), inherit: true);

	private static async Task InvokeProcessorsAsync(Type dtoType, object items, IServiceProvider services, CancellationToken cancellationToken)
	{
		var attributes = dtoType.GetCustomAttributes(typeof(PostProjectionProcessorAttribute), inherit: true);
		if (attributes.Length == 0)
		{
			return;
		}

		var processorInterface = typeof(IPostProjectionProcessor<>).MakeGenericType(dtoType);
		var processAsyncMethod = processorInterface.GetMethod(nameof(IPostProjectionProcessor<object>.ProcessAsync))!;

		// Evita di invocare due volte la stessa istanza: capita facilmente
		// quando l'attributo "soft" (vedi sotto) ereditato dal DTO base e un
		// eventuale attributo esplicito sul DTO concreto risolvono allo
		// stesso identico processor registrato in DI.
		var alreadyInvoked = new HashSet<object>();

		foreach (PostProjectionProcessorAttribute attribute in attributes)
		{
			object? processor;

			if (attribute.ProcessorType.IsGenericTypeDefinition)
			{
				// Modalità "soft": l'attributo punta all'interfaccia generica
				// aperta (tipicamente ereditata dal DTO base). Se per QUESTO
				// TDto non è registrato nulla in DI, non fa niente: nessuna
				// eccezione, nessun obbligo di attributo per-DTO.
				var closedInterface = attribute.ProcessorType.MakeGenericType(dtoType);
				processor = services.GetService(closedInterface);
				if (processor is null)
				{
					continue;
				}
			}
			else
			{
				// Modalità esplicita: l'attributo punta a un tipo concreto
				// preciso, dichiarato apposta su questo DTO. Qui la mancata
				// registrazione resta un errore di configurazione.
				processor = services.GetService(attribute.ProcessorType)
					?? throw new InvalidOperationException(
						$"Il processor '{attribute.ProcessorType.Name}' dichiarato su '{dtoType.Name}' " +
						$"non è registrato nel container DI del consumer.");

				if (!processorInterface.IsInstanceOfType(processor))
				{
					throw new InvalidOperationException(
						$"Il processor '{attribute.ProcessorType.Name}' deve implementare " +
						$"IPostProjectionProcessor<{dtoType.Name}>.");
				}
			}

			if (!alreadyInvoked.Add(processor))
			{
				continue;
			}

			await (Task)processAsyncMethod.Invoke(processor, [items, cancellationToken])!;
		}
	}
}