using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCarePackage.UI
{
    internal unsafe class ChatUI : IDisposable
    {
        private ILog log;
        private IGameGui gameGui;
        private readonly IFramework framework;
        public bool IsChatOpen { get; private set; }
        public event Action<bool>? IsChatOpenChanged;
        internal ChatUI(ILog log, IGameGui gameGui, IFramework framework)
        {
            this.log = log;
            this.gameGui = gameGui;
            this.framework = framework;
            this.framework.Update += framework_Update;
        }

        public void CheckIfChatIsOpen()
        {
            var module = gameGui.GetUIModule();
            if (module.IsNull) { return; }
            var uiModule = (UIModule*)module.Address;
            var inputActive = uiModule->GetRaptureAtkModule()->IsTextInputActive();
            //log.Debug("1" + inputActive.ToString());

            var globalInputActive = ImGui.GetIO().WantTextInput;
            //log.Debug("2" + globalInputActive.ToString());

            var isOpen = inputActive || globalInputActive;

            if (isOpen == IsChatOpen) { return; }

            IsChatOpen = isOpen;
            IsChatOpenChanged?.Invoke(isOpen);
        }

        private void framework_Update(IFramework framework)
        {
            CheckIfChatIsOpen();
        }

        public void Dispose()
        {
            this.framework.Update -= framework_Update;
        }
    }
}
