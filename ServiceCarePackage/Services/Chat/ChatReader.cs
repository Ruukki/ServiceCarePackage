using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Text.Payloads;
using Newtonsoft.Json;
using ServiceCarePackage.Config;
using ServiceCarePackage.ExtraPayload;
using ServiceCarePackage.Helpers;
using ServiceCarePackage.Models;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private Queue<string> messageQueue = new Queue<string>();
        private Stopwatch messageTimer = new Stopwatch();
        public ChatReader(IChatGui clientChat, Configuration config,
    MessageSender messageSender, IFramework framework, ILog log, MoveManager moveManager, IPlayerState playerState)
        {
            this.clientChat = clientChat;
            this.config = config;
            this.messageSender = messageSender;
            this.framework = framework;
            this.log = log;
            this.moveManager = moveManager;
            this.playerState = playerState;

            // begin our framework check
            this.framework.Update += framework_Update;
            // Begin our OnChatMessage Detection
            this.clientChat.CheckMessageHandled += Chat_OnCheckMessageHandled;
            //_clientChat.ChatMessage += Chat_OnChatMessage;
            this.clientChat.ChatMessageHandled += Chat_OnChatMessageHandled;
            //_clientChat.ChatMessageUnhandled += Chat_OnChatMessageUnhandled;
        }

        public void Dispose()
        {
            framework.Update -= framework_Update;
            clientChat.CheckMessageHandled -= Chat_OnCheckMessageHandled;
            //_clientChat.ChatMessage -= Chat_OnChatMessage;
            clientChat.ChatMessageHandled -= Chat_OnChatMessageHandled;
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
        private void Chat_OnCheckMessageHandled(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            var chatTypes = new[] { XivChatType.TellIncoming, XivChatType.TellOutgoing };
            var originalName = playerState.CharacterName;

            if (sender.Payloads.Count > 1)
            {
                var xx = TryGetSenderNameAndWorld(sender);
                log.Debug($"{xx.Name}@{xx.World}");
            }

            //_log.Debug($"Sender: {sender.TextValue} type: {type}:{Enum.GetName(typeof(XivChatType), type)} message: {message.TextValue}");
            var originalSender = sender;
            if ((int)type <= 107 && sender.TextValue.Equals(originalName))
            {
                /*var senderJson = sender.ToJson();
                senderJson = senderJson.Replace(originalName, FixedConfig.DisplayName);
                var newSender = JsonConvert.DeserializeObject<SeString>(senderJson, new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                });*/

                var newPayloads = new List<Payload>(RenameAndRecolor(sender.Payloads, new Vector3(1, 0, 0), originalName, FixedConfig.DisplayName));
                sender.Payloads.Clear();
                sender.Payloads.AddRange(newPayloads);                
            }
            //Alias for others
            if ((int)type <= 107 && sender.TextValue.Equals("Eveli Harukawa"))
            {
                /*var senderJson = sender.ToJson();
                //Fix with config
                senderJson = senderJson.Replace("Eveli Harukawa", "Miss");
                var newSender = JsonConvert.DeserializeObject<SeString>(senderJson, new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
                });*/

                var newPayloads = new List<Payload>(RenameAndRecolor(sender.Payloads, ColorHelper.HexToVector3Rgb("#76cd26"), "Eveli Harukawa", "Miss"));

                sender.Payloads.Clear();
                sender.Payloads.AddRange(newPayloads);
            }

            // if the message is incoming tell
            if (((int)type < 56 || ((int)type > 71 && (int)type < 108)) && (int)type != 12)
            {
                //_log.Debug($"Chat_OnCheckMessageHandled {message.TextValue}");
                var match = Regex.Match(message.TextValue.Trim(), FixedConfig.CommandRegex);
                if (match.Success)
                {
                    var matched = match.Groups[2].Value;
                    if (true/*config.UseFullMatch*/)
                    {
                        matched = matched.Replace('[', '<').Replace(']', '>');
                    }
                    log.Debug($"match.Success {matched}");
                    clientChat.Print(new SeStringBuilder().AddItalicsOn().AddUiForeground(31).AddText($"{sender.TextValue}").AddUiForegroundOff()
                        .AddText($" forced you to {matched}.")
                        .AddItalicsOff().BuiltString);

                    messageSender.SendMessage($"/{matched}");
                    moveManager.DisableMovingFor(5000);

                    /*switch (matched)
                    {
                        case "stay":
                            if (config.tester) moveManager.DisableMoving();
                            break;
                        case "move":
                            if (config.tester) moveManager.EnableMoving();
                            break;
                        case "shutup":
                            if (config.tester) config.chatMuted = true;
                            break;
                        case "speak":
                            if (config.tester) config.chatMuted = false;
                            break;
                        default:
                            moveManager.DisableMoving();

                            Task.Run(() =>
                            {
                                Thread.Sleep(100);
                                if (config.SuperSecretFeature)
                                {
                                    _plugService?.Vibrate(5, 0.1);
                                }
                                //_messageSender.SendMessage($"/{matched}");
                                messageSender.SendMessage($"/{matched}");

                                Thread.Sleep(5000);
                                moveManager.EnableMoving();
                                log.Debug($"ChatManager enableMovement");
                            });
                            break;
                    }*/

                    // if it does, hide it from the chat log
                    isHandled = true;
                    return;
                }
                //_log.Debug($"newSender: {sender.TextValue}");
                //_log.Debug($"ChatManage Type: {type} _config.OwnerName: {_config.OwnerName} {_config.ForcedChat}, Sender: {originalSender.TextValue}");
                // remove first nonletter symbol, because friendlist bookmark is visible in name
                /*if (config.ForcedChat && Regex.Replace(originalSender.TextValue, @"^[^a-zA-Z]?(?=[a-zA-Z])", "").StartsWith(config.OwnerName))
                {
                    var matchSay = Regex.Match(message.TextValue, config.CommandSayRegex);
                    if (matchSay.Success)
                    {
                        var matched = matchSay.Groups[2].Value;
                        matched = matched.Replace('[', '<').Replace(']', '>');
                        log.Debug($"match.Success {matched}");

                        messageSender.SendMessage($"{matched}");
                        clientChat.Print(new SeStringBuilder().AddItalicsOn().AddUiForeground(31).AddText($"{sender.TextValue}").AddUiForegroundOff()
                            .AddText($" forced you to say \"{matched}\".")
                            .AddItalicsOff().BuiltString);
                        isHandled = true;
                        return;
                    }
                }*/
            }
        }

        private List<Payload> RenameAndRecolor(List<Payload> payloads, Vector3 color, string originalName, string? nameAlias = null)
        {
            //log.Debug($"{originalName} {nameAlias}");
            var newPayloads = new List<Payload>();

            foreach (var p in payloads)
            {
                if ((p is TextPayload tp) && tp.Text != null && tp.Text.Equals(originalName))
                {
                    if (nameAlias != null)
                    {
                        tp.Text = tp.Text.Replace(originalName, nameAlias);
                    }
                    
                    newPayloads.Add(new ColorPayload(color).AsRaw());
                    newPayloads.Add(tp);
                    newPayloads.Add(new ColorEndPayload().AsRaw());
                }
                else
                {
                    newPayloads.Add(p);
                }
            }
            return newPayloads;
        }

        /// <summary>
        /// This function will determine what to do with an incoming message. By default, this handles the hiding of all incoming encoded tells
        /// <list type="bullet">
        /// <item><c>type</c><param name="type"> - The type of message that was sent.</param></item>
        /// <item><c>senderid</c><param name="senderid"> - The id of the sender.</param></item>
        /// <item><c>sender</c><param name="sender"> - The name of the sender.</param></item>
        /// <item><c>message</c><param name="message"> - The message that was sent.</param></item>
        /// <item><c>isHandled</c><param name="isHandled"> - Whether or not the message was handled.</param></item>
        /// </list> </summary>
        private void Chat_OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString chatmessage, ref bool isHandled)
        {
            log.Debug($"[ChatManager]: chatmessage: {chatmessage.TextValue}");
            // create some new SeStrings for the message and the new line
            var fmessage = new SeString(new List<Payload>());
            var nline = new SeString(new List<Payload>());
            nline.Payloads.Add(new TextPayload("\n"));
            // make payload for the player
            PlayerPayload? playerPayload;
            //removes special characters in party listings [https://na.finalfantasyxiv.com/lodestone/character/10080203/blog/2891974/]
            List<char> toRemove = new() {
            '','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','',
        };
            // convert the sender from SeString to String
            var sanitized = sender.ToString();
            // loop through each character in the toRemove list
            foreach (var c in toRemove) { sanitized = sanitized.Replace(c.ToString(), string.Empty); } // remove all special characters

            // if the sender is the local player, set the player payload to the local player 
            if (sanitized == playerState.CharacterName)
            {
                playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
                if (type == XivChatType.CustomEmote)
                {
                    var playerName = new SeString(new List<Payload>());
                    playerName.Payloads.Add(new TextPayload(playerState.CharacterName));
                    fmessage.Append(playerName);
                }
            }
            // if the sender is not the local player, set the player payload to the sender
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type, dont care didnt ask.
            else
            {
                if (type == XivChatType.StandardEmote)
                {
                    playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload ??
                                    chatmessage.Payloads.FirstOrDefault(x => x is PlayerPayload) as PlayerPayload;
                }
                else
                {
                    playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
                    if (type == XivChatType.CustomEmote && playerPayload != null)
                    {
                        fmessage.Append(playerPayload.PlayerName);
                    }
                }
            }
            log.Debug($"[ChatManager]: chatmessage: {chatmessage.TextValue}");
            log.Debug($"[ChatManager]: fmessage: {fmessage.TextValue}");
#pragma warning restore CS8600 // let us see if we have any others
            // append the chat message to the new formatted message 
            fmessage.Append(chatmessage);
            var isEmoteType = type is XivChatType.CustomEmote or XivChatType.StandardEmote;
            if (isEmoteType)
            {
                fmessage.Payloads.Insert(0, new EmphasisItalicPayload(true));
                fmessage.Payloads.Add(new EmphasisItalicPayload(false));
            }
            log.Debug($"[ChatManager]: fmessage: {fmessage}");
            // set the player name to the player payload, otherwise set it to the local player
            var pName = playerPayload == default(PlayerPayload) ? playerState.CharacterName : playerPayload.PlayerName;
            var sName = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload; // get the player payload from the sender 
            var senderName = sName?.PlayerName != null ? sName.PlayerName : pName;
            // if the message is an incoming tell
            #region TellIncomind
            //if (type == XivChatType.TellIncoming)
            //{
            //    if (senderName == null) { GagSpeak.Log.Error("senderName is null"); return; } // removes the possibly null reference warning

            //    switch (true)
            //    {
            //        // Logic commented on first case, left out on rest. All cases are the same, just with different conditions.
            //        case var _ when _config.friendsOnly && _config.partyOnly && _config.whitelistOnly: //  all 3 options are checked
            //                                                                                           // If a message is from a friend, or a party member, or a whitelisted player, it will become true,
            //                                                                                           // however, to make sure that we meet a condition that causes this to exit, we put a !() infront, to say
            //                                                                                           // they were a player outside of these parameters while the parameters were checked.
            //            if (!(IsFriend(senderName) || IsPartyMember(senderName) || IsWhitelistedPlayer(senderName))) { return; }
            //            break;

            //        case var _ when _config.friendsOnly && _config.partyOnly && !_config.whitelistOnly: // When both friend and party are checked
            //            if (!(IsFriend(senderName) || IsPartyMember(senderName))) { return; }
            //            break;

            //        case var _ when _config.friendsOnly && _config.whitelistOnly && !_config.partyOnly: // When both friend and whitelist are checked
            //            if (!(IsFriend(senderName) || IsWhitelistedPlayer(senderName))) { return; }
            //            break;

            //        case var _ when _config.partyOnly && _config.whitelistOnly && !_config.friendsOnly: // When both party and whitelist are checked
            //            if (!(IsPartyMember(senderName) || IsWhitelistedPlayer(senderName))) { return; }
            //            break;

            //        case var _ when _config.friendsOnly && !_config.partyOnly && !_config.whitelistOnly: // When only friend is checked
            //            if (!(IsFriend(senderName))) { return; }
            //            break;

            //        case var _ when _config.partyOnly && !_config.friendsOnly && !_config.whitelistOnly: // When only party is checked
            //            if (!(IsPartyMember(senderName))) { return; }
            //            break;

            //        case var _ when _config.whitelistOnly && !_config.friendsOnly && !_config.partyOnly: // When only whitelist is checked
            //            if (!(IsWhitelistedPlayer(senderName))) { return; }
            //            break;

            //        default: // None of the filters were checked, so just accept the message anyways because it works for everyone.
            //            break;
            //    }

            //    ////// Once we have reached this point, we know we have recieved a tell, and that it is from one of our filtered players. //////
            //    GagSpeak.Log.Debug($"[Chat Manager]: Recieved tell from: {senderName}");

            //    // if the incoming tell is an encoded message, lets check if we are in dom mode before accepting changes
            //    int encodedMsgIndex = 0; // get a index to know which encoded msg it is, if any
            //    if (MessageDictionary.EncodedMsgDictionary(chatmessage.TextValue, ref encodedMsgIndex))
            //    {
            //        // if in dom mode, back out, none of this will have any significance
            //        if (_config.InDomMode && encodedMsgIndex > 0 && encodedMsgIndex <= 8)
            //        {
            //            GagSpeak.Log.Debug("[Chat Manager]: Encoded Command Ineffective Due to Dominant Status");
            //            isHandled = true;
            //            return;
            //        }
            //        // if our encodedmsgindex is > 1 && < 6, we have a encoded message via command
            //        else if (encodedMsgIndex > 0 && encodedMsgIndex <= 8)
            //        {
            //            List<string> decodedCommandMsg = _messageDecoder.DecodeMsgToList(fmessage.ToString(), encodedMsgIndex);
            //            // function that will determine what happens to the player as a result of the tell.
            //            if (_msgResultLogic.CommandMsgResLogic(fmessage.ToString(), decodedCommandMsg, isHandled, _clientChat, _config))
            //            {
            //                isHandled = true; // logic sucessfully parsed, so hide from chat
            //            }
            //            _config.Save(); // save our config

            //            // for now at least, anything beyond 7 is a whitelist exchange message
            //        }
            //        else if (encodedMsgIndex > 8)
            //        {
            //            List<string> decodedWhitelistMsg = _messageDecoder.DecodeMsgToList(fmessage.ToString(), encodedMsgIndex);
            //            // function that will determine what happens to the player as a result of the tell.
            //            if (_msgResultLogic.WhitelistMsgResLogic(fmessage.ToString(), decodedWhitelistMsg, isHandled, _clientChat, _config))
            //            {
            //                isHandled = true; // logic sucessfully parsed, so hide from chat
            //            }
            //            isHandled = true;
            //            return;
            //        }
            //    } // skipped to this point if not encoded message
            //} // skips directly to here if not a tell
            #endregion
        }

        private void Chat_OnChatMessageHandled(XivChatType type, int timestamp, SeString sender, SeString message)
        {
            log.Debug($"Chat_OnChatMessageHandled");
        }

        private void Chat_OnChatMessageUnhandled(XivChatType type, int timestamp, SeString sender, SeString message)
        {
            log.Debug($"Chat_OnChatMessageUnhandled");
        }

        /// <summary>
        /// This function will handle the framework update, and will send messages to the server if there are any in the queue.
        /// <list type="bullet">
        /// <item><c>framework</c><param name="framework"> - The framework to be updated.</param></item>
        /// </list></summary>
        private void framework_Update(IFramework framework)
        {
            if (config != null /*&& _config.Enabled*/)
            {
                try
                {
                    if (messageQueue.Count > 0 && messageSender != null)
                    {
                        if (!messageTimer.IsRunning)
                        {
                            messageTimer.Start();
                        }
                        else
                        {
                            if (messageTimer.ElapsedMilliseconds > 1000)
                            {
                                try
                                {
                                    messageSender.SendMessage(messageQueue.Dequeue());
                                }
                                catch (Exception e)
                                {
                                    //GagSpeak.Log.Warning($"{e},{e.Message}");
                                }
                                messageTimer.Restart();
                            }
                        }
                    }

                }
                catch
                {
                    //GagSpeak.Log.Error($"[Chat Manager]: Failed to process Framework UpdateTerritoryChanged!");
                }
            }
        }

        private CharacterKey? TryGetSenderNameAndWorld(SeString sender)
        {
            if (!sender.Payloads.Any())
            {
                return null;
            }
            string? name, world;

            world = playerState.HomeWorld.Value.Name.ToString();
            name = playerState.CharacterName;

            var pp = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
            if (pp == null)
            {
                var tp = sender.Payloads.OfType<TextPayload>().FirstOrDefault();
                if (name != null && name.Equals(tp.Text))
                {
                    return new CharacterKey(name, world);
                }
                return null;
            }
            else
            {
                name = pp.PlayerName;
            }

            // World is a RowRef<World> (may be invalid when same-world is omitted)
            if (pp.World.IsValid)
            {
                world = pp.World.Value.Name.ToString();
            }

            return new CharacterKey(name, world);
        }
    }
}
