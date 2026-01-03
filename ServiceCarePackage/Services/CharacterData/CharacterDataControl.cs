using Lumina.Excel.Sheets;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using OnlineStatus = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCommonList.CharacterData.OnlineStatus;
using UIModule = FFXIVClientStructs.FFXIV.Client.UI.UIModule;
using InfoModule = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoModule;
using InfoProxyDetail = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyDetail;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel;

namespace ServiceCarePackage.Services.CharacterData
{
    internal unsafe class CharacterDataControl : IDisposable
    {
        private readonly ILog log;
        private readonly IClientState clientState;

        private IPlayerCharacter? localPlayer;

        internal CharacterDataControl(ILog log, IClientState clientState)
        {
            this.log = log;
            this.clientState = clientState;

        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
