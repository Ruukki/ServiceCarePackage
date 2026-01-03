using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using ServiceCarePackage.Config;
using ServiceCarePackage.ControllerEmulation;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using ServiceCarePackage.Services.Target;
using ServiceCarePackage.UI;
using ServiceCarePackage.Windows;
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
                .AddMovement()
                .AddTranslator()
                .AddChat()
                .AddTargeting()
                .AddUi(pi);
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
            return services.AddSingleton<Configuration>()
                .AddSingleton<ConfigManager>(_ =>
                {
                    var log = _.GetRequiredService<MyLog>();
                    var playerState = _.GetRequiredService<IPlayerState>();
                    return new ConfigManager(log, pi, playerState);
                });
        }

        private static IServiceCollection AddChat(this IServiceCollection services)
        => services
             .AddSingleton<MessageSender>(_ => 
             { 
                 var sigService = _.GetRequiredService<ISigScanner>(); 
                 var framework = _.GetRequiredService<IFramework>(); 
                 return new MessageSender(sigService, framework); 
             })
             .AddSingleton<ChatInputManager>(_ =>
             {
                 // this shit is all a bit wild but its nessisary to handle our danger file stuff correctly. Until you learn more about signatures, i dont advise
                 // you to try and replicate this. However, when you do, just know this is how to correctly integrate them into a service collection structure
                 var interop = _.GetRequiredService<IGameInteropProvider>();
                 var logger = _.GetRequiredService<MyLog>();
                 var translator = _.GetRequiredService<Translator.Translator>();
                 var playerState = _.GetRequiredService<IPlayerState>();
                 return new ChatInputManager(logger, interop, translator, playerState);
             })
            .AddSingleton<ChatUI>(_ => 
            {
                var logger = _.GetRequiredService<MyLog>();
                var framework = _.GetRequiredService<IFramework>();
                var gameUi = _.GetRequiredService<IGameGui>();
                return new ChatUI(logger, gameUi, framework);
            })
            .AddSingleton<ChatReader>(_ =>
            {
                var chatGui = _.GetRequiredService<IChatGui>();
                var config = _.GetRequiredService<Configuration>();
                var messageSender = _.GetRequiredService<MessageSender>();
                var framework = _.GetRequiredService<IFramework>();
                var log = _.GetRequiredService<MyLog>();
                var moveManager = _.GetRequiredService<MoveManager>();
                var playerState = _.GetRequiredService<IPlayerState>();
                return new ChatReader(chatGui, config, messageSender, framework, log, moveManager, playerState);
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
            /*services = services.AddSingleton<ControllerEmu>(_ => 
            { 
                var pluginLog = _.GetRequiredService<MyLog>();
                var chatui = _.GetRequiredService<ChatUI>();
                var framework = (_.GetRequiredService<IFramework>());
                return new ControllerEmu(framework, chatui, pluginLog); 
            });*/
            return services.AddSingleton<MoveMemory>(_ =>
            {
                var pluginLog = _.GetRequiredService<MyLog>();
                var interop = _.GetRequiredService<IGameInteropProvider>();
                return new MoveMemory(interop, pluginLog);
            }).AddSingleton<MoveManager>(_ =>
            {
                var pluginLog = _.GetRequiredService<MyLog>();
                var moveMem = _.GetRequiredService<MoveMemory>();
                var framework = (_.GetRequiredService<IFramework>());
                var condition = _.GetRequiredService<ICondition>();
                return new MoveManager(pluginLog, moveMem, framework, condition);
            });
        }

        private static IServiceCollection AddUi(this IServiceCollection services, IDalamudPluginInterface pi)
        {
            return services.AddSingleton<UiManager>(_ =>
            {
                var mainWindow = _.GetRequiredService<MainWindow>();
                var settingsWindow = _.GetRequiredService<SettingsWindow>();
                return new UiManager(mainWindow, settingsWindow, pi);
            }).AddSingleton<SettingsWindow>(_ =>
            {
                var log = _.GetRequiredService<MyLog>();
                var configuration = _.GetRequiredService<Configuration>();
                var targeting =_.GetRequiredService<TargetingManager>();
                var config = _.GetRequiredService<ConfigManager>();
                return new SettingsWindow(log, configuration, targeting, config);
            }).AddSingleton<MainWindow>(_ =>
            {
                var configuration = _.GetRequiredService<Configuration>();
                var settings = _.GetRequiredService<SettingsWindow>();
                return new MainWindow(configuration, settings);
            });
        }

        private static IServiceCollection AddTargeting(this IServiceCollection services)
        {
            return services.AddSingleton<TargetingManager>(_ =>
            {
                var log = _.GetRequiredService<MyLog>();
                var target = _.GetRequiredService<ITargetManager>();
                var state = _.GetRequiredService<IClientState>();
                return new TargetingManager(log, target, state);
            });
        }

        internal static void EnableHooks()
        {
            if (Services != null)
            {
                Services.GetRequiredService<ChatInputManager>().EnableHooks();
                Services.GetRequiredService<ChatReader>();
            }
        }
    }
}
