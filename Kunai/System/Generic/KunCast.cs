using SharpNeedle.Framework.Ninja.Csd;
using SharpNeedle.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public struct KunCast
    {
        public string Name;
        public int Type;
        public Vector2 Translation;
        public Vector2 Scale;
        public float Rotation;
        public bool Hidden;
        public int[] UserData;
        public int InstanceFlags;
        public int InheritanceFlags;
        public int MaterialFlags;
        public string Text;
        public string Font;
        public float Kerning;
        public Color<byte> Color;
        public Color<byte> GradientTopLeft;
        public Color<byte> GradientBottomLeft;
        public Color<byte> GradientTopRight;
        public Color<byte> GradientBottomRight;
        public int CropIndex;
        public int[] UsableCropIndices;
        public List<KunCast> Children = new List<KunCast>();
        public KunExtension? Extension;

        public KunCast()
        {
        }
    }
}
