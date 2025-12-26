using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ServiceCarePackage.Services;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using System;
using System.IO;

namespace ServiceCarePackage;

public sealed class Plugin : IDalamudPlugin
{
    private IDalamudPluginInterface PluginInterface { get; set; }
    private ICommandManager CommandManager { get; set; }
    private ILog Log { get; set; }
    private IClientState ClientState { get; set; }

    private readonly ServiceProvider services;

    private const string CommandName = "/slutify";

    public Configuration Configuration { get; set; }

    public Plugin(IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager)
    {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        services = ServiceHandler.CreateProvider(pluginInterface);

        Log = services.GetRequiredService<MyLog>();
        ClientState = services.GetRequiredService<IClientState>();

        Log.Verbose("Starting plugin");
        ServiceHandler.EnableHooks();

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        services.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        
    }
}
