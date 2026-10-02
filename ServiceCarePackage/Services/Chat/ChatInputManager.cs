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
using static FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.VertexShader;

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
                var stringToProcess = "";

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
                        prefix = regexMatch.Value.TrimEnd();
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

                    // load any command to prefix
                    if (string.IsNullOrEmpty(prefix))
                    {
                        var match = Regex.Match(originalMessage, @"^/\S+");

                        prefix = match.Success ? match.Value : "";
                    }
                }

                log.Debug($"Detouring Message: {originalMessage}");

                stringToProcess = originalMessage.Substring(prefix.Length).TrimStart();

                log.Debug("stringToProcess: " + stringToProcess);

                string? output = "";
                string? processedString;
                if (FixedConfig.CharConfig.EnableTranslate)
                {
                    processedString = translator.Translate(stringToProcess);
                }
                else
                {
                    processedString = stringToProcess;
                }

                if (FixedConfig.CharConfig.CowMode)
                {
                    processedString = Mooify(processedString);
                }

                if (!string.IsNullOrEmpty(processedString))
                {
                    output = string.IsNullOrEmpty(prefix)
                        ? processedString
                        : prefix + " " + processedString;
                }
                else
                {
                    output = prefix;
                }

                log.Debug("Output: " + output);

                if (string.IsNullOrWhiteSpace(output))
                    return; // Do not sent message.

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

        public static string Mooify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            string[] mooWords =
            {
        "moo",
        "mooo",
        "moooo",
        "mooooo",
        "mmoo",
        "mmooo",
        "mmmoo",
        "mrr",
        "mrrr",
        "mrrmoo",
        "mrrrmoo",
        "moomoo",
        "moo moo",
        "mmmooo",
        "MOO",
        "MOOO",
        "Moooo",
        "moo~",
        "mooo~",
        "mrr~"
    };

            string[] emotes =
            {
        ":3",
        ">//<",
        ">_<",
        "^_^",
        ">:3",
        "<3",
        "x3",
        "~~",
        "~"
    };

            var random = Random.Shared;
            var result = new System.Text.StringBuilder();

            while (result.Length < input.Length)
            {
                if (result.Length > 0)
                    result.Append(' ');

                // Mostly moo. Very occasional emote.
                if (random.NextDouble() < 0.93)
                {
                    result.Append(mooWords[random.Next(mooWords.Length)]);
                }
                else
                {
                    result.Append(emotes[random.Next(emotes.Length)]);
                }
            }

            // If original ended like a sentence, optionally finish with an emote.
            char last = input[^1];

            if (last is '.' or '!' or '?')
            {
                // Remove trailing spaces.
                while (result.Length > 0 && result[^1] == ' ')
                    result.Length--;

                result.Append(' ');
                result.Append(emotes[random.Next(emotes.Length)]);
            }

            return result.ToString();
        }
    }
}
