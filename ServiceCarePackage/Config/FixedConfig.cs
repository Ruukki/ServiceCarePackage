using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCarePackage.Config
{
    internal static class FixedConfig
    {
        internal static string DisplayName { get; set; } = "Slut";
        internal static string Name { get { return DisplayName.ToLowerInvariant(); } }
    }
}
