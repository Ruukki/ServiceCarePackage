using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Commands
{
    public class EmoteCommand : IChatCommandHandler
    {
        private readonly MessageSender messageSender;
        private readonly MoveManager moveManager;
        private readonly IChatGui chatGui;
        private readonly ILog log;

        private string? lastRegex;
        private Regex? cachedRegex;

        public EmoteCommand(MessageSender messageSender, MoveManager moveManager, IChatGui chatGui, ILog log)
        {
            this.messageSender = messageSender;
            this.moveManager = moveManager;
            this.chatGui = chatGui;
            this.log = log;
        }
        public Regex Pattern
        {
            get
            {
                var current = FixedConfig.CommandRegex ?? string.Empty;
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
            var restult = CommandGuards.ChatTypes(ctx, FixedConfig.CharConfig.AllowSayChatForPuppetMaster 
                ? Array.Empty<XivChatType>() : new[] { XivChatType.Say });
            log.Debug(restult.ToString());
            return restult;
        }

        public async Task HandleAsync(ChatCommandContext ctx, Match match, CancellationToken ct)
        {
            //Pattern = new(FixedConfig.CommandRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            log.Debug(FixedConfig.CommandRegex);
            var matched = match.Groups[1].Value;
            log.Debug($"match.Success {matched}");
            chatGui.Print(new SeStringBuilder().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                .AddText($" forced you to {matched}.")
                .BuiltString);

            moveManager.DisableMovingFor(FixedConfig.CharConfig.StunDuration+100);
            Thread.Sleep(100);
            messageSender.SendMessage($"/{matched}");
        }
    }
}
