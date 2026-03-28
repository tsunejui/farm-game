using System;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace FarmGame.Core;

public static class ColorHelper
{
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Color.Magenta;

        hex = hex.TrimStart('#');

        if (hex.Length == 6)
        {
            int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            return new Color(r, g, b);
        }

        if (hex.Length == 8)
        {
            int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            int a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
            return new Color(r, g, b, a);
        }

        return Color.Magenta;
    }
}
