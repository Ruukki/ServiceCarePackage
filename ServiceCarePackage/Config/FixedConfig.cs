using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ServiceCarePackage.Config
{
    internal static class FixedConfig
    {
        internal static CharacterConfiguration CharConfig { get; private set; } = new();

        internal static bool CharConfigLoaded { get; private set; } = false;

        internal static string DisplayName { get { return CharConfig.DisplayName; } }        
        internal static string CommandName { get { return CharConfig.CommandName; } }
        internal static bool PuppetMasterHArdcore { get { return CharConfig.EnablePuppetMasterHadcore; } }

        //readonly
        internal static string Name { get { return DisplayName.ToLowerInvariant(); } }
        internal static string CommandRegex
        {
            get
            {
                return @$"(?i)^(?:{Name},)\s+(?:\((.*?)\)|(\w+))"; //original                
            }
        }

        internal static string CommandRegexFull
        {
            get
            {
                return $@"(?i)^(?:{Name},)\s+(?:\((.*?)\)|(.+))"; //Full match            
            }
        }

        public static void LoadFromConfig(CharacterConfiguration config)
        {
            if (config != null)
            {
                CharConfig = config;
                CharConfigLoaded = true;
            }
        }
    }
}
