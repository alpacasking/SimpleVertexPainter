using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace SVTXPainter
{
    public enum PaintType
    {
        All = 0,
        R,
        G,
        B,
        A,
        Smooth,
    }
    public static class SVTXPainterUtils
    {
        public static Mesh GetMesh(GameObject aGO)
        {
            Mesh curMesh = null;
            if (aGO)
            {
                MeshFilter curFilter = aGO.GetComponent<MeshFilter>();
                SkinnedMeshRenderer curSkinnned = aGO.GetComponent<SkinnedMeshRenderer>();

                if (curFilter && !curSkinnned)
                {
                    curMesh = curFilter.sharedMesh;
                }
                if (!curFilter && curSkinnned)
                {
                    curMesh = curSkinnned.sharedMesh;
                }
            }
            return curMesh;
        }

        //Falloff 
        public static float LinearFalloff(float distance, float brushRadius)
        {
            return Mathf.Clamp01(1 - distance / brushRadius);
        }

        // Lerp
        public static Color VTXColorLerp(Color colorA, Color colorB, float value)
        {
            if (value > 1f)
            {
                return colorB;
            }
            if (value < 0f)
            {
                return colorA;
            }
            return new Color(colorA.r + (colorB.r - colorA.r) * value,
                colorA.g + (colorB.g - colorA.g) * value,
                colorA.b + (colorB.b - colorA.b) * value,
                colorA.a + (colorB.a - colorA.a) * value);
        }

        public static Color VTXOneChannelLerp(Color colorA, float intensity, float value, PaintType channel)
        {
            switch (channel)
            {
                case PaintType.R:
                    if (value > 1f)
                    {
                        return new Color(intensity, colorA.g, colorA.b, colorA.a);
                    }
                    if (value < 0f)
                    {
                        return colorA;
                    }
                    return new Color(colorA.r + (intensity - colorA.r) * value,
                        colorA.g,
                        colorA.b,
                        colorA.a);
                case PaintType.G:
                    if (value > 1f)
                    {
                        return new Color(colorA.r, intensity, colorA.b, colorA.a);
                    }
                    if (value < 0f)
                    {
                        return colorA;
                    }
                    return new Color(colorA.r,
                        colorA.g + (intensity - colorA.g) * value,
                        colorA.b,
                        colorA.a);
                case PaintType.B:
                    if (value > 1f)
                    {
                        return new Color(colorA.r, colorA.g, intensity, colorA.a);
                    }
                    if (value < 0f)
                    {
                        return colorA;
                    }
                    return new Color(colorA.r,
                        colorA.g,
                        colorA.b + (intensity - colorA.b) * value,
                        colorA.a);
                case PaintType.A:
                    if (value > 1f)
                    {
                        return new Color(colorA.r, colorA.g, colorA.b, intensity);
                    }
                    if (value < 0f)
                    {
                        return colorA;
                    }
                    return new Color(colorA.r,
                        colorA.g,
                        colorA.b,
                        colorA.a + (intensity - colorA.a) * value);
            }

            //error
            return Color.cyan;
        }

        public static string SanitizeForFileName(string name)
        {
            var reg = new Regex("[\\/:\\*\\?<>\\|\\\"]");
            return reg.Replace(name, "_");
        }
    }
}