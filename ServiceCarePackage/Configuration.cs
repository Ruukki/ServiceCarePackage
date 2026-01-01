using Dalamud.Configuration;
using Dalamud.Plugin;
using ServiceCarePackage.Config;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ServiceCarePackage;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;

    #region Features
    public bool EnableTranslate { get; set; } = true;    
    public bool EnablePuppetMaster { get; set; } = true;
    public bool EnablePuppetMasterHadcore { get; set; } = true;
    public bool EnableForcedWalk { get; set; } = true;
    #endregion

    // Feature data
    public string? OwnerName { get; set; }
    public string? OwnerWorld { get; set; }
    [JsonIgnore]
    public string OwnerNameFull { get { return $"{OwnerName}@{OwnerWorld}"; } }
    public string? OwnerNameAlias { get; set; }
    public string DisplayName { get; set; } = "Slut";
    public string CommandName { get; set; } = "slut";

    // The below exists just to make saving less cumbersome
    public void Save(IDalamudPluginInterface pluginInterface)
    {
        FixedConfig.LoadFromConfig(this);
        pluginInterface.SavePluginConfig(this);
    }
}
