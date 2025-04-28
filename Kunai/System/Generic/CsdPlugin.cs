using Kunai.ShurikenRenderer;
using SharpNeedle.Framework.Ninja.Csd;
using SharpNeedle.Framework.Ninja.Csd.Motions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Kunai.Generic.KunFont;

namespace Kunai.Generic
{
    public class CsdPlugin : IImportExport<CsdProject>
    {
        public CsdProject Export(KunaiProjectFile in_File)
        {
            CsdProject proj = new CsdProject();
            var projChunk = new ProjectChunk();
            projChunk.Field08 = in_File.Field08;
            projChunk.Field0C = in_File.Field0C;
            projChunk.Name = in_File.Name;
            projChunk.TextureFormat = in_File.TextureFormat;

            // Scene nodes
            projChunk.Root = ExportSceneNode(in_File.Nodes[0]);

            // Fonts
            projChunk.Fonts = new();
            foreach (var xmlFont in in_File.Fonts)
            {
                Font font = new Font();
                foreach (var xmlChar in xmlFont.Characters)
                {
                    font.Add(ExportFontChara(xmlChar));
                }
                projChunk.Fonts.Add(xmlFont.Name, font);
            }

            // Textures
            if(projChunk.TextureFormat == TextureFormat.Mirage)
            {
                proj.Textures = new TextureListMirage();
            }
            if (projChunk.TextureFormat == TextureFormat.NextNinja)
            {
                proj.Textures = new TextureListNN();
            }
            foreach (var tex in in_File.Texture)
            {
                proj.Textures.Add(ExportTexture(tex));
            }
            proj.Project = projChunk;
            return proj;
            // You can optionally save to file using `out_Path` if needed
            // SaveToDisk(out_Project, out_Path); // (if you have this kind of method)
        }

        public ITexture ExportTexture(KunTexture in_Tex)
        {
            if (in_Tex.Extension is CsdNNTexture ext)
            {
                TextureNN texture = new TextureNN
                {
                    Name = in_Tex.Name,
                    Field00 = ext.Field00,
                    Field08 = ext.Field08,
                    Field0A = ext.Field0A,
                    Field0C = ext.Field0C,
                    Field10 = ext.Field10
                };
                return texture;
            }
            else
            {
                TextureMirage texture = new TextureMirage
                {
                    Name = in_Tex.Name
                };
                return texture;
            }
        }
        public KeyFrame ExportMotFrame(KunFrame xmlFrame, TrackType type)
        {
            var frame = new KeyFrame();
            frame.Frame = xmlFrame.Frame;
            frame.Interpolation = (InterpolationType)xmlFrame.Easing;
            frame.InTangent = xmlFrame.InTangent;
            frame.OutTangent = xmlFrame.OutTangent;
            frame.Field14 = xmlFrame.Field14;

            
            if (type is TrackType.PositionX or TrackType.PositionY or TrackType.ScaleX or TrackType.ScaleY or TrackType.Rotation)
                frame.Value = new KeyFrame.Union((float)xmlFrame.Value.Float);
            else if (type is TrackType.Color or TrackType.GradientTopLeft or TrackType.GradientBottomLeft or TrackType.GradientTopRight or TrackType.GradientBottomRight)
                frame.Value = new KeyFrame.Union((SharpNeedle.Structs.Color<byte>)xmlFrame.Value.Color);
            else
                frame.Value = new KeyFrame.Union((uint)xmlFrame.Value.Uint);

            if(xmlFrame.Extension is CsdKeyframeExtension ext)
            {
                frame.Correction = ext.Correction;
            }
            return frame;
        }

        public KeyFrameList ExportMotTrack(KunTrack xmlTrack)
        {
            var track = new KeyFrameList
            {
                Field00 = xmlTrack.Field00,
                Property = (KeyProperty)xmlTrack.TrackType,
                Frames = new List<KeyFrame>()
            };

            foreach (var xmlFrame in xmlTrack.Frames)
            {
                track.Frames.Add(ExportMotFrame(xmlFrame, xmlTrack.TrackType));
            }

            return track;
        }

        public CastMotion ExportCastMot(KunCastMotion xmlMot)
        {
            var castMotion = new CastMotion
            {
                Flags = (uint)(xmlMot.Flags)
            };

            foreach (var xmlTrack in xmlMot.Tracks)
            {
                castMotion.Add(ExportMotTrack(xmlTrack));
            }

            return castMotion;
        }

