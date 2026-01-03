using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ServiceCarePackage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCarePackage.Helpers
{
    public static class Generic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRange<T>(this int idx, IReadOnlyCollection<T> collection)
            => (uint)idx < (uint)collection.Count;

        public static T SafelySelect<T>(this IReadOnlyList<T> list, int index)
        {
            // if the list is null, return the default value of T
            if (list is null)
                return default!;
            // if the list is out of range, return the default.
            if (index < 0 || index >= list.Count)
                return default!;
            // otherwise, return the item at the index.
            return list[index];
        }

        public static void SafeEnable<T>(this Hook<T>? hook) where T : Delegate
        {
            if (hook is null || hook.IsEnabled)
                return;
            // hook can be enabled.
            hook.Enable();
        }

        public static void SafeDisable<T>(this Hook<T>? hook) where T : Delegate
        {
            if (hook is null || !hook.IsEnabled)
                return;
            // hook can be disabled.
            hook.Disable();
        }

        public static void SafeDispose<T>(this Hook<T>? hook) where T : Delegate
        {
            if (hook is null || hook.IsDisposed) // already disposed.
                return;

            // Disable first if it can be.
            if (hook.IsEnabled)
                hook.Disable();

            // Dispose the hook.
            hook.Dispose();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Safe(Action a, bool suppressErrors = false)
        {
            try
            {
                a();
            }
            catch (Exception e) { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? Safe<T>(Func<T> a, bool suppressErrors = false)
        {
            try
            {
                return a();
            }
            catch (Exception e) { return default; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task Safe(Func<Task> a, bool suppressErrors = false)
        {
            try
            {
                await a();
            }
            catch (Exception e) { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T?> Safe<T>(Func<Task<T>> a, bool suppressErrors = false)
        {
            try
            {
                return await a();
            }
            catch (Exception e) { return default; }
        }

        /// <summary> Consumes any ObjectDisposedExceptions when trying to cancel a token source. </summary>
        /// <remarks> Only use if you know what you are doing and need to consume the error. </remarks>
        public static void SafeCancel(this CancellationTokenSource? cts)
        {
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException) { /* CONSUME THE VOID */ }
        }

        /// <summary> Consumes any ObjectDisposedExceptions when trying to dispose a token source. </summary>
        /// <remarks> Only use if you know what you are doing and need to consume the error. </remarks>
        public static void SafeDispose(this CancellationTokenSource? cts)
        {
            try
            {
                cts?.Dispose();
            }
            catch (ObjectDisposedException) { /* CONSUME THE VOID */ }
        }

        /// <summary> Consumes any ObjectDisposedExceptions when trying to cancel and dispose a token source. </summary>
        /// <remarks> Only use if you know what you are doing and need to consume the error. </remarks>
        public static void SafeCancelDispose(this CancellationTokenSource? cts)
        {
            try
            {
                cts?.Cancel();
                cts?.Dispose();
            }
            catch (ObjectDisposedException) { /* CONSUME THE VOID */ }
        }

        /// <summary> Consumes any ObjectDisposedExceptions when trying to recreate a token source. </summary>
        /// <remarks> Only use if you know what you are doing and need to consume the error. </remarks>
        public static CancellationTokenSource SafeCancelRecreate(this CancellationTokenSource? cts)
        {
            cts?.SafeCancelDispose();
            return new CancellationTokenSource();
        }

        /// <summary> Consumes any ObjectDisposedExceptions when trying to recreate a token source. </summary>
        /// <remarks> Only use if you know what you are doing and need to consume the error. </remarks>
        public static void SafeCancelRecreate(ref CancellationTokenSource? cts)
        {
            cts?.SafeCancelDispose();
            cts = new CancellationTokenSource();
        }

        public static CharacterKey? TryGetSenderNameAndWorld(this SeString sender, IPlayerState playerState)
        {
            if (!sender.Payloads.Any())
            {
                return null;
            }
            string? name, world;

            world = playerState.HomeWorld.Value.Name.ToString();
            name = playerState.CharacterName;

            var pp = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
            if (pp == null)
            {
                var tp = sender.Payloads.OfType<TextPayload>().FirstOrDefault();
                if (name != null && name.Equals(tp.Text))
                {
                    return new CharacterKey(name, world);
                }
                return null;
            }
            else
            {
                name = pp.PlayerName;
            }

            // World is a RowRef<World> (may be invalid when same-world is omitted)
            if (pp.World.IsValid)
            {
                world = pp.World.Value.Name.ToString();
            }

            return new CharacterKey(name, world);
        }
    }
}
