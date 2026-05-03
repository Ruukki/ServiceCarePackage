using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Shell;
using Serilog.Core;
using ServiceCarePackage.Config;
using ServiceCarePackage.Helpers;
using ServiceCarePackage.Models;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Translator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ServiceCarePackage.Services.Chat
{
    internal class ChatInputManager : IDisposable
    {
        private unsafe delegate byte ProcessChatInputDelegate(IntPtr uiModule, byte** message, IntPtr a3);
        private unsafe delegate void ProcessChatInputDelegateNew(ShellCommandModule* uiModule, Utf8String* message, UIModule* a3);
        [Signature(Signatures.ProcessChatInput, DetourName = nameof(ProcessChatInputDetour), Fallibility = Fallibility.Auto)]
        private Hook<ProcessChatInputDelegateNew> ProcessChatInputHook { get; set; } = null!;

        private ILog log;
        private ITranslator translator;
        private IPlayerState playerState;
        internal ChatInputManager(ILog log, IGameInteropProvider gameInteropProvider, ITranslator translator, IPlayerState playerState)
        {
            this.log = log;
            this.translator = translator;
            gameInteropProvider.InitializeFromAttributes(this);
            this.playerState = playerState;
        }

        internal void EnableHooks()
        {
            log.Debug("Enabling hook");
            ProcessChatInputHook.SafeEnable();
            if (ProcessChatInputHook != null)
            {
                log.Debug("IsEnabled: " + ProcessChatInputHook.IsEnabled.ToString());
            }
        }

        internal void Dispose()
        {
            ProcessChatInputHook.SafeDisable();
            ProcessChatInputHook.SafeDispose();
        }

        private unsafe void ProcessChatInputDetour(ShellCommandModule* uiModule, Utf8String* message, UIModule* a3)
        {
            try
            {
                //log.Warning(message->ToString());
                //message->SetString(translator.Translate(message->ToString()));
                var originalMessage = message->ToString();

                if (string.IsNullOrWhiteSpace(originalMessage))
                {
                    ProcessChatInputHook.Original(uiModule, message, a3);
                    return;
                }

                var prefix = string.Empty;
                var tellName = string.Empty;
                var tellWorld = string.Empty;
                InputChannel channel = 0;

                if (originalMessage.StartsWith("/"))
                {
                    if (channel is InputChannel.Tell_In)
                    {
                        // Match any other outgoing tell to preserve target name
                        var tellRegex = @"(?<=^|\s)/t(?:ell)?\s{1}(?:(\S+\s?\S+)@(\S+)|\<r\>)\s?(?=\S|\s|$)";
                        var regexMatch = Regex.Match(originalMessage, tellRegex);
                        prefix = regexMatch.Value;
                        tellName = regexMatch.Groups[1].Value;
                        tellWorld += regexMatch.Groups[2].Value;
                    }

                    //Restore swapped alias names
                    var recoveredName = string.Empty;
                    CharData recoveredData = new();

                    if (TryFindByAliasAndWorld(FixedConfig.AliasDataUnion, tellName, tellWorld, out recoveredName, out recoveredData))
                    {
                        prefix = prefix.Replace($"{tellName}@{tellWorld}", recoveredName);
                    }
                }

                log.Debug($"Detouring Message: {originalMessage}");
                
                var stringToProcess = originalMessage.Substring(prefix.Length);

                string? output;
                if (FixedConfig.CharConfig.EnableTranslate)
                {
                    output = string.IsNullOrEmpty(prefix)
                        ? translator.Translate(stringToProcess)
                        : prefix + " " + translator.Translate(stringToProcess);
                }
                else
                {
                    output = string.IsNullOrEmpty(prefix)
                        ? stringToProcess
                        : prefix + " " + stringToProcess;
                }

                if (string.IsNullOrWhiteSpace(output))
                    return; // Do not sent message.

                log.Debug("Output: " + output);

                // Verify its a legal width
                if (output.Length <= 500)
                {
                    message->SetString(output);
                }
                else
                {
                    log.Error("Chat Garbler Variant of Message was longer than max message length!");
                }

                ProcessChatInputHook.Original(uiModule, message, a3);
                return;

            }
            catch (Exception e)
            {
                log.Error($"Error sending message to chat box (secondary): {e}");
            }

        }

        void IDisposable.Dispose()
        {
            Dispose();
        }

        private bool TryFindByAliasAndWorld(
            Dictionary<string, CharData> ownerChars,
            string alias,
            string world,
            out string nameAtWorldKey,
            out CharData data)
        {
            foreach (var kv in ownerChars)
            {
                if (kv.Value?.Alias == null)
                    continue;

                // Parse "Name@World" key
                if (!CharacterKey.TryParse(kv.Key, out var ck))
                    continue;

                if (ck.World.Equals(world, StringComparison.OrdinalIgnoreCase) &&
                    kv.Value.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                {
                    nameAtWorldKey = kv.Key;
                    data = kv.Value;
                    return true;
                }
            }

            nameAtWorldKey = "";
            data = null!;
            return false;
        }
    }
}
