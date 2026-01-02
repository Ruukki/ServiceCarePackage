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

    

    // The below exists just to make saving less cumbersome
    public void Save(IDalamudPluginInterface pluginInterface)
    {        
        this.Version++;
        pluginInterface.SavePluginConfig(this);
    }
}
