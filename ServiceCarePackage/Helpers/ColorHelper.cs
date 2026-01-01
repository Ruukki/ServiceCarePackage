using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Globalization;

namespace ServiceCarePackage.Helpers
{
    public static class ColorHelper
    {
        public static Vector3 HexToVector3Rgb(string hex)
        {
            hex = hex.TrimStart('#');

            if (hex.Length != 6)
                throw new ArgumentException("Hex must be 6 characters.");

            float r = int.Parse(hex[..2], NumberStyles.HexNumber) / 255f;
            float g = int.Parse(hex[2..4], NumberStyles.HexNumber) / 255f;
            float b = int.Parse(hex[4..6], NumberStyles.HexNumber) / 255f;

            return new Vector3(r, g, b);
        }
    }
}
