using Dalamud.Plugin.Services;
using ServiceCarePackage.Config;
using ServiceCarePackage.Services.Logs;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Services.Movement
{
    internal class MoveManager : IDisposable
    {
        private bool MovingDisabled { get; set; } = false;
        public bool IsWalkingForced { get; set; }

        private MoveMemory _memory { get; set; }
        private ILog log { get; set; }
        private IFramework framework { get; set; }
        private ICondition condition { get; set; }

        private readonly object _gate = new();
        private CancellationTokenSource? _autoEnableCts;
        private nint gameControl;
        private bool isWalkingOffset;

        private volatile bool _pendingEnable;

        internal MoveManager(ILog log, MoveMemory mem, IFramework framework, ICondition condition)
        {
            this.log = log;
            _memory = mem;
            this.framework = framework;
            this.condition = condition;

            Init();

            this.framework.Update += OnUpdate;
        }

        private unsafe void Init()
        {
            gameControl = (nint)FFXIVClientStructs.FFXIV.Client.Game.Control.Control.Instance();
        }

        #region Block movement
        public unsafe void EnableMoving()
        {
            if (MovingDisabled)
            {
                //PluginLog.Debug($"Enabling moving, cnt {_memory.ForceDisableMovement}");
                _memory.DisableHooks();
                if (_memory.ForceDisableMovement > 0)
                {
                    _memory.ForceDisableMovement--;
                }
                MovingDisabled = false;
            }
        }

        public void DisableMoving()
        {
            if (!MovingDisabled)
            {
                //PluginLog.Debug($"Disabling moving, cnt {_memory.ForceDisableMovement}");
                _memory.EnableHooks();
                _memory.ForceDisableMovement++;
                MovingDisabled = true;
            }
        }

        /// <summary>
        /// Disables movement once, and schedules an automatic EnableMoving after duration.
        /// If movement is already disabled, this does nothing and does NOT alter the existing timer.
        /// </summary>
        public void DisableMovingFor(TimeSpan duration)
        {
            // If already disabled, per your requirement: do nothing, keep existing timer (if any).
            if (MovingDisabled)
                return;

            // Perform the state change now (same thread as caller; if you prefer, you can queue it to framework)
            DisableMoving();

            // Ensure only a single timer exists for THIS disable.
            StartAutoEnableTimer(duration);
        }

        public void DisableMovingFor(int durationMs)
        {
            DisableMovingFor(TimeSpan.FromMilliseconds(durationMs));
        }

        private void StartAutoEnableTimer(TimeSpan duration)
        {
            // Replace any prior timer (normally none, because we only create on transition)
            lock (_gate)
            {
                _autoEnableCts?.Cancel();
                _autoEnableCts?.Dispose();
                _autoEnableCts = new CancellationTokenSource();
                _ = AutoEnableAfter(duration, _autoEnableCts.Token);
            }
        }

        private async Task AutoEnableAfter(TimeSpan duration, CancellationToken token)
        {
            try
            {
                if (duration > TimeSpan.Zero)
                    await Task.Delay(duration, token);

                if (token.IsCancellationRequested)
                    return;

                // Don’t call EnableMoving() from threadpool; request it for framework thread.
                _pendingEnable = true;
            }
            catch (TaskCanceledException e)
            {
                log.Error(e,"");
            }
        }        

        /// <summary>
        /// If you ever want a manual Enable that also cancels the timer.
        /// </summary>
        public void EnableMovingAndCancelTimer()
        {
            CancelAutoEnable();
            EnableMoving();
        }

        private void CancelAutoEnable()
        {
            lock (_gate)
            {
                if (_autoEnableCts == null) return;
                _autoEnableCts.Cancel();
                _autoEnableCts.Dispose();
                _autoEnableCts = null;
            }
        }
        #endregion
        #region forced walk
        private unsafe void ForceWalk()
        {
            if (!insideInstance())
            {
                var control = (FFXIVClientStructs.FFXIV.Client.Game.Control.Control*)gameControl;
                bool isWalking = control->IsWalking;
                if (isMountedOrInCombat() /*|| PlayerContext.isInWhitelistedTerritory()*/)
                {
                    if (isWalking)
                    {
                        control->IsWalking = false;
                        return;
                    }
                }
                else if (!isWalking)
                {
                    control->IsWalking = true;
                    return;
                }
            }
        }

        private bool insideInstance()
        {
            return
                condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty] ||
                condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty56] ||
                condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty95] ||
                condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Disguised];
        }

        private bool isMountedOrInCombat()
        {
            return condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted] ||
                condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
        }
        #endregion
        private void OnUpdate(IFramework _)
        {
            if (_pendingEnable)
            {
                _pendingEnable = false;

                // Only enable if still disabled (idempotent)
                EnableMoving();

                // Timer fulfilled; clear CTS
                lock (_gate)
                {
                    _autoEnableCts?.Dispose();
                    _autoEnableCts = null;
                }
            }

            if (FixedConfig.CharConfig.EnableForcedWalk)
            {
                ForceWalk();
            }
        }
        public void Dispose()
        {
            framework.Update -= OnUpdate;
            CancelAutoEnable();

            if (MovingDisabled)
                EnableMoving();
        }
    }
}
