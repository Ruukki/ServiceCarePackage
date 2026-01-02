using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceCarePackage.Models
{
    public sealed record CharacterKey(string Name, string World)
    {
        public override string ToString() => $"{Name}@{World}";

        public static bool TryParse(string s, out CharacterKey? key)
        {
            var i = s.LastIndexOf('@');
            if (i <= 0 || i >= s.Length - 1) { key = default; return false; }
            key = new CharacterKey(s[..i], s[(i + 1)..]);
            return true;
        }
    }
}
