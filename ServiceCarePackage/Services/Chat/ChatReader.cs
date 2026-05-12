using Dalamud.Bindings.ImGui;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Text.Payloads;
using Newtonsoft.Json;
using ServiceCarePackage.Commands;
using ServiceCarePackage.Config;
using ServiceCarePackage.ExtraPayload;
using ServiceCarePackage.Helpers;
using ServiceCarePackage.Models;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.CharaView.Delegates;
using TextPayload = Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload;

namespace ServiceCarePackage.Services.Chat
{
    internal class ChatReader
    {
        private readonly IChatGui clientChat;
        private readonly Configuration config;
        private readonly MessageSender messageSender;
        private readonly ILog log;
        private readonly MoveManager moveManager;
        private readonly IFramework framework;
        private readonly IPlayerState playerState;
        private readonly CommandsHandler commandsHandler;

        private Queue<string> messageQueue = new Queue<string>();
        private Stopwatch messageTimer = new Stopwatch();
        public ChatReader(IChatGui clientChat, Configuration config,
    MessageSender messageSender, IFramework framework, ILog log, MoveManager moveManager, IPlayerState playerState, CommandsHandler commandsHandler)
        {
            this.clientChat = clientChat;
            this.config = config;
            this.messageSender = messageSender;
            this.framework = framework;
            this.log = log;
            this.moveManager = moveManager;
            this.playerState = playerState;
            this.commandsHandler = commandsHandler;

            // Begin our OnChatMessage Detection
            this.clientChat.CheckMessageHandled += Chat_OnCheckMessageHandled;
            //_clientChat.ChatMessage += Chat_OnChatMessage;
            //this.clientChat.ChatMessageHandled += Chat_OnChatMessageHandled;            
            //_clientChat.ChatMessageUnhandled += Chat_OnChatMessageUnhandled;
        }

        public void Dispose()
        {
            clientChat.CheckMessageHandled -= Chat_OnCheckMessageHandled;
            //_clientChat.ChatMessage -= Chat_OnChatMessage;
            //clientChat.ChatMessageHandled -= Chat_OnChatMessageHandled;
            //_clientChat.ChatMessageUnhandled -= Chat_OnChatMessageUnhandled;
        }

        /// <summary> 
        /// This function will determine if we hide an incoming message or not. By default, this handles the hiding of all outgoing encoded tells
        /// <list type="bullet">
        /// <item><c>type</c><param name="type"> - The type of message that was sent.</param></item>
        /// <item><c>senderid</c><param name="senderid"> - The id of the sender.</param></item>
        /// <item><c>sender</c><param name="sender"> - The name of the sender.</param></item>
        /// <item><c>message</c><param name="message"> - The message that was sent.</param></item>
        /// <item><c>isHandled</c><param name="isHandled"> - Whether or not the message was handled.</param></item>
        /// </list> </summary>
        private void Chat_OnCheckMessageHandled(IHandleableChatMessage message)
            //(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            //log.Warning(message.Message.TextValue);
            var chatTypes = new[] { XivChatType.TellIncoming, XivChatType.TellOutgoing };
            var localPlayerName = playerState.CharacterName;
            CharacterKey? senderKey = null;
            var selfMessage = Regex.Match(message.Sender.TextValue, $@"(?i)^\W?{localPlayerName}$").Success;            
            
            if (message.Sender.Payloads.Count > 1)
            {
                senderKey = message.Sender.TryGetSenderNameAndWorld(playerState);
            }
            var isSenderOwner = FixedConfig.CharConfig.OwnerChars.ContainsKey(senderKey?.ToString() ?? "");

            //_log.Debug($"Sender: {sender.TextValue} type: {type}:{Enum.GetName(typeof(XivChatType), type)} message: {message.TextValue}");
            var originalSender = message.Sender;

            if ((int)message.LogKind <= 107 && selfMessage)
            {
                if (FixedConfig.CharConfig.EnableAliasNameChanger)
                {
                    SwapNameToAlias(message.Sender, FixedConfig.CharConfig.AliasColorHex, localPlayerName, FixedConfig.DisplayName);
                }
            }

            var ctx = new ChatCommandContext(message.LogKind, message.Timestamp, senderKey, message.Sender, message.Message.TextValue.Trim(), isSenderOwner);
            var runner = Task.Run(() => commandsHandler.TryDispatchAsync(ctx));
            runner.Wait();
            if (runner.Result)
            {
                message.PreventOriginal();
                return;
            }

