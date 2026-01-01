using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCarePackage.Services
{
    public static class Signatures
    {
        // gip.HookFromAddress<ProcessActionEffect>(ss.ScanText(this)
        public const string ReceiveActionEffect = "40 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24";

        // ScanType: Signature
        public const string OnEmote = "E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24";

        // DetourName = nameof(FireCallbackDetour), Fallibility = Fallibility.Auto), Define via SignatureAttribute.
        public const string FireCallback = "E8 ?? ?? ?? ?? 0F B6 E8 8B 44 24 20";

        // Marshal.GetDelegateForFunctionPointer<ForcedStayCallbackDelegate>(ss.ScanText(this))
        public const string Callback = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? BF";

        // ScanType.StaticAddress, Fallibility = Fallibility.Infallible. Define via SignatureAttribute.
        public const string ForceDisableMovement = "F3 0F 10 05 ?? ?? ?? ?? 0F 2E C7";

        // DetourName = nameof(MovementUpdate), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
        public const string MouseMoveBlock = "48 8b C4 48 89 70 ?? 48 89 78 ?? 55 41 56 41 57";

        // DetourName = nameof(TestUpdate), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
        public const string UnfollowTarget = "48 89 5c 24 ?? 48 89 74 24 ?? 57 48 83 ec ?? 48 8b d9 48 8b fa 0f b6 89 ?? ?? 00 00 be 00 00 00 e0";

        // Inner, single paramater that is a post-confirmation of an unfollowing. Works for mouse turning but does not prevent LMB+RMB unfollowing.
        public const string UnfollowTargetPost = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 BF ?? ?? ?? ?? 0F B6 89";

        // Signatures for Imprisonment
        public const string RMICamera = "48 8B C4 53 48 81 EC ?? ?? ?? ?? 44 0F 29 50 ??";

        public const string RMIWalk = "E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D";

        public const string RMIWalkIsInputEnabled1 = "E8 ?? ?? ?? ?? 84 C0 75 10 38 43 3C";

        public const string RMIWalkIsInputEnabled2 = "E8 ?? ?? ?? ?? 84 C0 75 03 88 47 3F";

        // DetourName = nameof(ApplyGlamourPlateDetour), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
        public const string ApplyGlamourPlate = "E8 ?? ?? ?? ?? 41 C6 44 24 ?? ?? E9 ?? ?? ?? ?? 0F B6 83";

        // Sends a constructed chat message to the server. (No longer nessisary)
        // public const string SendChat = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9";

        // DetourName = nameof(ProcessChatInputDetour), Fallibility = Fallibility.Auto, Define via SignatureAttribute.
        public const string ProcessChatInput = "E8 ?? ?? ?? ?? FE 87 ?? ?? ?? ?? C7 87";

        // Spatial Audio Sigs from VFXEDITOR
        internal const string CreateStaticVfx = "E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08";
        internal const string RunStaticVfx = "E8 ?? ?? ?? ?? ?? ?? ?? 8B 4A ?? 85 C9";
        internal const string RemoveStaticVfx = "40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9";

        internal const string CreateActorVfx = "40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8";
        internal const string RemoveActorVfx = "0F 11 48 10 48 8D 05"; // the weird one

        //SendMessage
        public static string ProcessChatBoxEntry = FFXIVClientStructs.FFXIV.Client.UI.UIModule.Addresses.ProcessChatBoxEntry.String; //"48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 45 84 C9"


        // CORBY'S EXPERIMENTAL VOODOO BLACK MAGIC SIGNATURES..

        // related to a condition that changes automove
        // sub_1417229C0(nint a1, nint a2)
        public const string UnkAutoMoveUpdate = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 20 44 0F B6 7A ?? 48 8B D9";
    }
}
