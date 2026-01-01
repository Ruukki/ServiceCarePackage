using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCarePackage.Config
{
    internal static class FixedConfig
    {
        internal static string DisplayName { get; set; } = "Slut";        
        internal static string CommandName { get; set; } = DisplayName.ToLowerInvariant();
        internal static bool PuppetMasterHArdcore { get; set; } = true;

        //readonly
        internal static string Name { get { return DisplayName.ToLowerInvariant(); } }
        internal static string CommandRegex
        {
            get
            {
                if (PuppetMasterHArdcore)
                {
                    return $@"(?i)^(?:{Name},)\s+(?:\((.*?)\)|(.+))"; //Full match
                }
                return @$"(?i)^(?:{Name},)\s+(?:\((.*?)\)|(\w+))"; //original                
            }
        }

        public static void LoadFromConfig(Configuration config)
        {
            if (config != null)
            {
                DisplayName = config.DisplayName;
                CommandName = config.CommandName;
                PuppetMasterHArdcore = config.EnablePuppetMasterHadcore;
            }
        }
    }
}
