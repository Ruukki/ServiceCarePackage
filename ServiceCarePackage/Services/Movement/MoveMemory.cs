using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using ServiceCarePackage.Services.Logs;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MButtonHoldState = FFXIVClientStructs.FFXIV.Client.Game.Control.InputManager.MouseButtonHoldState;

namespace ServiceCarePackage.Services.Movement
{
    public unsafe class MoveMemory : IDisposable
    {
        private ILog log { get; set; }
        internal MoveMemory(IGameInteropProvider hook, ILog log)
        {
            this.log = log;
            hook.InitializeFromAttributes(this);
            //PluginLog.Debug($"forceDisableMovementPtr = {forceDisableMovementPtr:X16}");            
        }

        [Signature(Signatures.ForceDisableMovement, ScanType = ScanType.StaticAddress, Fallibility = Fallibility.Infallible)]
        private nint forceDisableMovementPtr;
        internal ref int ForceDisableMovement => ref *(int*)(forceDisableMovementPtr + 4);

        #region hooks
        // better for preventing mouse movements in both camera modes
        public unsafe delegate void MoveOnMousePreventerDelegate(MoveControllerSubMemberForMine* thisx, float wishdir_h, float wishdir_v, char arg4, byte align_with_camera, Vector3* direction);
        [Signature(Signatures.MouseMoveBlock, DetourName = nameof(MovementUpdate), Fallibility = Fallibility.Auto)]
        private static Hook<MoveOnMousePreventerDelegate>? MouseMovePreventerHook { get; set; } = null!;
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe void MovementUpdate(MoveControllerSubMemberForMine* thisx, float wishdir_h, float wishdir_v, char arg4, byte align_with_camera, Vector3* direction)
        {
            if (thisx->Unk_0x3F != 0)
                return;

            MouseMovePreventerHook?.Original(thisx, wishdir_h, wishdir_v, arg4, align_with_camera, direction);
        }
        #endregion

        internal void EnableHooks()
        {
            MouseMovePreventerHook?.Enable();
        }

        internal void DisableHooks()
        {
            MouseMovePreventerHook?.Disable();
        }

        public void Dispose()
        {
            DisableHooks();
            MouseMovePreventerHook?.Disable();
            MouseMovePreventerHook?.Dispose();
        }
    }

    public static class OrbHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsAny<T>(this T obj, params T[] values)
        {
            return values.Any(x => x.Equals(obj));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct UnkGameObjectStruct
    {
        [FieldOffset(0xD0)] public int Unk_0xD0;
        [FieldOffset(0x101)] public byte Unk_0x101;
        [FieldOffset(0x1C0)] public Vector3 DesiredPosition;
        [FieldOffset(0x1D0)] public float NewRotation;
        [FieldOffset(0x1FC)] public byte Unk_0x1FC;
        [FieldOffset(0x1FF)] public byte Unk_0x1FF;
        [FieldOffset(0x200)] public byte Unk_0x200;
        [FieldOffset(0x2C6)] public byte Unk_0x2C6;
        [FieldOffset(0x3D0)] public GameObject* Actor; // points to local player
        [FieldOffset(0x3E0)] public byte Unk_0x3E0;
        [FieldOffset(0x3EC)] public float Unk_0x3EC;
        [FieldOffset(0x3F0)] public float Unk_0x3F0;
        [FieldOffset(0x418)] public byte Unk_0x418;
        [FieldOffset(0x419)] public byte Unk_0x419;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct MoveControllerSubMemberForMine
    {
        [FieldOffset(0x10)] public Vector3 Direction;
        [FieldOffset(0x20)] public UnkGameObjectStruct* ActorStruct;
        [FieldOffset(0x28)] public uint Unk_0x28;
        [FieldOffset(0x3C)] public byte Moved;
        [FieldOffset(0x3D)] public byte Rotated;
        [FieldOffset(0x3E)] public byte MovementLock;
        [FieldOffset(0x3F)] public byte Unk_0x3F;
        [FieldOffset(0x40)] public byte Unk_0x40;
        [FieldOffset(0x80)] public Vector3 ZoningPosition;
        [FieldOffset(0xF4)] public byte Unk_0xF4;
        [FieldOffset(0x80)] public Vector3 Unk_0x80;
        [FieldOffset(0x90)] public float MoveDir;
        [FieldOffset(0x94)] public byte Unk_0x94;
        [FieldOffset(0xA0)] public Vector3 MoveForward;
        [FieldOffset(0xB0)] public float Unk_0xB0;
        [FieldOffset(0x104)] public byte Unk_0x104;
        [FieldOffset(0x110)] public Int32 WishdirChanged;
        [FieldOffset(0x114)] public float Wishdir_Horizontal;
        [FieldOffset(0x118)] public float Wishdir_Vertical;
        [FieldOffset(0x120)] public byte Unk_0x120;
        [FieldOffset(0x121)] public byte Rotated1;
        [FieldOffset(0x122)] public byte Unk_0x122;
        [FieldOffset(0x123)] public byte Unk_0x123;
    }
}
