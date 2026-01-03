using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ServiceCarePackage.Config;
using ServiceCarePackage.ControllerEmulation;
using ServiceCarePackage.Services;
using ServiceCarePackage.Services.CharacterData;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using ServiceCarePackage.UI;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        log = services.GetRequiredService<MyLog>();
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
            services.GetRequiredService<ConfigManager>().LoadForCurrentCharacter();
        }

        ClientState?.Login += OnLogin;

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

        var move = services.GetRequiredService<MoveManager>();

        if (args.IsNullOrEmpty())
        {
            /*new Task(() =>
            {
                Thread.Sleep(10000);
                xx.IsWalkingForced = false;
            }).Start();*/
            //xx.DisableMovingFor(5000);
            /*var zz = services.GetRequiredService<MessageSender>();
            zz.SendMessage("test");*/
            //services.GetRequiredService<CharacterDataControl>().SetOnlineStatus();
        }
        else if (args.Equals("true")) 
        {
            //log.Debug($"{}");
            move.IsWalkingForced = true;
        }
        else if (args.Equals("false"))
        {
            move.IsWalkingForced = false;
        }
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
