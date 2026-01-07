using ServiceCarePackage.Commands;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Services
{
    public class CommandsHandler
    {
        private readonly IChatCommandHandler[] handlers;
        private readonly ILog log;

        public CommandsHandler(IEnumerable<IChatCommandHandler> handlers, ILog log)
        {
            if (handlers is null) throw new ArgumentNullException(nameof(handlers));
            this.handlers = [.. handlers.OrderByDescending(h => h.Priority)];

            this.log = log;
        }

        public async Task<bool> TryDispatchAsync(ChatCommandContext ctx, CancellationToken ct = default)
        {
            if (ctx is null) throw new ArgumentNullException(nameof(ctx));

            foreach (IChatCommandHandler handler in handlers)
            {
                //log.Debug(handler.GetType().Name);                
                Match match = handler.Pattern.Match(ctx.message.TextValue);
                
                if (!match.Success) continue;
                
                if (!handler.CanExecute(ctx, match))
                    return true; // "handled" in the sense that the text was a command, but disallowed

                await handler.HandleAsync(ctx, match, ct).ConfigureAwait(false);
                return true;
            }

            return false;
        }
    }
}
