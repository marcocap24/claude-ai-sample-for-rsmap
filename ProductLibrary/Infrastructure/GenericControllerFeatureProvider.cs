using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ProductLibrary.Infrastructure;

/// <summary>
/// ASP.NET Core, di default, esclude dal discovery i tipi generici APERTI
/// (non potrebbe comunque istanziarli). Questo feature provider aggiunge
/// esplicitamente i tipi generici CHIUSI presenti nel registry: è il modo
/// "ufficiale" (documentato da Microsoft) per avere controller generici.
/// </summary>
public sealed class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly GenericControllerRegistry _registry;

    public GenericControllerFeatureProvider(GenericControllerRegistry registry)
    {
        _registry = registry;
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var registration in _registry.Registrations)
        {
            var typeInfo = registration.ControllerType.GetTypeInfo();
            if (!feature.Controllers.Contains(typeInfo))
            {
                feature.Controllers.Add(typeInfo);
            }
        }
    }
}