            //Chat hider
            //log.Debug($"{FixedConfig.IsActive_ChatHider} {message.Payloads.Count == 1} {message.TextValue.Contains(FixedConfig.CommandName)} {message.Payloads[0] is TextPayload}");
            if (FixedConfig.IsActive_ChatHider
                && !(message.LogKind == XivChatType.TellOutgoing)
                && message.Message.Payloads.Count == 1
                && !message.Message.TextValue.Contains(FixedConfig.CommandName)
                && message.Message.Payloads[0] is TextPayload tp)
            {
                if (tp.Text != null)
                {
                    char[] replacements = { '–', '┓', '┗', '┐', '└', '┏', '┛', '┘', '┌', '├', '┝', '┥', '┤', '┣', '┠', '┨', '┫', '┰', '┯', '┬', '┳', '┴', '┷', '┼', '┻', '┸', '┿', '╂', '╂', '┿', '╋'};
                    var rng = new Random();
                    char[] buffer = new char[tp.Text.Length];
                    for (int i = 0; i < tp.Text.Length; i++)
                    {
                        char c = tp.Text[i];
                        buffer[i] = char.IsWhiteSpace(c) ? c : replacements[rng.Next(replacements.Length)];
                    }

                    tp.Text = new string(buffer);
                }
                
            }

            /*if (!selfMessage && ((int)type < 56 || ((int)type > 71 && (int)type < 108)) && (int)type != 12)
            {
                var regCommand = string.Empty;

                if (senderKey != null && FixedConfig.PuppetMasterHArdcore && isSenderOwner)
                {
                    regCommand = FixedConfig.CommandRegexFull;
                }
                else
                {
                    regCommand = FixedConfig.CommandRegex;
                }

                var match = Regex.Match(message.TextValue.Trim(), regCommand);
                if (match.Success)
                {
                    var matched = match.Groups[2].Value;
                    if (FixedConfig.PuppetMasterHArdcore)
                    {
                        matched = matched.Replace('[', '<').Replace(']', '>');
                    }
                    log.Debug($"match.Success {matched}");
                    clientChat.Print(new SeStringBuilder().AddItalicsOn().AddUiForeground(31).AddText($"[{sender.TextValue}]").AddUiForegroundOff()
                        .AddText($" forced you to {matched}.")
                        .AddItalicsOff().BuiltString);

                    Task.Run(() =>
                    {
                        moveManager.DisableMovingFor(5100);
                        Thread.Sleep(100);
                        messageSender.SendMessage($"/{matched}");
                    });

                    isHandled = true;
                    return;
                }
            }*/

            // Apply aliases for others
            if ((int)message.LogKind <= 107 && senderKey != null)
            {
                var alias = FixedConfig.AliasDataUnion.Where(x => x.Key.Equals(senderKey.ToString()));
                if (alias.Any())
                {
                    var first = alias.First();

                    SwapNameToAlias(message.Sender, first.Value.ColorHex, senderKey.Name, first.Value.Alias);
                }
            }
        }

        private List<Payload> RenameAndRecolor(List<Payload> payloads, Vector3 color, string originalName, string? nameAlias = null)
        {
            //log.Debug($"{originalName} {nameAlias}");
            var newPayloads = new List<Payload>();

            foreach (var p in payloads)
            {
                if (p is PlayerPayload pp && Regex.Match(pp.PlayerName, $@"(?i)^\W?{originalName}$").Success)
                {
                    newPayloads.Add(pp);
                    continue;
                }
                if (p is TextPayload tp && tp.Text != null && tp.Text.Equals(originalName, StringComparison.Ordinal))
                {
                    var newName = nameAlias ?? tp.Text;
                    newPayloads.Add(new ColorPayload(color).AsRaw());
                    newPayloads.Add(new TextPayload(newName));          // fresh payload
                    newPayloads.Add(new ColorEndPayload().AsRaw());
                    continue;
                }
            }
            return newPayloads;
        }

        private void SwapNameToAlias(SeString sender, string? colorHex, string originalName, string? alias = null)
        {
            if (alias.IsNullOrEmpty()) { alias = null; }
            if (colorHex.IsNullOrEmpty()) { colorHex = "#FFFFFF"; }
            var newPayloads = RenameAndRecolor(sender.Payloads, colorHex.HexToVector3Rgb(), originalName, alias);
            //log.Error(sender.ToJson());
            sender.Payloads.Clear();
            sender.Payloads.AddRange(newPayloads);
            //log.Error(sender.ToJson());
        }
    }
}
