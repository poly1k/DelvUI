﻿using System;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;

namespace DelvUI {
    public static class Extensions {
        public static string Abbreviate(this SeString str) {
            var splits = str.TextValue.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            
            for (var i = 0; i < splits.Length - 1; i++) {
                splits[i] = splits[i][0].ToString();
            }
    
            return string.Join(". ", splits).ToUpper();
        }

        public static Vector4 AdjustColor(this Vector4 vec, float correctionFactor) {
            var red = vec.X;
            var green = vec.Y;
            var blue = vec.Z;

            if (correctionFactor < 0) {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else {
                red = (1 - red) * correctionFactor + red;
                green = (1 - green) * correctionFactor + green;
                blue = (1 - blue) * correctionFactor + blue;
            }

            return new Vector4(red, green, blue, vec.W);
        }
        
        public static string KiloFormat(this uint num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0M");

            if (num >= 1000000)
                return (num / 1000000).ToString("0.#") + "M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0K");

            if (num >= 10000)
                return (num / 1000).ToString("0.#") + "K";

            return num.ToString("#,0");
        } 
        
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength); 
        }
    }
}