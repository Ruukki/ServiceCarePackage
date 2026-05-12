using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using Serilog.Core;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace ServiceCarePackage.Services.Chat
{
    public class MessageSender : IDisposable
    {
        private readonly ConcurrentQueue<string> messageQueue = new();

        private readonly Stopwatch delayTimer = new();

        private readonly ILog log;
        private readonly IFramework framework;

        /// <summary> By being an internal constructor, it means that this class can only be accessed by the same assembly. </summary>
        public MessageSender(ISigScanner scanner, ILog log, IFramework framework)
        {
            delayTimer.Start();
            this.log = log;
            this.framework = framework;

            framework.Update += OnFrameworkUpdate;
        }

        public void Dispose()
        {
            framework.Update -= OnFrameworkUpdate;
        }

        public void SendMessageEnqueue(string message)
        {
            messageQueue.Enqueue(message);
        }

        public void SendMessageNow(string message)
        {
            SendInternal(message);
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (messageQueue.IsEmpty)
                return;

            if (delayTimer.ElapsedMilliseconds < 500)
                return;

            if (!messageQueue.TryDequeue(out var message))
                return;

            SendInternal(message);

            delayTimer.Restart();
        }

        private static unsafe void SendInternal(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            if (bytes.Length is 0 or > 500)
                return;

            var utf8 = Utf8String.FromString(message);

            try
            {
                UIModule.Instance()->ProcessChatBoxEntry(utf8);
            }
            finally
            {
                utf8->Dtor(true);
            }
        }
    }
}
