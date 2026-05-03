using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace ServiceCarePackage.Windows;

internal class MainWindow : Window, IDisposable
{
    private Configuration configuration {  get; }

    private SettingsWindow settingsWindow { get; }

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    internal MainWindow(Configuration configuration, SettingsWindow settingsWindow)
        : base("Miki Mod Workshop##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.configuration = configuration;
        this.settingsWindow = settingsWindow;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            settingsWindow.IsOpen = true;
        }

        ImGui.Spacing();

    }
}
