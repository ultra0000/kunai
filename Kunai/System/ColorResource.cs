using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace Kunai
{
    public static class ColorResource
    {
        public static Vector4 SceneNode { get { return new System.Numerics.Vector4(0.992156f, 0.76078f, 0, 1); } }
        public static Vector4 Scene { get { return new System.Numerics.Vector4(0, 0.75f, 0.48039f, 1); } }
        public static Vector4 CastNull { get { unsafe { return *ImGui.GetStyleColorVec4(ImGuiCol.Text); } } }
        public static Vector4 CastSprite { get { return new Vector4(0.26666f, 0.69411f, 1, 1);} }
        public static Vector4 CastFont { get { return new Vector4(1, 0.509803f, 0.15686274f, 1); } }

        //Anims
        public static Vector4 HideFlag { get { return new Vector4(1.0f, 0.388f, 0.278f, 1.0f); } }
        public static Vector4 PositionX { get { return new Vector4(0.118f, 0.565f, 1.0f, 1.0f); } }
        public static Vector4 PositionY { get { return new Vector4(0.196f, 0.804f, 0.196f, 1.0f); } }
        public static Vector4 Rotation { get { return new Vector4(1.0f, 0.647f, 0.0f, 1.0f); } }
        public static Vector4 ScaleX { get { return new Vector4(0.729f, 0.333f, 0.827f, 1.0f); } }
        public static Vector4 ScaleY { get { return new Vector4(0.275f, 0.510f, 0.706f, 1.0f); } }
        public static Vector4 SpriteIndex { get { return new Vector4(0.863f, 0.078f, 0.235f, 1.0f); } }
        public static Vector4 Color { get { return new Vector4(1.0f, 0.843f, 0.0f, 1.0f); } }
        public static Vector4 GradientTopLeft { get { return new Vector4(0.275f, 1.0f, 0.706f, 1.0f); } }
        public static Vector4 GradientBottomLeft { get { return new Vector4(1.0f, 0.412f, 0.706f, 1.0f); } }
        public static Vector4 GradientTopRight { get { return new Vector4(0.0f, 0.749f, 1.0f, 1.0f); } }
        public static Vector4 GradientBottomRight { get { return new Vector4(0.604f, 0.804f, 0.196f, 1.0f); } }

    }
}
