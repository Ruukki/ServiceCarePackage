using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
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
        [Signature(Signatures.ProcessChatInput, DetourName = nameof(ProcessChatInputDetour), Fallibility = Fallibility.Auto)]
        private Hook<ProcessChatInputDelegate> ProcessChatInputHook { get; set; } = null!;

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

        private unsafe byte ProcessChatInputDetour(IntPtr uiModule, byte** message, IntPtr a3)
        {
            try
            {
                // Grab the original string.
                var originalSeString = MemoryHelper.ReadSeStringNullTerminated((nint)(*message));
                var messageDecoded = originalSeString.ToString();

                // Debug the output (remove later)
                /*foreach (var payload in originalSeString.Payloads)
                {
                    log.Debug($"Message Payload [{payload.Type}]: {payload.ToString()}");
                }
                log.Debug($"Message Payload Present");*/

                if (string.IsNullOrWhiteSpace(messageDecoded))
                    return ProcessChatInputHook.Original(uiModule, message, a3);

                // If we are not meant to garble the message, then return original.
                if (false)
                    return ProcessChatInputHook.Original(uiModule, message, a3);

                /* -------------------------- MUFFLERCORE / GAGSPEAK CHAT GARBLER TRANSLATION LOGIC -------------------------- */
                // Firstly, make sure that we are setup to allow garbling in the current channel.
                var prefix = string.Empty;
                var tellName = string.Empty;
                var tellWorld = string.Empty;
                InputChannel channel = 0;
                //var muffleMessage = g.AllowedGarblerChannels.IsActiveChannel((int)ChatLogAgent.CurrentChannel());

                // It's possible to be in a channel (ex. Say) but send (/party Hello World), we must check this.
                if (messageDecoded.StartsWith("/"))
                {
                    // If its a command outside a chatChannel command, return original.
                    /*if (!ChatLogAgent.IsPrefixForGsChannel(messageDecoded, out prefix, out channel))
                        return ProcessChatInputHook.Original(uiModule, message, a3);*/

                    // Handle Tells, these are special, use advanced Regex to protect name mix-up
                    if (channel is InputChannel.Tell_In)
                    {
                        log.Debug($"[Chat Processor]: Matched Command is a tell command");
                        // Using /gag command on yourself sends /tell which should be caught by this
                        // Depends on the message to start like :"/tell {targetPlayer} *{playerPayload.PlayerName}"
                        // Since only outgoing tells are affected, {targetPlayer} and {playerPayload.PlayerName} will be the same
                        var selfTellRegex = @"(?<=^|\s)/t(?:ell)?\s{1}(?<name>\S+\s{1}\S+)@\S+\s{1}\*\k<name>(?=\s|$)";

                        //log.Debug($"{Regex.Match(messageDecoded, selfTellRegex).Value}");
                        // If the condition is not matched here, it means we are performing a self-tell (someone is messaging us), so return original.
                        if (!Regex.Match(messageDecoded, selfTellRegex).Value.IsNullOrEmpty())
                        {
                            log.Debug("[Chat Processor]: Ignoring Message as it is a self tell garbled message.");
                            //return ProcessChatInputHook.Original(uiModule, message, a3);
                        }


                        // Match any other outgoing tell to preserve target name
                        var tellRegex = @"(?<=^|\s)/t(?:ell)?\s{1}(?:(\S+\s?\S+)@(\S+)|\<r\>)\s?(?=\S|\s|$)";
                        var regexMatch = Regex.Match(messageDecoded, tellRegex);
                        prefix = regexMatch.Value;
                        tellName = regexMatch.Groups[1].Value;
                        tellWorld += regexMatch.Groups[2].Value;
                    }
                    //log.Debug($"Matched Command [{prefix}] [{tellName}] for [{channel}]");

                    //Restore swapped alias names
                    var recoveredName = string.Empty;
                    CharData recoveredData = new();

                    if (TryFindByAliasAndWorld(FixedConfig.CharConfig.OwnerChars, tellName, tellWorld, out recoveredName, out recoveredData))
                    {
                        prefix = prefix.Replace($"{tellName}@{tellWorld}", recoveredName);
                    }

                    // Finally if we reached this point, update `muffleAllowedForChannel` to reflect the intended channel.
                    //muffleMessage = g.AllowedGarblerChannels.IsActiveChannel((int)channel);
                }

                // If it's not allowed, do not garble.
                if (/*muffleMessage*/true)
                {
                    log.Debug($"Detouring Message: {messageDecoded}");

                    // only obtain the text payloads from this message, as nothing else should madder.
                    var textPayloads = originalSeString.Payloads.OfType<TextPayload>().ToList();
                    // merge together the text of all the split text payloads.
                    var originalText = string.Join("", textPayloads.Select(tp => tp.Text));
                    // Get the string to garble starting after the prefix text.
                    var stringToProcess = originalText.Substring(prefix.Length);
                    // set the output to the prefix + the garbled message.
                    /*var output = string.IsNullOrEmpty(prefix)
                        ? _muffler.ProcessMessage(stringToProcess)
                        : prefix + " " + _muffler.ProcessMessage(stringToProcess);*/

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
                        return 0; // Do not sent message.

                    log.Debug("Output: " + output);
                    var newSeString = new SeStringBuilder().Add(new TextPayload(output)).Build();

                    // Verify its a legal width
                    if (newSeString.TextValue.Length <= 500)
                    {
                        var utf8String = Utf8String.FromString(".");
                        utf8String->SetString(newSeString.Encode());
                        return ProcessChatInputHook.Original(uiModule, (byte**)((nint)utf8String).ToPointer(), a3);
                    }
                    else
                    {
                        log.Error("Chat Garbler Variant of Message was longer than max message length!");
                        return ProcessChatInputHook.Original(uiModule, message, a3);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error($"Error sending message to chat box (secondary): {e}");
            }

            // return the original message untranslated
            return ProcessChatInputHook.Original(uiModule, message, a3);
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
