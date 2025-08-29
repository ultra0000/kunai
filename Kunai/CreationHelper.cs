using Kunai.ShurikenRenderer;
using Kunai.Window;
using SharpNeedle.Framework.Ninja.Csd;
using Shuriken.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Kunai
{
    public static class CreationHelper
    {
        static Cast CreateNewCastFromDefault(string in_Name, Cast in_Parent, Cast.EType in_Type)
        {
            Cast newCast = new Cast();
            newCast.Field2C = 32767;
            newCast.SpriteIndices = new int[32];
            for (int i = 0; i < 32; i++)
                newCast.SpriteIndices[i] = -1;
            newCast.Parent = in_Parent;
            var info = newCast.Info;
            newCast.Type = in_Type;
            newCast.Name = in_Name;
            info.Scale = new Vector2(1, 1);
            info.SpriteIndex = -1;
            newCast.Enabled = true;
            newCast.TopLeft = new Vector2(-25, -25) / KunaiProject.Instance.ViewportSize;
            newCast.BottomLeft = new Vector2(-25, 25) / KunaiProject.Instance.ViewportSize;
            newCast.TopRight = new Vector2(25, -25) / KunaiProject.Instance.ViewportSize;
            newCast.BottomRight = new Vector2(25, 25) / KunaiProject.Instance.ViewportSize;
            info.Color = Vector4.One.ToSharpNeedleColor();
            info.GradientTopLeft = Vector4.One.ToSharpNeedleColor();
            info.GradientBottomLeft = Vector4.One.ToSharpNeedleColor();
            info.GradientTopRight = Vector4.One.ToSharpNeedleColor();
            info.GradientBottomRight = Vector4.One.ToSharpNeedleColor();
            newCast.Info = info;
            return newCast;
        }
        public static Family CreateNewFamily(Scene in_Parent)
        {
            Family newFamily = new Family();
            newFamily.Scene = in_Parent;
            return newFamily;
        }
        public static void CreateNewCast(CsdVisData.Scene in_Scene, Cast.EType in_Type)
        {
            Family newFam = CreateNewFamily(in_Scene.Value.Value);
            Cast newCast = CreateNewCastFromDefault($"Cast_{in_Scene.Casts.Count}", null, in_Type);
            newFam.Add(newCast);
            in_Scene.Value.Value.Families.Add(newFam);
            in_Scene.Casts.Add(new CsdVisData.Cast(newCast, in_Scene));
            foreach(var test in in_Scene.Value.Value.Motions)
            {
                test.Value.FamilyMotions.Add(new SharpNeedle.Framework.Ninja.Csd.Motions.FamilyMotion(newFam));
            }
        }
        public static void CreateNewCast(CsdVisData.Cast in_Cast, Cast.EType in_Type)
        {
            Cast newCast = CreateNewCastFromDefault($"Cast_{in_Cast.Parent.Casts.Count}", null, in_Type);
            in_Cast.Value.Add(newCast);
            in_Cast.Parent.Casts.Add(new CsdVisData.Cast(newCast, in_Cast.Parent));
        }

        public static void CreateNewScene(CsdVisData.Node in_Node)
        {
            List<SharpNeedle.Framework.Ninja.Csd.Sprite> sprites = new List<SharpNeedle.Framework.Ninja.Csd.Sprite>();
            List<Vector2> textures = new List<Vector2>();
            if(in_Node.Scene.Count > 0)
            {
                sprites = in_Node.Scene[0].Value.Value.Sprites;
                textures = in_Node.Scene[0].Value.Value.Textures;
            }
            Scene scene = new Scene();
            scene.Sprites = sprites;
            scene.Textures = textures;
            scene.Version = 3;
            scene.AspectRatio = 16.0f / 9.0f;
            scene.Motions = new CsdDictionary<SharpNeedle.Framework.Ninja.Csd.Motions.Motion>();
            scene.Families = new List<Family>();
            scene.FrameRate = 60;
            var pair = new KeyValuePair<string, Scene>($"New Scene{in_Node.Scene.Count}", scene);
            in_Node.Value.Value.Scenes.Add(pair);
            in_Node.Scene.Add(new CsdVisData.Scene(pair, in_Node));
        }
        static Cast CloneCast(Cast in_Cast, Cast in_Parent, CsdVisData.Cast in_Vis)
        {
            var oldInfo = in_Cast.Info;
            CastInfo newCastInfo = new CastInfo()
            {
                HideFlag = oldInfo.HideFlag,
                Translation = oldInfo.Translation,
                Rotation = oldInfo.Rotation,
                Scale = oldInfo.Scale,
                SpriteIndex = oldInfo.SpriteIndex,
                Color = oldInfo.Color,
                GradientBottomLeft = oldInfo.GradientBottomLeft,
                GradientBottomRight = oldInfo.GradientBottomRight,
                GradientTopLeft = oldInfo.GradientTopLeft,
                GradientTopRight = oldInfo.GradientTopRight,
                UserData0 = oldInfo.UserData0,
                UserData1 = oldInfo.UserData1,
                UserData2 = oldInfo.UserData2
            };
            Cast returnable =  new Cast
            {
                Name = in_Cast.Name + "_clone",
                Field00 = in_Cast.Field00,
                Type = in_Cast.Type,
                Enabled = in_Cast.Enabled,

                TopLeft = in_Cast.TopLeft,
                BottomLeft = in_Cast.BottomLeft,
                TopRight = in_Cast.TopRight,
                BottomRight = in_Cast.BottomRight,

                Field2C = in_Cast.Field2C.Value,
                InheritanceFlags = in_Cast.InheritanceFlags.Value,
                MaterialFlags = in_Cast.MaterialFlags.Value,

                Text = in_Cast.Text,
                FontName = in_Cast.FontName,
                FontKerning = in_Cast.FontKerning,
                Width = in_Cast.Width,
                Height = in_Cast.Height,
                Field58 = in_Cast.Field58,
                Field5C = in_Cast.Field5C,

                Origin = in_Cast.Origin,
                Position = in_Cast.Position,
                Field70 = in_Cast.Field70,

                Info = newCastInfo,

                SpriteIndices = (int[])in_Cast.SpriteIndices.Clone(),
            };
            in_Parent?.Add(returnable);
            if (in_Cast.Children.Count > 0 )
            {
                foreach (var child in in_Cast.Children)
                {
                    CloneCast(child, returnable, in_Vis);                    
                }
            }
            in_Vis.Parent.Casts.Add(new CsdVisData.Cast(returnable, in_Vis.Parent));
            return returnable;
        }
        internal static void DuplicateCast(CsdVisData.Cast cast)
        {
            var castNew = CloneCast(cast.Value, null, cast);
            cast.Value.Parent.Add(castNew);
        }
    }
}
