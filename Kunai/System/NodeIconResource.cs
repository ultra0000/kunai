using IconFonts;


namespace Kunai
{
    public static class NodeIconResource
    {
        private static SIconData sceneNode = new SIconData(FontAwesome6.FolderClosed, ColorResource.SceneNode);
        private static SIconData scene = new SIconData(FontAwesome6.Film, ColorResource.Scene);
        private static SIconData castNull = new SIconData(FontAwesome6.SquarePlus, ColorResource.CastNull);
        private static SIconData castSpr = new SIconData(FontAwesome6.Image, ColorResource.CastSprite);
        private static SIconData castFont = new SIconData(FontAwesome6.Font, ColorResource.CastFont);
        private static SIconData hideFlag = new SIconData(FontAwesome6.Square, "Hide Flag", ColorResource.HideFlag);
        private static SIconData positionX = new SIconData(FontAwesome6.LeftRight, "X Translation", ColorResource.PositionX);
        private static SIconData positionY = new SIconData(FontAwesome6.UpDown, "Y Translation", ColorResource.PositionY);
        private static SIconData rotation = new SIconData(FontAwesome6.ArrowsRotate, "Rotation", ColorResource.Rotation);
        private static SIconData scaleX = new SIconData(FontAwesome6.Expand, "X Scale", ColorResource.ScaleX);
        private static SIconData scaleY = new SIconData(FontAwesome6.UpRightAndDownLeftFromCenter, "Y Scale", ColorResource.ScaleY);
        private static SIconData spriteIndex = new SIconData(FontAwesome6.PhotoFilm, "Crop", ColorResource.SpriteIndex);
        private static SIconData color = new SIconData(FontAwesome6.Palette, "Color", ColorResource.Color);
        private static SIconData gradientTopLeft = new SIconData(FontAwesome6.Palette, "TL Color", ColorResource.GradientTopLeft);
        private static SIconData gradientBottomLeft = new SIconData(FontAwesome6.Palette, "BL Color", ColorResource.GradientBottomLeft);
        private static SIconData gradientTopRight = new SIconData(FontAwesome6.Palette, "TR Color", ColorResource.GradientTopRight);
        private static SIconData gradientBottomRight = new SIconData(FontAwesome6.Palette, "BR Color", ColorResource.GradientBottomRight);


        public static SIconData SceneNode => sceneNode;
        public static SIconData Scene => scene;
        public static SIconData CastNull => castNull;
        public static SIconData CastSprite => castSpr;
        public static SIconData CastFont => castFont;
        public static SIconData HideFlag => hideFlag;
        public static SIconData PositionX => positionX;
        public static SIconData PositionY => positionY;
        public static SIconData Rotation => rotation;
        public static SIconData ScaleX => scaleX;
        public static SIconData ScaleY => scaleY;
        public static SIconData SpriteIndex => spriteIndex;
        public static SIconData Color => color;
        public static SIconData GradientTopLeft => gradientTopLeft;
        public static SIconData GradientBottomLeft => gradientBottomLeft;
        public static SIconData GradientTopRight => gradientTopRight;
        public static SIconData GradientBottomRight => gradientBottomRight;
    }
}
