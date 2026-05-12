using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ServiceCarePackage.Config;
using ServiceCarePackage.Models;
using ServiceCarePackage.Services.Chat;
using System;
using System.Linq;
using System.Numerics;

namespace ServiceCarePackage.Windows;

internal class MainWindow : Window, IDisposable
{
    private Configuration configuration { get; }
    private ConfigManager configManager { get; }
    private SettingsWindow settingsWindow { get; }
    private MessageSender messageSender { get; }

    private string commandNameBuffer = string.Empty;
    private string commandTextBuffer = string.Empty;
    private bool advancedCommandBuffer = true;
    private string? selectedCommandName;
    private string? selectedCharacterKey;
    private string? selectedCharacterValue;
    private string selectedChatChannel = "say";
    private string sendCommandPreviewBuffer = string.Empty;

    // Values are slash-command channel names without the leading slash, so they can be
    // used directly when building the command to send, e.g. $"/{selectedChatChannel} ...".
    //
    // FFXIV exposes the localized chat command strings through game data, not through a
    // simple stable ClientStruct enum. If this window gets DataManager injected later, this
    // list can be replaced by reading the command sheet and filtering to chat commands.
    private static readonly string[] ChatChannels = BuildChatChannels();

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    internal MainWindow(Configuration configuration, SettingsWindow settingsWindow, 
        ConfigManager configManager, MessageSender messageSender)
        : base("Miki Mod Workshop##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.configuration = configuration;
        this.settingsWindow = settingsWindow;
        this.configManager = configManager;
        this.messageSender = messageSender;
    }

    public void Dispose() { }

