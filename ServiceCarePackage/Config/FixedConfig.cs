using Dalamud.Game.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ServiceCarePackage.Config
{
    internal static class FixedConfig
    {
        internal static CharacterConfiguration CharConfig { get; private set; } = new();

        internal static bool CharConfigLoaded { get; private set; } = false;

        internal static string DisplayName { get { return CharConfig.DisplayName; } }        
        internal static string CommandName { get { return CharConfig.CommandName; } }

        //runtime features
        internal static bool IsActive_ChatHider { get; set; } = false;

        //readonly
        internal static XivChatType[] BaseChatTypes
        {
            get
            {
                return Enum.GetValues<XivChatType>().Where(x =>
                {
                    int xInt = Convert.ToInt32(x);
                    return (xInt < 56 || (xInt > 71 && xInt < 108)) && xInt != 12;
                }).ToArray();
            }
        }
        internal static string Name { get { return DisplayName.ToLowerInvariant(); } }

        internal static string CommandRegexBase(string commandName)
        {
                return @$"(?i)^(?:{CommandName},)\s(?:({commandName})$)"; //original 
        }

        internal static string CommandRegexLock { get { return @$"(?i)^(?:{CommandName},)\s(?:(lock)\s((?:none)|(?:basic)|(?:full))$)"; } }

        internal static string CommandRegex { get { return @$"(?i)^(?:{CommandName},)\s(?:(\w+)$)"; } }

        internal static string CommandRegexFull { get { return $@"(?i)^(?:{CommandName},)\s+(?:\((.+)\))"; } }

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
