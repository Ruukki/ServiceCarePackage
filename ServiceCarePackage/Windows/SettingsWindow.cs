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

    private string addKeyBuffer = "";
    private string addAliasBuffer = "";
    private string filterBuffer = "";
    private string addColorBuffer = "#FFFFFF";


    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    internal SettingsWindow(ILog log, Configuration configuration, TargetingManager targetingManager, ConfigManager configManager) 
        : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoScrollbar |
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
    }

    public override void OnClose()
    {
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

        ImGui.Text($"Loaded configuration for: {configManager.CurrentKey}");
        ImGui.Text($"Current access level: {configManager.Current.SettingLockLevels}");

        if (ImGui.BeginTabBar("Outer"))
        {
            if (ImGui.BeginTabItem("Features"))
            {
                #region FullLock
                if (configManager.Current.SettingLockLevels == Enums.SettingLockLevels.FulLock) { ImGui.BeginDisabled(); }

                bool enableTranslate = configManager.Current.EnableTranslate;
                if (ImGui.Checkbox("Translate", ref enableTranslate))
                {
                    configManager.Current.EnableTranslate = enableTranslate;
                }
                Tooltip("Helps you be extra cute and silly by swappign those pest first person pronouns to third person");

                bool enablePuppetMasterHc = configManager.Current.EnablePuppetMasterHadcore;
                if (ImGui.Checkbox("Hardcore puppet master", ref enablePuppetMasterHc))
                {
                    configManager.Current.EnablePuppetMasterHadcore = enablePuppetMasterHc;
                }
                Tooltip("Enables puppet master mathching for full commands.\n"
                                   + "Allows use of multi word/argument commands (replace '<>' with '[]').\n"
                                   + "name, target [me]");

                if (configManager.Current.SettingLockLevels == Enums.SettingLockLevels.FulLock) { ImGui.EndDisabled(); }
                #endregion

                #region BasicLock
                if (configManager.Current.SettingLockLevels >= Enums.SettingLockLevels.BasicLock) { ImGui.BeginDisabled(); }

                bool enablePuppetMaster = configManager.Current.EnablePuppetMaster;
                if (ImGui.Checkbox("PuppetMaster+", ref enablePuppetMaster))
                {
                    configManager.Current.EnablePuppetMaster = enablePuppetMaster;
                }
                Tooltip("PuppetMaster but better, forces you to stop and do the commands");

                bool enableForcedWalk = configManager.Current.EnableForcedWalk;
                if (ImGui.Checkbox("Forced walk", ref enableForcedWalk))
                {
                    configManager.Current.EnableForcedWalk = enableForcedWalk;
                }
                Tooltip("Goodgirls shouldn't be running anyway in those heels");

                bool enableAliasNameChanger = configManager.Current.EnableAliasNameChanger;
                if (ImGui.Checkbox("Enable chat alias", ref enableAliasNameChanger))
                {
                    configManager.Current.EnableAliasNameChanger = enableAliasNameChanger;
                }
                Tooltip("Swaps your boring FF name in chat to your new much mroe fun one (client side only)");

                if (configManager.Current.SettingLockLevels >= Enums.SettingLockLevels.BasicLock) { ImGui.EndDisabled(); }
                #endregion


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

                var colorBuf = configManager.Current.AliasColorHex ?? "#FFFFFF";
                //ImGui.SetNextItemWidth();
                if (ImGui.InputText("Color for chat alias##color", ref colorBuf, 16,
                    ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsNoBlank))
                {
                    configManager.Current.AliasColorHex = NormalizeHex(colorBuf);
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("About owner"))
            {
                DrawOwners();                

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
        

        /*using (var child = ImRaii.Child("AboutYou", new Vector2(800, 150), true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                
            }
        }
        ImGui.Spacing();*/

        //if (configManager.Current.SomePropertyToBeSavedAndWithADefault) { ImGui.BeginDisabled(); }
        /*var movable = configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Movable Config Window", ref movable))
        {
            configuration.IsConfigWindowMovable = movable;
            //configManager.Current.Save();
        }*/
        //if (configManager.Current.SomePropertyToBeSavedAndWithADefault) { ImGui.EndDisabled(); }
        
    }

    private void DrawOwners()
    {
        ImGui.TextUnformatted("Aliases");
        ImGui.Separator();

        // Filter
        ImGui.InputText("Filter", ref filterBuffer, 128);
        Tooltip("Search for owner char list");

        // Add row
        ImGui.InputText("Character (Name@World)", ref addKeyBuffer, 128);
        ImGui.InputText("Alias", ref addAliasBuffer, 128);

        ImGui.SameLine();
        if (ImGui.Button("Add / Update"))
        {
            if (CharacterKey.TryParse(addKeyBuffer.Trim(), out var key) && key is not null)
            {
                bool worldExists = configManager.Current.OwnerChars.Keys.Any(k =>
                    CharacterKey.TryParse(k, out var existing) &&
                    existing is not null &&
                    existing.World.Equals(key.World, StringComparison.OrdinalIgnoreCase));

                if (!worldExists)
                {
                    configManager.Current.OwnerChars[key.ToString()] = new CharData
                    {
                        Alias = addAliasBuffer.Trim(),
                        ColorHex = NormalizeHex(addColorBuffer)
                    };

                    // configManager.Save();
                }
                else
                {
                    // Optional: show feedback
                    ImGui.OpenPopup("World already exists");
                }
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
        if (ImGui.BeginTable("AliasesTable", 4,
    ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY,
    new System.Numerics.Vector2(0, 250)))
        {
            ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Alias", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 110);
            ImGui.TableSetupColumn("##Actions", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableHeadersRow();

            foreach (var kv in configManager.Current.OwnerChars.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToList())
            {
                var charKey = kv.Key;
                var entry = kv.Value ?? new CharData();

                ImGui.TableNextRow();

                // Character
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(charKey);

                ImGui.PushID(charKey);

                // Alias editable
                ImGui.TableSetColumnIndex(1);
                var aliasBuf = entry.Alias ?? "";
                if (ImGui.InputText("##alias", ref aliasBuf, 128))
                {
                    entry.Alias = aliasBuf;
                    //configManager.Save();
                }

                // Color editable (hex)
                ImGui.TableSetColumnIndex(2);
                var colorBuf = entry.ColorHex ?? "#FFFFFF";
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##color", ref colorBuf, 16,
                    ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsNoBlank))
                {
                    entry.ColorHex = NormalizeHex(colorBuf);
                    //configManager.Save();
                }

                // Actions
                ImGui.TableSetColumnIndex(3);
                if (ImGui.Button("Delete"))
                {
                    configManager.Current.OwnerChars.Remove(charKey);
                    //configManager.Save();
                    ImGui.PopID();
                    break;
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private static string NormalizeHex(string s)
    {
        s = (s ?? "").Trim();

        if (s.Length == 0) return "#FFFFFF";
        if (s[0] != '#') s = "#" + s;

        // Allow #RGB -> #RRGGBB
        if (s.Length == 4)
            s = $"#{s[1]}{s[1]}{s[2]}{s[2]}{s[3]}{s[3]}";

        // Clamp to #RRGGBB or #AARRGGBB
        if (s.Length > 9) s = s[..9];
        if (s.Length != 7 && s.Length != 9) return "#FFFFFF";

        // Ensure hex chars
        for (int i = 1; i < s.Length; i++)
            if (!Uri.IsHexDigit(s[i])) return "#FFFFFF";

        return s.ToUpperInvariant();
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
