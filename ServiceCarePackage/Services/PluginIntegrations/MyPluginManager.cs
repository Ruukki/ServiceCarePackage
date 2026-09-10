using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCarePackage.Services.PluginIntegrations
{
    internal class MyPluginManager : IDisposable
    {
        private readonly ILog log;
        private readonly MessageSender messageSender;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly IChatGui chatGui;
        private string[] pluginNames = { "ProjectGagSpeak", "" };

        internal MyPluginManager(ILog log, MessageSender messageSender, IDalamudPluginInterface pluginInterface, IChatGui chatGui)
        {
            this.log = log;
            this.messageSender = messageSender;
            this.pluginInterface = pluginInterface;
            this.chatGui = chatGui;

            this.pluginInterface.ActivePluginsChanged += OnActivePluginsChanged;
        }

        private void OnActivePluginsChanged(IActivePluginsChangedEventArgs args)
        {
            log.Info($"OnActivePlugins Changed");

            if (FixedConfig.BypassPluginStateCheck)
            {
                return;
            }

            if (args.Kind != PluginListInvalidationKind.Unloaded)
            {
                return;
            }

            var matches = args.AffectedInternalNames.Where(x => pluginNames.Contains(x)).ToList();

            if (matches.Any())
            {
                foreach (var plugin in matches)
                {
                    _ = EnablePluginDelayedAsync(plugin);
                }
            }
        }

        private async Task EnablePluginDelayedAsync(string pluginName, bool printText = true)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            if (printText)
            {
                chatGui.Print(new SeStringBuilder().AddUiForeground(17).AddText($"What do you think you are doing, {FixedConfig.DisplayName}").AddUiForegroundOff()
                        .BuiltString);
                chatGui.Print(new SeStringBuilder().AddUiForeground(17).AddText($"{pluginName} will stay on!").AddUiForegroundOff()
                        .BuiltString);
            }

            messageSender.SendMessageEnqueue($"/xlenableplugin {pluginName}");
        }

        public void Dispose()
        {
            this.pluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;
        }
    }
}
