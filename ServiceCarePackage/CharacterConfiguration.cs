using Dalamud.Configuration;
using Dalamud.Game.Network.Structures.InfoProxy;
using ServiceCarePackage.Enums;
using ServiceCarePackage.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ServiceCarePackage
{
    public class CharacterConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public SettingLockLevels SettingLockLevels { get; set; } = SettingLockLevels.NoLock;

        #region Features
        public bool EnableTranslate { get; set; } = true;
        public bool EnablePuppetMaster { get; set; } = true;
        public bool EnablePuppetMasterHadcore { get; set; } = true;
        public bool EnableForcedWalk { get; set; } = true;
        public bool EnableAliasNameChanger { get; set; } = true;
        #endregion

        #region Feature data
        public Dictionary<string, CharData> OwnerChars { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string DisplayName { get; set; } = "Slut";
        public string CommandName { get; set; } = "slut";
        public string AliasColorHex { get; set; } = "#FFFFFF";
        #endregion
    }
}
