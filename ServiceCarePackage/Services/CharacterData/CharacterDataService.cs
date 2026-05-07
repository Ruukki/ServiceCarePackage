using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfoModule = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoModule;
using InfoProxyDetail = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyDetail;
using OnlineStatus = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCommonList.CharacterData.OnlineStatus;
using UIModule = FFXIVClientStructs.FFXIV.Client.UI.UIModule;

namespace ServiceCarePackage.Services.CharacterData
{
    internal unsafe class CharacterDataService : IDisposable
    {
        private readonly ILog log;
        private readonly IClientState clientState;
        private readonly ConfigManager configManager;

        private IPlayerCharacter? localPlayer;

        internal CharacterDataService(ILog log, IClientState clientState, ConfigManager configManager)
        {
            this.log = log;
            this.clientState = clientState;
            this.configManager = configManager;

            clientState.Logout += LogoutSave;
        }

        public ulong GetTotalGil()
        {
            ulong result = 0;
            var retainerGil = GetRetainerGil();
            result = retainerGil.Any() ? retainerGil.Aggregate(0UL, (acc, v) => acc + v) : FixedConfig.CharConfig.RetainerGil;
            result += GetPlayerGil();
            return result;
        }

        public uint GetPlayerGil()
        {
            return InventoryManager.Instance()->GetGil();
        }

        public uint[] GetRetainerGil()
        {
            var rm = RetainerManager.Instance();

            if (rm == null || !rm->IsReady)
                return Array.Empty<uint>();

            var count = rm->GetRetainerCount();
            var returnArray = new uint[count];

            for (uint i = 0; i < count; i++)
            {
                var retainer = rm->GetRetainerBySortedIndex(i);
                if (retainer == null || retainer->RetainerId == 0)
                    continue;

                var name = retainer->NameString;
                var gil = retainer->Gil;

                returnArray[i] = gil;
            }

            if (returnArray.Any())
            {
                FixedConfig.CharConfig.RetainerGil = returnArray.Aggregate(0UL, (acc, v) => acc + v);
            }

            return returnArray;
        }

        private void LogoutSave(int type, int code)
        {
            configManager.Save();
        }

        public void Dispose()
        {
            clientState.Logout -= LogoutSave;
            //throw new NotImplementedException();
        }
    }
}
