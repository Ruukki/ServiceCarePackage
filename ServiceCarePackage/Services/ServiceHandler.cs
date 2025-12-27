using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using ServiceCarePackage.Config;
using ServiceCarePackage.ControllerEmulation;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.UI;
using System.Collections.Generic;

namespace ServiceCarePackage.Services
{
    public static class ServiceHandler
    {
        public static ServiceProvider? Services { get; set; }
        public static ServiceProvider CreateProvider(IDalamudPluginInterface pi)
        {
            // Create a service collection (see Dalamud.cs, if confused about AddDalamud, that is what AddDalamud(pi) pulls from)
            var services = new ServiceCollection()
                .AddDalamud(pi)
                .AddLogger()
                .AddConfig(pi)
                //.AddMovement()
                .AddTranslator()
                .AddChat();
                //.AddMovement();
            // return the built services provider in the form of a instanced service collection
            Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
            return Services;
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
                 var interop = _.GetRequiredService<IGameInteropProvider>();
                 //var config = _.GetRequiredService<Configuration>();
                 var logger = _.GetRequiredService<MyLog>();
                 //var clientState = _.GetRequiredService<IClientState>();
                 //var historyService = _.GetRequiredService<HistoryService>();
                 var translator = _.GetRequiredService<Translator.Translator>();
                 return new ChatInputManager(logger, interop, translator);
             })
            .AddSingleton<ChatUI>(_ => 
            {
                var logger = _.GetRequiredService<MyLog>();
                var framework = _.GetRequiredService<IFramework>();
                var gameUi = _.GetRequiredService<IGameGui>();
                return new ChatUI(logger, gameUi, framework);
            });

        private static IServiceCollection AddTranslator(this IServiceCollection services)
        {
            Dictionary<string, string> englishDict = new Dictionary<string, string>()
            {
                {"my", "_'s"},
                {"mine", "_'s"},
                {"me", "_"},
                {"I'm", "_ is"},
                {"Im", "_ is"},
                {"I'll", "_ will"},
                {"I'd", "_ would"},
                {"I've", "_ has"},
                {"I am", "_ is"},
                //{"myself", "itself"},
                {"I", "_"},
                {"am", "is"}
            };
            return services.AddSingleton<Translator.Translator>(_ => { return new Translator.Translator(englishDict, () => FixedConfig.Name); });
        }

        private static IServiceCollection AddMovement(this IServiceCollection services)
        {
            return services.AddSingleton<ControllerEmu>(_ => 
            { 
                var pluginLog = _.GetRequiredService<MyLog>();
                var chatui = _.GetRequiredService<ChatUI>();
                var framework = (_.GetRequiredService<IFramework>());
                return new ControllerEmu(framework, chatui, pluginLog); 
            });
        }

        internal static void EnableHooks()
        {
            if (Services != null)
            {
                Services.GetRequiredService<ChatInputManager>().EnableHooks();
            }
        }
    }
}
