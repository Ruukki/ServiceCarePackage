using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Serilog;
using ServiceCarePackage.Config;
using ServiceCarePackage.Models;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Target;
using ServiceCarePackage.UI;
using System;
using System.Linq;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyFriendList;

namespace ServiceCarePackage.Windows;

internal class SettingsWindow : Window, IDisposable
{
    private Configuration configuration { get; }
    private ConfigManager configManager { get; }
    private TargetingManager targetingManager { get; }
    private ILog log { get; }

    private string ownerNameBuffer = string.Empty;
    private string ownerWorldBuffer = string.Empty;
    private bool loadedFromConfig;

    private string addKeyBuffer = "";
    private string addAliasBuffer = "";
    private string filterBuffer = "";


    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    internal SettingsWindow(ILog log, Configuration configuration, TargetingManager targetingManager, ConfigManager configManager) 
        : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        //Size = new Vector2(232, 60);
        //SizeCondition = ImGuiCond.Always;

        this.configuration = configuration;
        this.targetingManager = targetingManager;
        this.log = log;
        this.configManager = configManager;
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
        ownerNameBuffer = configManager.Current.OwnerName ?? "";
        ownerWorldBuffer = configManager.Current.OwnerWorld ?? "";
        loadedFromConfig = true;
    }

    public override void OnClose()
    {
        loadedFromConfig = false;
    }

    public override void Draw()
    {
        
        if (ImGui.Button("Load settings"))
        {
            configManager.LoadForCurrentCharacter();
        }
        ImGui.SameLine();
        if (ImGui.Button("Save settings"))
        {
            configManager.Save();
        }

        if (ImGui.BeginTabBar("Outer"))
        {
            if (ImGui.BeginTabItem("Features"))
            {
                bool enableTranslate = configManager.Current.EnableTranslate;
                if (ImGui.Checkbox("Translate", ref enableTranslate))
                {
                    configManager.Current.EnableTranslate = enableTranslate;
                }


                bool enablePuppetMaster = configManager.Current.EnablePuppetMaster;
                if (ImGui.Checkbox("PuppetMaster", ref enablePuppetMaster))
                {
                    configManager.Current.EnablePuppetMaster = enablePuppetMaster;
                }


                bool enableForcedWalk = configManager.Current.EnableForcedWalk;
                if (ImGui.Checkbox("Forced walk", ref enableForcedWalk))
                {
                    configManager.Current.EnableForcedWalk = enableForcedWalk;
                }


                bool enablePuppetMasterHc = configManager.Current.EnablePuppetMasterHadcore;
                if (ImGui.Checkbox("Hardcore puppet master", ref enablePuppetMasterHc))
                {
                    configManager.Current.EnablePuppetMasterHadcore = enablePuppetMasterHc;
                }
                Tooltip("Enables puppet master mathching for full commands.\n"
                                   + "Allows use of multi word/argument commands (replace '<>' with '[]').\n"
                                   + "name, target [me]");
                ImGui.Spacing();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("About you"))
            {
                string nameBuffer = configManager.Current.DisplayName;
                if (ImGui.InputText("Name", ref nameBuffer, 30))
                {
                    configManager.Current.DisplayName = nameBuffer;
                }
                Tooltip("Your new name for translate and client side cosmetics.");


                string commandNameBuffer = configManager.Current.CommandName;
                if (ImGui.InputText("Name for commands", ref commandNameBuffer, 30))
                {
                    configManager.Current.CommandName = string.IsNullOrEmpty(commandNameBuffer) ?
                        configManager.Current.DisplayName.ToLower() : commandNameBuffer;
                }
                Tooltip("Name used for puppetmaster command matching.\n"
                        + "If left empty - lowercase display name will be used");

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("About owner"))
            {
                DrawOwners();                

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
        

        using (var child = ImRaii.Child("AboutYou", new Vector2(800, 150), true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                
            }
        }
        ImGui.Spacing();

        using (var child = ImRaii.Child("AboutOwner", new Vector2(800, 250), true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                
            }
        }

        //if (configManager.Current.SomePropertyToBeSavedAndWithADefault) { ImGui.BeginDisabled(); }
        var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            //configManager.Current.Save();
        }
        //if (configManager.Current.SomePropertyToBeSavedAndWithADefault) { ImGui.EndDisabled(); }
        
    }

    private void DrawOwners()
    {
        ImGui.TextUnformatted("Aliases");
        ImGui.Separator();

        // Filter
        ImGui.InputText("Filter", ref filterBuffer, 128);

        // Add row
        ImGui.InputText("Character (Name@World)", ref addKeyBuffer, 128);
        ImGui.InputText("Alias", ref addAliasBuffer, 128);

        ImGui.SameLine();
        if (ImGui.Button("Add / Update"))
        {
            if (CharacterKey.TryParse(addKeyBuffer.Trim(), out var key))
            {
                configManager.Current.OwnerChars[key.ToString()] = addAliasBuffer.Trim();
                //configManager.Save();
            }
        }

        ImGui.SameLine();
        CharacterKey? owner;
        if (ImGui.Button("Add from Target"))
        {
            owner = targetingManager.GetTargetedPlayerName();
            if (owner != null)
            {
                var name = owner.Name;
                var world = owner.World;
                addKeyBuffer = $"{name}@{world}";
            }

        }

        ImGui.Spacing();

        // List
        if (ImGui.BeginTable("AliasesTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY,
            new System.Numerics.Vector2(0, 250)))
        {
            ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Alias", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##Actions", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableHeadersRow();

            // Sort keys for stable UI
            foreach (var kv in configManager.Current.OwnerChars.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToList())
            {
                var charKey = kv.Key;
                var alias = kv.Value ?? "";

                if (!string.IsNullOrWhiteSpace(filterBuffer))
                {
                    var f = filterBuffer.Trim();
                    if (!charKey.Contains(f, StringComparison.OrdinalIgnoreCase) &&
                        !alias.Contains(f, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                ImGui.TableNextRow();

                // Character column
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(charKey);

                // Alias column (editable inline)
                ImGui.TableSetColumnIndex(1);
                ImGui.PushID(charKey); // stable per row

                var aliasBuf = alias; // local copy for InputText
                if (ImGui.InputText("##alias", ref aliasBuf, 128))
                {
                    // Update in config on edit (or switch to "Save" button if you prefer)
                    configManager.Current.OwnerChars[charKey] = aliasBuf;
                    //configManager.Save();
                }

                // Actions column
                ImGui.TableSetColumnIndex(2);
                if (ImGui.Button("Delete"))
                {
                    configManager.Current.OwnerChars.Remove(charKey);
                    //configManager.Save();
                    ImGui.PopID();
                    break; // dictionary changed; break out this frame
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private void Tooltip(string text)
    {
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextDisabled($"{FontAwesomeIcon.QuestionCircle.ToIconString()}");
        ImGui.PopFont();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
    }
}
