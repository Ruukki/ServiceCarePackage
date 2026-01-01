using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ServiceCarePackage.Windows;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCarePackage.UI
{
    internal class UiManager : IDisposable
    {
        private readonly WindowSystem windowSystem = new("ServiceCarePackage");
        public MainWindow Main { get; }
        public SettingsWindow Settings { get; }
        public IDalamudPluginInterface pi { get; }
        internal UiManager(MainWindow main, SettingsWindow config, IDalamudPluginInterface pi) 
        {
            this.Main = main;
            this.Settings = config;
            this.pi = pi;

            windowSystem.AddWindow(Main);
            windowSystem.AddWindow(Settings);

            this.pi.UiBuilder.Draw += Draw;
            this.pi.UiBuilder.OpenConfigUi += ShowSettings; // optional: opens config from /xlplugins
        }

        private void Draw() => windowSystem.Draw();
        public void ShowMain() => Main.IsOpen = true;
        public void ShowSettings() => Settings.IsOpen = true;

        public void Dispose()
        {
            pi.UiBuilder.Draw -= Draw;
            pi.UiBuilder.OpenConfigUi -= ShowSettings;
        }
    }
}
