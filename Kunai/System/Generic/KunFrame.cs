using SharpNeedle.Framework.Ninja.Csd.Motions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public enum MotionInterpType : uint
    {
        Const, Linear, Hermite
    }

    public enum TrackType
    {
        HideFlag,
        PositionX,
        PositionY,
        Rotation,
        ScaleX,
        ScaleY,
        SpriteIndex,
        Color,
        GradientTopLeft,
        GradientBottomLeft,
        GradientTopRight,
        GradientBottomRight,
    }
    public struct KunFrame
    {
        public int Frame;
        public MotionInterpType Easing;
        public float InTangent;
        public float OutTangent;
        public uint Field14;
        public KunFrameValue Value;
        public KunExtension? Extension;        
    }
}
