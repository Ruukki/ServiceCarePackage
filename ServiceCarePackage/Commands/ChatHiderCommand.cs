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
    public class ChatHidetCommand : IChatCommandHandler
    {
        private readonly MessageSender messageSender;
        private readonly MoveManager moveManager;
        private readonly IChatGui chatGui;
        private readonly ILog log;

        public ChatHidetCommand(MessageSender messageSender, MoveManager moveManager, IChatGui chatGui, ILog log)
        {
            this.messageSender = messageSender;
            this.moveManager = moveManager;
            this.chatGui = chatGui;
            this.log = log;
        }
        public Regex Pattern { get; } =
            new(FixedConfig.CommandRegexBase("dont look"), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public int Priority => 100;

        public bool CanExecute(ChatCommandContext ctx, Match match)
        {
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
            var matched = match.Groups[1].Value;
            log.Debug($"match.Success {matched}");
            if (FixedConfig.IsActive_ChatHider)
            {
                chatGui.Print(new SeStringBuilder().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                    .AddText($" allowed you to look again.")
                    .BuiltString);
                FixedConfig.IsActive_ChatHider = false;
            }
            else
            {                
                chatGui.Print(new SeStringBuilder().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                    .AddText($" forced you to {matched}.")
                    .BuiltString);
                FixedConfig.IsActive_ChatHider = true;
            }
        }
    }
}
