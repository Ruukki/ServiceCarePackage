using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using ServiceCarePackage.Models;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Text;
using static FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyFriendList;

namespace ServiceCarePackage.Services.Target
{
    internal class TargetingManager
    {
        private ILog log { get; }
        private ITargetManager targetManager { get; }
        private IClientState clientState { get; }

        public TargetingManager(ILog log, ITargetManager targetManager, IClientState client)
        {
            this.log = log;
            this.targetManager = targetManager;
            this.clientState = client;
        }

        public CharacterKey? GetTargetedPlayerName()
        {
            string name = string.Empty;
            string world = string.Empty;

            var target = targetManager.Target;
            if (target == null)
            {
                log.Debug("Target was null");
                return null;
            }

            if (target is IPlayerCharacter pc && pc.HomeWorld.IsValid)
            {
                name = pc.Name.TextValue;
                world = pc.HomeWorld.Value.Name.ToString();
                log.Debug($"Target was {name}@{world}");
                return new(name, world);
            }

            return null;
        }
    }
}
