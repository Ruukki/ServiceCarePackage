using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using static FFXIVClientStructs.FFXIV.Client.Game.ActionManager;

namespace ServiceCarePackage.Services.Action
{
    internal unsafe class ActionsManager : IDisposable
    {
        private readonly ILog log;
        private readonly IFramework framework;
        private readonly IGameInteropProvider gameInteropProvider;
        private readonly IObjectTable gameObjectsTable;
        private HashSet<uint> healerJobs = new();

        private delegate bool UseActionDelegate(
            ActionManager* self,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint extraParam,
            UseActionMode mode,
            uint comboRouteId,
            bool* outOptAreaTargeted);

        private Hook<UseActionDelegate>? useActionHook { get; set; } = null!;

        internal ActionsManager(ILog log, IFramework framework, IGameInteropProvider gameInteropProvider, IDataManager data, IObjectTable objects) 
        {
            this.log = log;
            this.framework = framework;
            this.gameInteropProvider = gameInteropProvider;
            gameObjectsTable = objects;

            healerJobs = data.GetExcelSheet<ClassJob>()!
                .Where(j => j.Role == 4)
                .Select(j => j.RowId)
                .ToHashSet();

            useActionHook = this.gameInteropProvider.HookFromAddress<UseActionDelegate>(
                ActionManager.MemberFunctionPointers.UseAction,
                UseActionDetour);

            useActionHook.Enable();
        }

        private unsafe bool UseActionDetour(
            ActionManager* self,
            ActionType actionType,
            uint actionId,
            ulong targetId,
            uint extraParam,
            UseActionMode mode,
            uint comboRouteId,
            bool* outOptAreaTargeted)
        {
            //log.Information($"Action: {actionId} Type: {actionType}");

            if (FixedConfig.CharConfig.GilActionBlockingActive)
            {
                log.Information($"Feature: {FixedConfig.CharConfig.GilActionBlockingActive} Total: {FixedConfig.TotalGil} Threshold: {FixedConfig.CharConfig.GilThreshhold}");
                /*if (FixedConfig.ActionTypeWhitelist.Contains(actionType) || FixedConfig.ActionIdWhitelist.Contains(actionId))
                {
                    return useActionHook!.Original(
                self,
                actionType,
                actionId,
                targetId,
                extraParam,
                mode,
                comboRouteId,
                outOptAreaTargeted);
                }*/

                if (FixedConfig.TotalGil > FixedConfig.CharConfig.GilThreshhold)
                {
                    return false;
                }
            }

            if (actionType == ActionType.Action && actionId > 6)
            {
                if (FixedConfig.CharConfig.HealSlutMode)
                {
                    var player = gameObjectsTable.LocalPlayer;
                    if (player is not null && !healerJobs.Contains(player.ClassJob.RowId))
                    {
                        // player is on a healer — your logic
                        return false;
                    }
                }
            }

            return useActionHook!.Original(
                self,
                actionType,
                actionId,
                targetId,
                extraParam,
                mode,
                comboRouteId,
                outOptAreaTargeted);
        }

        public void Dispose() 
        {
            useActionHook?.Disable();
            useActionHook?.Dispose();
        }
    }
}
