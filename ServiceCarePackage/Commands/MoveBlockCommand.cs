using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Commands
{
    public class MoveBlockCommand : IChatCommandHandler
    {
        private readonly MoveManager moveManager;
        private readonly IChatGui chatGui;
        private readonly ILog log;

        private string? lastRegex;
        private Regex? cachedRegex;

        public MoveBlockCommand(MoveManager moveManager, IChatGui chatGui, ILog log)
        {
            this.moveManager = moveManager;
            this.chatGui = chatGui;
            this.log = log;
        }
        public Regex Pattern
        {
            get
            {
                var current = FixedConfig.CommandRegexBase("dont move") ?? string.Empty;
                if (!string.Equals(lastRegex, current, StringComparison.Ordinal))
                {
                    lastRegex = current;
                    cachedRegex = new Regex(current,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                return cachedRegex!;
            }
        }

        public int Priority => 99;

        public bool CanExecute(ChatCommandContext ctx, Match match)
        {
            if (FixedConfig.CharConfig.EnablePuppetMasterHadcore
                && CommandGuards.OwnerOnly(ctx)
                && CommandGuards.ChatTypes(ctx, FixedConfig.CharConfig.AllowSayChatForPuppetMaster ? Array.Empty<XivChatType>() : new[] { XivChatType.Say }))
            {
                return true;
            }
            /*chatGui.Print(new SeStringBuilder().AddUiForeground("[Warning]", 8).AddUiForegroundOff().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                    .AddText($" has tried to execute {match.Groups[1].Value} but had insuficient permissions.")
                    .BuiltString);*/
            return false;
        }

        public async Task HandleAsync(ChatCommandContext ctx, Match match, CancellationToken ct)
        {
            var matched = match.Groups[1].Value;
            log.Debug($"match.Success {matched}");
            
            chatGui.Print(new SeStringBuilder().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                    .AddText($" has {moveManager.ToggleMoving().ToString()} your ability to move.")
                    .BuiltString);
        }
    }
}
