using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.Havok.Common.Base.Types;
using ServiceCarePackage.Config;
using ServiceCarePackage.Enums;
using ServiceCarePackage.Services.Chat;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.Services.Movement;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Commands
{
    public class SettingsLockCommand : IChatCommandHandler
    {
        private readonly ConfigManager configManager;
        private readonly IChatGui chatGui;
        private readonly ILog log;

        private string? lastRegex;
        private Regex? cachedRegex;

        public SettingsLockCommand(ConfigManager configManager, IChatGui chatGui, ILog log)
        {
            this.configManager = configManager;
            this.chatGui = chatGui;
            this.log = log;
        }
        public Regex Pattern
        {
            get
            {
                var current = FixedConfig.CommandRegexLock ?? string.Empty;
                if (!string.Equals(lastRegex, current, StringComparison.Ordinal))
                {
                    lastRegex = current;
                    cachedRegex = new Regex(current,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                return cachedRegex!;
            }
        }

        public int Priority => 1;

        public bool CanExecute(ChatCommandContext ctx, Match match)
        {
            if (CommandGuards.OwnerOnly(ctx)
                && CommandGuards.ChatTypes(ctx, FixedConfig.CharConfig.AllowSayChatForPuppetMaster ? Array.Empty<XivChatType>() : new[] { XivChatType.Say }))
            {
                return true;
            }
            return false;
        }

        public async Task HandleAsync(ChatCommandContext ctx, Match match, CancellationToken ct)
        {
            var matched = match.Groups[2].Value;
            log.Debug($"match.Success {matched}");
            Enum.TryParse<SettingLockLevels>(matched, ignoreCase: true, out var value);

            FixedConfig.CharConfig.SettingLockLevels = value;

            chatGui.Print(new SeStringBuilder().AddUiForeground(31).AddText($"[{ctx.sender.TextValue}]").AddUiForegroundOff()
                    .AddText($" has applied {value} lock to your settings. Have fun :3")
                    .BuiltString);

            configManager.Save();

        }
    }
}
