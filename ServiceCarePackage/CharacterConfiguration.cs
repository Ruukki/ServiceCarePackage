using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ServiceCarePackage
{
    public class CharacterConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        #region Features
        public bool EnableTranslate { get; set; } = true;
        public bool EnablePuppetMaster { get; set; } = true;
        public bool EnablePuppetMasterHadcore { get; set; } = true;
        public bool EnableForcedWalk { get; set; } = true;
        #endregion

        #region Feature data
        public Dictionary<string, string> OwnerChars { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string? OwnerName { get; set; }
        public string? OwnerWorld { get; set; }
        [JsonIgnore]
        public string OwnerNameFull { get { return $"{OwnerName}@{OwnerWorld}"; } }
        public string? OwnerNameAlias { get; set; }
        public string DisplayName { get; set; } = "Slut";
        public string CommandName { get; set; } = "slut";
        #endregion
    }
}
