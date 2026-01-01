using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Serilog;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Target;
using ServiceCarePackage.UI;
using System;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyFriendList;

namespace ServiceCarePackage.Windows;

internal class SettingsWindow : Window, IDisposable
{
    private Configuration configuration { get; }
    private TargetingManager targetingManager { get; }
    private ILog log { get; }

    private string ownerNameBuffer = string.Empty;
    private string ownerWorldBuffer = string.Empty;
    private bool loadedFromConfig;


    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    internal SettingsWindow(ILog log, Configuration configuration, TargetingManager targetingManager) 
        : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        //Size = new Vector2(232, 60);
        //SizeCondition = ImGuiCond.Always;

        this.configuration = configuration;
        this.targetingManager = targetingManager;
        this.log = log;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void OnOpen()
    {
        // Load once when window opens
        ownerNameBuffer = configuration.OwnerName ?? "";
        ownerWorldBuffer = configuration.OwnerWorld ?? "";
        loadedFromConfig = true;
    }

    public override void OnClose()
    {
        loadedFromConfig = false;
    }

    public override void Draw()
    {
        bool enableForcedWalk = configuration.EnableForcedWalk;
        if (ImGui.Checkbox("Forced walk", ref enableForcedWalk))
        {
            configuration.EnableForcedWalk = enableForcedWalk;
        }


        bool enablePuppetMasterHc = configuration.EnablePuppetMasterHadcore;
        if (ImGui.Checkbox("Hardcore puppet master", ref enablePuppetMasterHc))
        {
            configuration.EnablePuppetMasterHadcore = enablePuppetMasterHc;
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Enables puppet master mathching for full commands.\n"
                           + "Allows use of multi word/argument commands (replace '<>' with '[]').\n"
                           + "name, target [me]");
        }
        ImGui.Spacing();

        using (var child = ImRaii.Child("AboutYou", new Vector2(800, 150), true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                ImGui.LabelText("", "About you");

                string nameBuffer = configuration.DisplayName;
                if (ImGui.InputText("Name", ref nameBuffer, 30))
                {
                    configuration.DisplayName = nameBuffer;
                }
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Your new name for translate and client side cosmetics.");
                }


                string commandNameBuffer = configuration.CommandName;
                if (ImGui.InputText("Name for commands", ref commandNameBuffer, 30))
                {
                    configuration.CommandName = string.IsNullOrEmpty(commandNameBuffer) ?
                        configuration.DisplayName.ToLower() : commandNameBuffer;
                }
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Name used for puppetmaster command matching.\n"
                        + "If left empty - lowercase display name will be used");
                }
            }
        }
        ImGui.Spacing();

        using (var child = ImRaii.Child("AboutOwner", new Vector2(800, 250), true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                ImGui.LabelText("", "About owner");

                (string name, string world)? owner;
                if (ImGui.Button("Target select"))
                {
                    owner = targetingManager.GetTargetedPlayerName();
                    if (owner != null)
                    {
                        ownerNameBuffer = owner.Value.name;
                        ownerWorldBuffer = owner.Value.world;
                        configuration.OwnerName = ownerNameBuffer;
                        configuration.OwnerWorld = ownerWorldBuffer;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Clear owner"))
                {
                        ownerNameBuffer = string.Empty;
                        ownerWorldBuffer = string.Empty;
                        configuration.OwnerName = ownerNameBuffer;
                        configuration.OwnerWorld = ownerWorldBuffer;
                }
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Name of your owner.\n"
                        + "Used for some more intrusive features as whitelist");
                }
                ImGui.BeginDisabled();
                if (ImGui.InputText("Owner's name", ref ownerNameBuffer, 30))
                {

                }
                if (ImGui.InputText("Owner's world", ref ownerWorldBuffer, 30))
                {
                    
                }
                ImGui.EndDisabled();


                string nameAliasBuffer = configuration.OwnerNameAlias ?? "";
                if (ImGui.InputText("Owners name alias", ref nameAliasBuffer, 30))
                {
                    configuration.OwnerNameAlias = string.IsNullOrEmpty(nameAliasBuffer) ?
                        null : nameAliasBuffer;
                }
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Cute name for your owner.\n"
                        + "Used mostly cosmetically.");
                }
            }
        }

        //if (configuration.SomePropertyToBeSavedAndWithADefault) { ImGui.BeginDisabled(); }
        var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            //configuration.Save();
        }
        //if (configuration.SomePropertyToBeSavedAndWithADefault) { ImGui.EndDisabled(); }
    }
}
