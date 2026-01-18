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
    public class FullMatchCommand : IChatCommandHandler
    {
        private readonly MessageSender messageSender;
        private readonly MoveManager moveManager;
        private readonly IChatGui chatGui;
        private readonly ILog log;

        private string? lastRegex;
        private Regex? cachedRegex;

        public FullMatchCommand(MessageSender messageSender, MoveManager moveManager, IChatGui chatGui, ILog log)
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
                var current = FixedConfig.CommandRegexFull ?? string.Empty;
                if (!string.Equals(lastRegex, current, StringComparison.Ordinal))
                {
                    lastRegex = current;
                    cachedRegex = new Regex(current,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                return cachedRegex!;
            }
        }

        public int Priority => 1001;

        public bool CanExecute(ChatCommandContext ctx, Match match)
        {
            log.Debug($"{FixedConfig.CharConfig.EnablePuppetMasterHadcore}");
            log.Debug($"{CommandGuards.OwnerOnly(ctx)}");
            log.Debug($"{CommandGuards.ChatTypes(ctx, FixedConfig.CharConfig.AllowSayChatForPuppetMaster ? Array.Empty<XivChatType>() : new[] {XivChatType.Say})}");
            if (FixedConfig.CharConfig.EnablePuppetMasterHadcore
                && CommandGuards.OwnerOnly(ctx)
                && CommandGuards.ChatTypes(ctx, FixedConfig.CharConfig.AllowSayChatForPuppetMaster ? Array.Empty<XivChatType>() : new[] { XivChatType.Say }))
            {
                return true;
            }
            return false;
        }

        public async Task HandleAsync(ChatCommandContext ctx, Match match, CancellationToken ct)
        {
            log.Debug(FixedConfig.CommandRegexFull);
            var matched = match.Groups[1].Value;
            matched = matched.Replace('[', '<').Replace(']', '>');
            log.Debug($"match.Success {matched}");
            chatGui.Print(new SeStringBuilder().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                .AddText($" forced you to {matched}.")
                .BuiltString);

            //moveManager.DisableMovingFor(5100);
            //Thread.Sleep(100);
            messageSender.SendMessage($"/{matched}");
        }
    }
}
