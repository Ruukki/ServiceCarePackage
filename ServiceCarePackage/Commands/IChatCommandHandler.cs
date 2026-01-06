using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ServiceCarePackage.Config;
using ServiceCarePackage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Commands
{
    public interface IChatCommandHandler
    {
        // Used for matching and extracting groups.
        Regex Pattern { get; }

        // Optional: order/priority if multiple patterns could match.
        int Priority => 0;

        bool CanExecute(ChatCommandContext ctx, Match match);

        Task HandleAsync(ChatCommandContext ctx, Match match, CancellationToken ct);
    }

    public sealed record ChatCommandContext(
        XivChatType chatType, int timestamp,
        CharacterKey senderIriginal, SeString sender, SeString message,
        bool IsSenderOwner = false
    );

    public static class CommandGuards
    {
        public static bool OwnerOnly(ChatCommandContext ctx) => ctx.IsSenderOwner;

        public static bool ChatTypes(ChatCommandContext ctx, params XivChatType[] disAllowed)
        {
            foreach (var t in FixedConfig.BaseChatTypes.Except(disAllowed))
                if (ctx.chatType == t) return true;
            return false;
        }
    }
}
