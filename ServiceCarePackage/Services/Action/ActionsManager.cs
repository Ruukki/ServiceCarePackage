using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.Logs;
using System;
using System.Collections.Generic;
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

        internal ActionsManager(ILog log, IFramework framework, IGameInteropProvider gameInteropProvider) 
        {
            this.log = log;
            this.framework = framework;
            this.gameInteropProvider = gameInteropProvider;

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