        public Motion ExportMotion(KunMotion xmlMotion, KunScene xmlScene)
        {
            var motion = new Motion
            {
                StartFrame = xmlMotion.StartFrame,
                EndFrame = xmlMotion.EndFrame,
                FamilyMotions = new List<FamilyMotion>()
            };
            foreach(var f in xmlScene.Casts)
            {
                motion.FamilyMotions.Add(new FamilyMotion());
            }

            return motion;
        }

        public Cast ExportCast(KunCast xmlCast, Scene in_Scene, int in_FamilyIdx)
        {
            var cast = new Cast
            {
                Name = xmlCast.Name,
                Type = (Cast.EType)xmlCast.Type,
                Text = xmlCast.Text,
                FontKerning = xmlCast.Kerning,
                FontName = xmlCast.Font,
                Field2C = (uint)(xmlCast.InstanceFlags),
                InheritanceFlags = (uint)(xmlCast.InheritanceFlags),
                MaterialFlags = (uint)(xmlCast.MaterialFlags),
                SpriteIndices = xmlCast.UsableCropIndices,
                Position = xmlCast.Extension.GetExtensionAs<CsdCastExtension>().CsePosition,
                Info = new CastInfo
                {
                    Translation = xmlCast.Translation,
                    Scale = xmlCast.Scale,
                    Rotation = xmlCast.Rotation,
                    HideFlag = xmlCast.Hidden ? 1u : 0u,
                    SpriteIndex = xmlCast.CropIndex,
                    UserData0 = (uint)xmlCast.UserData[0],
                    UserData1 = (uint)xmlCast.UserData[1],
                    UserData2 = (uint)xmlCast.UserData[2],
                    Color = xmlCast.Color,
                    GradientBottomLeft = xmlCast.GradientBottomLeft,
                    GradientBottomRight = xmlCast.GradientBottomRight,
                    GradientTopRight = xmlCast.GradientTopRight,
                    GradientTopLeft = xmlCast.GradientTopLeft,
                }
            };

            foreach (var anim in xmlCast.Animations)
            {
                foreach (var c in in_Scene.Motions)
                {
                    if (c.Key == anim.AnimationName)
                    {
                        var exp = ExportCastMot(anim);
                        cast.AttachMotion(exp);
                        in_Scene.Motions[c.Key].FamilyMotions[in_FamilyIdx].CastMotions.Add(exp);
                    }
                }
            }
            foreach (var child in xmlCast.Children)
            {
                cast.Children.Add(ExportCast(child, in_Scene, in_FamilyIdx));
            }
            if(xmlCast.Extension is CsdCastExtension ext)
            {
                cast.Origin = ext.Origin;
                cast.Field00 = ext.Field00;
                cast.Enabled = ext.Enabled;
                cast.TopLeft = ext.TopLeft;
                cast.BottomLeft = ext.BottomLeft;
                cast.TopRight = ext.TopRight;
                cast.BottomRight = ext.BottomRight;
                cast.Width = (uint)ext.CseSize.X;
                cast.Height = (uint)ext.CseSize.Y;
                cast.Field58 = ext.Field58;
                cast.Field5C = ext.Field5C;
                cast.Field70 = ext.Field70;
            }
            return cast;
        }

        public Scene ExportScene(KunScene xmlScene)
        {
            var scene = new Scene
            {
                Priority = (uint)xmlScene.Priority,
                FrameRate = (uint)xmlScene.Framerate,
                AspectRatio = xmlScene.AspectRatio
            };


            foreach (var motion in xmlScene.Motions)
            {
                var mot = ExportMotion(motion, xmlScene);
                scene.Motions.Add(motion.Name, mot);
            }
            for (int i = 0; i < xmlScene.Casts.Count; i++)
            {
                List<KunCast> xmlFamily = xmlScene.Casts[i];
                var family = new Family();
                if (xmlFamily.Count > 0)
                {
                    var cast = ExportCast(xmlFamily[0], scene, i);
                    family.Add(cast);
                }
                family.Scene = scene;
                scene.Families.Add(family);
            }
            foreach(var mot in scene.Motions)
            {
                mot.Value.Attach(scene);
            }
            if(xmlScene.Extension is CsdSceneExtension ext)
            {
                scene.Textures = ext.Textures;
                scene.Sprites = ext.Sprites;
            }
            return scene;
        }

