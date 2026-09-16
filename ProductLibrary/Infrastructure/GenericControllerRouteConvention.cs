using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ProductLibrary.Infrastructure;

/// <summary>
/// I tipi generici chiusi non hanno (e non possono avere in modo statico)
/// un attributo [Route] pensato per ogni singola istanziazione. Questa
/// convention, applicata in fase di costruzione del model MVC, assegna a
/// ciascun controller generico scoperto la rotta dichiarata nel registry al
/// momento della registrazione (Program.cs del consumer).
/// </summary>
public sealed class GenericControllerRouteConvention : IControllerModelConvention
{
    private readonly GenericControllerRegistry _registry;

    public GenericControllerRouteConvention(GenericControllerRegistry registry)
    {
        _registry = registry;
    }

    public void Apply(ControllerModel controller)
    {
        var match = _registry.Registrations
            .FirstOrDefault(r => r.ControllerType == controller.ControllerType);

        if (match is null) return;

        // RouteTemplate non valorizzato => non tocchiamo i selector: resta
        // valido il [Route] statico già dichiarato sul controller generico
        // (letto normalmente da MVC in fase di discovery, come per qualunque
        // altro controller "normale").
        if (match.RouteTemplate is null) return;

        foreach (var selector in controller.Selectors)
        {
            selector.AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(match.RouteTemplate));
        }
    }
}
