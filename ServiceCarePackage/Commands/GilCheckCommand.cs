using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.CharacterData;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Events;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Commands
{
    internal class GilCheckCommand : IChatCommandHandler
    {
        private readonly MessageSender messageSender;
        private readonly IChatGui chatGui;
        private readonly ILog log;
        private readonly CharacterDataService characterDataService;

        private string? lastRegex;
        private Regex? cachedRegex;

        public GilCheckCommand(MessageSender messageSender, IChatGui chatGui, ILog log, CharacterDataService characterDataService)
        {
            this.messageSender = messageSender;
            this.chatGui = chatGui;
            this.log = log;
            this.characterDataService = characterDataService;
        }
        public Regex Pattern
        {
            get
            {
                var current = FixedConfig.CommandRegexBase("show wallet") ?? string.Empty;
                if (!string.Equals(lastRegex, current, StringComparison.Ordinal))
                {
                    lastRegex = current;
                    cachedRegex = new Regex(current,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                return cachedRegex!;
            }
        }

        public int Priority => 0;

        public bool CanExecute(ChatCommandContext ctx, Match match)
        {
            var restult = CommandGuards.OwnerOnly(ctx) && CommandGuards.ChatTypes(ctx, FixedConfig.CharConfig.AllowSayChatForPuppetMaster
                ? Array.Empty<XivChatType>() : new[] { XivChatType.Say });
            return restult;
        }

        public async Task HandleAsync(ChatCommandContext ctx, Match match, CancellationToken ct)
        {
            //Pattern = new(FixedConfig.CommandRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            log.Debug(lastRegex);
            var matched = match.Groups[1].Value?.ToLower();
            log.Debug($"match.Success {matched}");
            chatGui.Print(new SeStringBuilder().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                .AddText($" forced you to expose contents of your wallet.")
                .BuiltString);

            try
            {
                messageSender.SendMessage($"/tell {ctx.senderOriginal} {FixedConfig.DisplayName} currently owns: {characterDataService?.GetPlayerGil()}gil and retainers have: {FixedConfig.CharConfig.RetainerGil}gil");
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }
    }
}