    private static string[] BuildChatChannels()
    {
        var channels = new[]
        {
            "tell",
            "say",
            "yell",
            "shout",
            "party",
            "alliance",
            "fc",
            "pvpteam",
            "novice"
        }
        .Concat(Enumerable.Range(1, 8).Select(i => $"ls{i}"))
        .Concat(Enumerable.Range(1, 8).Select(i => $"cwl{i}"));

        return channels.ToArray();
    }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            settingsWindow.IsOpen = true;
        }

        ImGui.Spacing();
        DrawSavedCommandsArea();
    }

    private void DrawSavedCommandsArea()
    {
        ImGui.TextUnformatted("Saved Commands");
        ImGui.Separator();

        ImGui.SetNextItemWidth(300);
        ImGui.InputText("Command name", ref commandNameBuffer, 128);

        ImGui.SetNextItemWidth(500);
        ImGui.InputTextMultiline("Command text", ref commandTextBuffer, 512, new Vector2(500, 80));

        ImGui.Checkbox("Advanced command", ref advancedCommandBuffer);

        if (ImGui.Button(selectedCommandName is null ? "Save command" : "Save command changes"))
        {
            SaveCommandFromInputs();
            configManager.Save();
        }

        if (selectedCommandName is not null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Clear selection"))
            {
                ClearCommandInputs();
            }
        }

        DrawCommandPopups();

        ImGui.Spacing();
        DrawSavedCommandsTable();

        ImGui.Spacing();
        DrawCharacterDropdown();
        DrawSendArea();
    }

    private void SaveCommandFromInputs()
    {
        var commandName = commandNameBuffer.Trim();
        var commandText = commandTextBuffer.Trim();

        if (commandName.Length == 0)
        {
            ImGui.OpenPopup("CommandNameRequired");
            return;
        }

        if (commandText.Length == 0)
        {
            ImGui.OpenPopup("CommandTextRequired");
            return;
        }

        // Command name is the unique identifier. If the user selected an existing row
        // and changed its name, remove the old key before saving under the new key.
        if (selectedCommandName is not null
            && !string.Equals(selectedCommandName, commandName, StringComparison.OrdinalIgnoreCase))
        {
            configManager.Current.SavedCommands.Remove(selectedCommandName);
        }

        configManager.Current.SavedCommands[commandName] = new SavedCommandData
        {
            CommandText = commandText,
            AdvancedCommand = advancedCommandBuffer
        };

        selectedCommandName = commandName;
        // Uncomment if you want every button click to persist immediately instead of relying on SettingsWindow's Save settings button.
        // configManager.Save();
    }

    private void DrawSavedCommandsTable()
    {
        if (ImGui.BeginTable("SavedCommandsTable", 4,
            ImGuiTableFlags.RowBg
            | ImGuiTableFlags.Borders
            | ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.ScrollX
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp,
            new Vector2(0, 500)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("Command name", ImGuiTableColumnFlags.WidthStretch, 1.0f);
            ImGui.TableSetupColumn("Command text", ImGuiTableColumnFlags.WidthStretch, 2.5f);
            ImGui.TableSetupColumn("Advanced", ImGuiTableColumnFlags.WidthFixed, 90);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 90);
            ImGui.TableHeadersRow();

            foreach (var kv in configManager.Current.SavedCommands.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToList())
            {
                var commandName = kv.Key;
                var command = kv.Value ?? new SavedCommandData();
                var isSelected = string.Equals(selectedCommandName, commandName, StringComparison.OrdinalIgnoreCase);

                ImGui.TableNextRow();
                ImGui.PushID(commandName);

                ImGui.TableSetColumnIndex(0);
                if (ImGui.Selectable(commandName, isSelected))
                {
                    LoadCommandIntoInputs(commandName, command);
                }

                ImGui.TableSetColumnIndex(1);
                if (ImGui.Selectable(command.CommandText ?? string.Empty, isSelected))
                {
                    LoadCommandIntoInputs(commandName, command);
                }

                ImGui.TableSetColumnIndex(2);
                if (ImGui.Selectable(command.AdvancedCommand ? "Yes" : "No", isSelected))
                {
                    LoadCommandIntoInputs(commandName, command);
                }

                ImGui.TableSetColumnIndex(3);
                if (ImGui.Button("Delete"))
                {
                    configManager.Current.SavedCommands.Remove(commandName);
                    if (isSelected)
                    {
                        ClearCommandInputs();
                    }
                    // configManager.Save();
                    ImGui.PopID();
                    configManager.Save();
                    break;
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private void DrawCharacterDropdown()
    {
        var characters = configManager.Current.OwnerChars
            .Union(configManager.Current.OtherChars)
            .ToDictionary()
            .OrderBy(k => k.Value?.Alias ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selectedCharacterKey is null && characters.Count > 0)
        {
            selectedCharacterKey = characters[0].Key;
        }

        var preview = selectedCharacterKey ?? "Select character";
        if (selectedCharacterKey is not null && characters.FirstOrDefault(k => k.Key == selectedCharacterKey) is var selected)
        {
            preview = FormatCharacterDropdownLabel(selected.Key, selected.Value);
        }

        ImGui.SetNextItemWidth(450);
        if (ImGui.BeginCombo("Character", preview))
        {
            foreach (var kv in characters)
            {
                var label = FormatCharacterDropdownLabel(kv.Key, kv.Value);
                var isSelected = string.Equals(selectedCharacterKey, kv.Key, StringComparison.OrdinalIgnoreCase);

                if (ImGui.Selectable(label, isSelected))
                {
                    selectedCharacterKey = kv.Key;
                    selectedCharacterValue = kv.Value.Alias ?? string.Empty;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(220);
        if (ImGui.BeginCombo("Chat channel", selectedChatChannel))
        {
            foreach (var channel in ChatChannels)
            {
                var isSelected = string.Equals(selectedChatChannel, channel, StringComparison.OrdinalIgnoreCase);

                if (ImGui.Selectable(channel, isSelected))
                {
                    selectedChatChannel = channel;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
    }

    private void DrawSendArea()
    {
        ImGui.Spacing();

        sendCommandPreviewBuffer = 
            $"/{selectedChatChannel}{(selectedChatChannel.Equals("tell")?$" {selectedCharacterKey}":"")} {selectedCharacterValue?.ToLower()}, {(advancedCommandBuffer ? "(":"")}{commandTextBuffer}{(advancedCommandBuffer ? ")" : "")}";

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextMultiline(
            "Command to send",
            ref sendCommandPreviewBuffer,
            512,
            new Vector2(0, 80));

        if (ImGui.Button("Send"))
        {
            messageSender.SendMessageEnqueue(sendCommandPreviewBuffer);
        }
    }

    private void LoadCommandIntoInputs(string commandName, SavedCommandData command)
    {
        selectedCommandName = commandName;
        commandNameBuffer = commandName;
        commandTextBuffer = command.CommandText ?? string.Empty;
        advancedCommandBuffer = command.AdvancedCommand;
    }

    private static string FormatCharacterDropdownLabel(string characterKey, CharData? charData)
    {
        var alias = string.IsNullOrWhiteSpace(charData?.Alias) ? characterKey : charData!.Alias.Trim();
        return $"{alias} ({characterKey})";
    }

    private void DrawCommandPopups()
    {
        if (ImGui.BeginPopupModal("CommandNameRequired", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Command name is required.");
            ImGui.Separator();

            if (ImGui.Button("OK"))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopupModal("CommandTextRequired", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Command text is required.");
            ImGui.Separator();

            if (ImGui.Button("OK"))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }
    }

    private void ClearCommandInputs()
    {
        selectedCommandName = null;
        commandNameBuffer = string.Empty;
        commandTextBuffer = string.Empty;
        advancedCommandBuffer = true;
    }
}
