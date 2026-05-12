using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services;
using ServiceCarePackage.Services.Action;
using ServiceCarePackage.Services.CharacterData;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Events;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.UI;
using System;

namespace ServiceCarePackage;

public sealed class Plugin : IDalamudPlugin
{
    private IDalamudPluginInterface PluginInterface { get; set; }
    private ICommandManager CommandManager { get; set; }
    private ILog log { get; set; }
    private IClientState ClientState { get; set; }

    private readonly ServiceProvider services;
    private readonly UiManager ui;

    private const string CommandName = "/slutify";
    private const string TestCommandName = "/slut";

    //public Configuration Configuration { get; set; }

    public Plugin(IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager)
    {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        services = ServiceHandler.CreateProvider(pluginInterface);

        log = services.GetRequiredService<ILog>();
        ClientState = services.GetRequiredService<IClientState>();

        log.Verbose("Starting plugin");
        ServiceHandler.EnableHooks();
        ui = services.GetRequiredService<UiManager>();
        //services.GetRequiredService<ControllerEmu>();

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Main menu"
        });
        CommandManager.AddHandler(TestCommandName, new CommandInfo(OnTestCommand)
        {
            HelpMessage = "Test Debug"
        });

        ClientState = services.GetRequiredService<IClientState>();
        if (ClientState != null && ClientState.IsLoggedIn)
        {
            LoadServices();
        }

        ClientState?.Login += OnLogin;

        //services.GetRequiredService<MoveManager>();

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        services.Dispose();
        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(TestCommandName);
    }

    private void OnCommand(string command, string args) => ui.ShowMain();

    private void OnTestCommand(string command, string args)
    {
        log.Debug($"[TestCommands] {command} {args}");

        var s = services.GetRequiredService<CharacterDataService>();

        log.Warning($"Total: {s.GetTotalGil()} Gil: {s.GetPlayerGil()} Retainers: {string.Join(", ", s.GetRetainerGil()??Array.Empty<uint>())}");

        var sender = services.GetRequiredService<MessageSender>();
        log.Warning("sendtest msg");
        sender.SendMessageEnqueue("/say test");
    }

    private void LoadServices()
    {
        services.GetRequiredService<ConfigManager>().LoadForCurrentCharacter();
        services.GetRequiredService<GilService>();
        services.GetRequiredService<ActionsManager>();
    }

    private void OnLogin()
    {
        if (ClientState != null)
        {
            ClientState = services.GetRequiredService<IClientState>();
        }
        if (ClientState != null)
        {
            services.GetRequiredService<ConfigManager>().LoadForCurrentCharacter();
        }
    }

    private void OnLogout()
    {

    }
}
