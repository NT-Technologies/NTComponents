using System.Diagnostics.CodeAnalysis;
using NTComponents;
using NTComponents.Core;
using NTComponents.Snackbar;
using NTComponents.Storage;
using NTComponents.Toast;
using NTComponents.Windowing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for adding NT services to the service collection.
/// </summary>
[ExcludeFromCodeCoverage]
public static class NTServiceExt {

    /// <summary>
    /// Adds NT services to the service collection.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="options">Action to configure NTComponents default options</param>
    /// <returns>The IServiceCollection instance</returns>
    public static IServiceCollection AddNTServices(this IServiceCollection services, Action<NTComponentsDefaultOptions>? options = null) {
        //#if DEBUG
        //        services.AddSassCompiler();
        //#endif
        var o = new NTComponentsDefaultOptions();
        options?.Invoke(o);
        return services.AddScoped<INTSnackbarService, NTSnackbarService>()
             .AddScoped<INTToastService, NTToastService>()
             .AddScoped<INTWindowService, NTWindowService>()
             .AddScoped<ISessionStorageService, SessionStorageService>()
             .AddScoped<ILocalStorageService, LocalStorageService>()
             .AddSingleton(o);
    }

}