        public SceneNode ExportSceneNode(KunSceneNode xmlNode)
        {
            var node = new SceneNode();

            foreach (var xmlScene in xmlNode.Scenes)
            {
                node.Scenes.Add(xmlScene.Name, ExportScene(xmlScene));
            }

            foreach (var child in xmlNode.Children)
            {
                node.Children.Add(child.Name, ExportSceneNode(child));
            }

            return node;
        }

        public SharpNeedle.Framework.Ninja.Csd.CharacterMapping ExportFontChara(XmlCharacter xmlChar)
        {
            return new SharpNeedle.Framework.Ninja.Csd.CharacterMapping
            {
                SourceIndex = xmlChar.SourceIndex,
                DestinationIndex = xmlChar.DestinationIndex
            };
        }

        public KunFrame ImportMotFrame(TrackType in_Type, KeyFrame in_Frame)
        {
            KunFrame node = new KunFrame();
            node.Frame = in_Frame.Frame;
            node.Easing = (MotionInterpType)(int)in_Frame.Interpolation;
            node.InTangent = in_Frame.InTangent;
            node.OutTangent = in_Frame.OutTangent;
            node.Field14 = in_Frame.Field14;
            if (in_Type == TrackType.PositionX || in_Type == TrackType.PositionY || in_Type == TrackType.ScaleX || in_Type == TrackType.ScaleY || in_Type == TrackType.Rotation)
                node.Value.Float = in_Frame.Value.Float;
            else if (in_Type == TrackType.Color || in_Type == TrackType.GradientTopLeft || in_Type == TrackType.GradientBottomLeft || in_Type == TrackType.GradientTopRight || in_Type == TrackType.GradientBottomRight)
                node.Value.Color = in_Frame.Value.Color;
            else
                node.Value.Uint = in_Frame.Value.Uint;
            node.Extension = new CsdKeyframeExtension(in_Frame);
            return node;
        }
        public KunTrack ImportMotTrack(KeyFrameList in_Track)
        {
            KunTrack node = new();
            node.Field00 = in_Track.Field00;
            node.TrackType = (TrackType)in_Track.Property;
            node.Frames = new List<KunFrame>();
            foreach (var f in in_Track.Frames)
            {
                node.Frames.Add(ImportMotFrame(node.TrackType, f));
            }
            return node;
        }
        public KunCastMotion ImportCastMot(string in_MotName, CastMotion in_Mot)
        {
            KunCastMotion node = new();
            node.AnimationName = in_MotName;
            node.Flags = in_Mot.Flags.Value;
            node.Tracks = new List<KunTrack>();
            foreach (KeyFrameList track in in_Mot)
            {
                node.Tracks.Add(ImportMotTrack(track));
            }
            return node;
        }
        public KunMotion ImportMotion(string in_Name, Motion in_Mot)
        {
            KunMotion node = new KunMotion();
            node.Name = in_Name;
            node.StartFrame = in_Mot.StartFrame;
            node.EndFrame = in_Mot.EndFrame;            
            return node;
        }
        public KunCast ImportCast(string in_Name, Cast in_Cast, Scene in_ParentScene)
        {
            KunCast node = new KunCast();
            node.Name = in_Name;
            node.UserData = new int[3];
            node.Type = (int)in_Cast.Type;
            node.Translation = in_Cast.Info.Translation;
            node.Scale = in_Cast.Info.Scale;
            node.Rotation = in_Cast.Info.Rotation;
            node.Hidden = in_Cast.Info.HideFlag == 1;
            node.UserData[0] = (int)in_Cast.Info.UserData0;
            node.UserData[1] = (int)in_Cast.Info.UserData1;
            node.UserData[2] = (int)in_Cast.Info.UserData2;
            node.InstanceFlags = (int)in_Cast.Field2C.Value;
            node.InheritanceFlags = (int)in_Cast.InheritanceFlags.Value;
            node.MaterialFlags = (int)in_Cast.MaterialFlags.Value;
            node.CropIndex = (int)in_Cast.Info.SpriteIndex;
            node.UsableCropIndices = in_Cast.SpriteIndices;
            node.Color = in_Cast.Info.Color;
            node.Text = in_Cast.Text;
            node.Kerning = in_Cast.FontKerning;
            node.Font = in_Cast.FontName;
            node.GradientBottomLeft = in_Cast.Info.GradientBottomLeft;
            node.GradientBottomRight = in_Cast.Info.GradientBottomRight;
            node.GradientTopRight = in_Cast.Info.GradientTopRight;
            node.GradientTopLeft = in_Cast.Info.GradientTopLeft;

            var ext = new CsdCastExtension(in_Cast);
            node.Extension = ext;
            foreach(var in_Mot in in_ParentScene.Motions)
            {
                foreach (FamilyMotion fam in in_Mot.Value.FamilyMotions)
                {
                    foreach (CastMotion castMot in fam.CastMotions)
                    {
                        if(castMot.Cast == in_Cast)
                        {
                            node.Animations.Add(ImportCastMot(in_Mot.Key, castMot));
                        }
                    }
                }
            }            
            foreach (var c in in_Cast.Children)
            {
                node.Children.Add(ImportCast(c.Name, c, in_ParentScene));
            }
            return node;
        }
        public KunScene ImportScene(string in_Name, Scene in_Scene)
        {
            KunScene node = new KunScene();
            node.Name = in_Name;
            node.Priority = (int)in_Scene.Priority;
            node.Framerate = (int)in_Scene.FrameRate;
            node.AspectRatio = in_Scene.AspectRatio;
            node.Casts = new List<List<KunCast>>();
            foreach (var family in in_Scene.Families)
            {
                List<KunCast> kFamily = new List<KunCast>();
                kFamily.Add(ImportCast(family.Casts[0].Name, family.Casts[0], in_Scene));
                node.Casts.Add(kFamily);
            }
            node.Motions = new List<KunMotion>();
            foreach (var motions in in_Scene.Motions)
            {
                node.Motions.Add(ImportMotion(motions.Key, motions.Value));
            }
            node.Extension = new CsdSceneExtension(in_Scene);
            return node;
        }
        public KunSceneNode ImportSceneNode(string in_Name, SceneNode in_Node)
        {
            KunSceneNode node = new KunSceneNode();
            if (string.IsNullOrEmpty(in_Name))
                in_Name = "Root";
            node.Name = in_Name;
            node.Scenes = new List<KunScene>();
            foreach (var scene in in_Node.Scenes)
            {
                node.Scenes.Add(ImportScene(scene.Key, scene.Value));
            }
            node.Children = new List<KunSceneNode>();
            foreach (var child in in_Node.Children)
            {
                node.Children.Add(ImportSceneNode(child.Key, child.Value));
            }
            return node;
        }
        public KunTexture ImportTexNN(TextureNN tex)
        {
            KunTexture node = new();
            node.Name = tex.Name;
            node.Extension = new CsdNNTexture(tex);
            return node;
        }

