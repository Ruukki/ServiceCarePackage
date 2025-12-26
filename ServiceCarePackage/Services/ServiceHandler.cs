using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;

namespace ServiceCarePackage.Services
{
    public static class ServiceHandler
    {
        public static ServiceProvider CreateProvider(IDalamudPluginInterface pi)
        {
            // Create a service collection (see Dalamud.cs, if confused about AddDalamud, that is what AddDalamud(pi) pulls from)
            var services = new ServiceCollection()
                .AddDalamud(pi)
                .AddLogger()
                .AddConfig(pi)
                //.AddMovement()
                .AddChat();
                //.AddExtras()
                //.AddAction()
                //.AddManagers()
                //.AddApi()
                //.AddUi();
            // return the built services provider in the form of a instanced service collection
            return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }

        private static IServiceCollection AddDalamud(this IServiceCollection services, IDalamudPluginInterface pi)
        {
            // Add the dalamudservices to the service collection
            new DalamudServices(pi).AddServices(services);
            return services;
        }

        private static IServiceCollection AddLogger(this IServiceCollection services)
        {
            return services.AddSingleton<MyLog>(_ => { var pluginLog = _.GetRequiredService<IPluginLog>(); return new MyLog(pluginLog); });
        }

        private static IServiceCollection AddConfig(this IServiceCollection services, IDalamudPluginInterface pi)
        {
            Configuration? config = pi.GetPluginConfig() as Configuration;
            if (config != null)
            {
                //config.LoadInterface(pi);
                return services.AddSingleton<Configuration>(config);
            }
            return services.AddSingleton<Configuration>();
        }

        private static IServiceCollection AddChat(this IServiceCollection services)
        => services
             //.AddSingleton<MessageSender>(_ => { var sigService = _.GetRequiredService<ISigScanner>(); var framework = _.GetRequiredService<IFramework>(); return new MessageSender(sigService, framework); })
             .AddSingleton<ChatInputManager>(_ =>
             {
                 // this shit is all a bit wild but its nessisary to handle our danger file stuff correctly. Until you learn more about signatures, i dont advise
                 // you to try and replicate this. However, when you do, just know this is how to correctly integrate them into a service collection structure
                 //var sigService = _.GetRequiredService<ISigScanner>();
                 //var interop = _.GetRequiredService<IGameInteropProvider>();
                 //var config = _.GetRequiredService<Configuration>();
                 var logger = _.GetRequiredService<ILog>();
                 //var clientState = _.GetRequiredService<IClientState>();
                 //var historyService = _.GetRequiredService<HistoryService>();
                 return new ChatInputManager(logger);
             });
    }
}