        public KunTexture ImportTexMir(TextureMirage tex)
        {
            KunTexture node = new();
            node.Name = tex.Name;
            if (tex.Data != null)
            {
                if (tex.Data.Length != 0)
                {
                    node.TextureData = tex.Data;
                }
            }
            return node;
        }
        public XmlCharacter ImportFontChara(SharpNeedle.Framework.Ninja.Csd.CharacterMapping in_Csd)
        {
            XmlCharacter node = new XmlCharacter();
            node.SourceIndex = in_Csd.SourceIndex;
            node.DestinationIndex = in_Csd.DestinationIndex;
            return node;
        }

        public KunaiProjectFile Import(string in_Path)
        {
            var in_Project = KunaiProject.Instance.WorkProjectCsd;
            KunaiProjectFile file = new KunaiProjectFile();
            var projChunk = in_Project.Project;
            file.Name = projChunk.Name;
            file.Field08 = projChunk.Field08;
            file.Field0C = projChunk.Field0C;
            file.TextureFormat = projChunk.TextureFormat;

            file.Nodes.Clear();
            file.Nodes.Add(ImportSceneNode("", projChunk.Root));
            file.Fonts.Clear();
            foreach (var font in projChunk.Fonts)
            {
                KunFont kFont = new KunFont();
                kFont.Name = font.Key;
                kFont.Characters = new List<KunFont.XmlCharacter>();
                foreach (var char1 in font.Value)
                {
                    kFont.Characters.Add(ImportFontChara(char1));
                }
                file.Fonts.Add(kFont);
            }
            file.Texture.Clear();
            if (file.TextureFormat == TextureFormat.Mirage)
            {
                foreach (var tex in in_Project.Textures)
                {
                    file.Texture.Add(ImportTexMir((TextureMirage)tex));
                }
            }
            if (file.TextureFormat == TextureFormat.NextNinja)
            {
                foreach (var tex in in_Project.Textures)
                {
                    file.Texture.Add(ImportTexNN((TextureNN)tex));
                }
            }
            return file;
        }
    }
}
