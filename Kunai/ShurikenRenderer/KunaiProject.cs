using Amicitia.IO.Binary;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using ColoursXncpGen;
using HekonrayBase;
using HekonrayBase.Base;
using Kunai.Window;
using libWiiSharp;
using SharpNeedle.Framework.Ninja;
using SharpNeedle.Framework.Ninja.Csd;
using SharpNeedle.IO;
using SharpNeedle.Resource;
using SharpNeedle.Utilities;
using Shuriken.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using SharpNeedle.Framework.Ninja.Csd.Motions;
using TeamSpettro.SettingsSystem;
using Image = SixLabors.ImageSharp.Image;
using Hexa.NET.OpenGL;
using System.Xml.Serialization;
using System.Xml;
using Kunai.Generic;
using System.Reflection;

namespace Kunai.ShurikenRenderer
{

    public partial class KunaiProject : Singleton<KunaiProject>, IProgramProject
    {
        public SProjectConfig Config;
        public SSelectionData SelectionData;
        public SReferenceImageData ReferenceImageData;
        public Vector2 ViewportSize;
        public Vector3 ViewportColor = new Vector3(-1, -1, -1);
        public Vector2 ScreenSize;
        public Renderer Renderer;
        public CsdVisData VisibilityData;
        public CsdProject WorkProjectCsd;
        public List<WindowBase> Windows = new List<WindowBase>();
        private SViewportData m_ViewportData;
        private HekonrayMainWindow m_Window;
        private bool m_SaveScreenshotWhenRendered;
        private float m_DeltaTime;

        public bool IsFileLoaded
        {
            get
            {
                return !string.IsNullOrEmpty(Config.WorkFilePath);
            }
        }

        public KunaiProject()
        {
            ViewportSize = new Vector2(1280, 720);
            Renderer = new Renderer((int)ViewportSize.X, (int)ViewportSize.Y);
            Renderer.SetShader(Renderer.ShaderDictionary["basic"]);
            m_ViewportData = new SViewportData();
            Config = new SProjectConfig();
        }
        public void SetViewportColor(Vector3 in_Color)
        {
            ViewportColor = in_Color;
            SettingsManager.SetFloat("ViewColor_X", in_Color.X);
            SettingsManager.SetFloat("ViewColor_Y", in_Color.Y);
            SettingsManager.SetFloat("ViewColor_Z", in_Color.Z);
        }
        public void SetWindowParameters(HekonrayMainWindow in_Window2, Vector2 in_ClientSize)
        {
            ScreenSize = in_ClientSize;
            m_Window = in_Window2;

        }

        public void ShowMessageBoxCross(string in_Title, string in_Message, bool in_IsWarning)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                System.Windows.MessageBox.Show(in_Message, in_Title, System.Windows.MessageBoxButton.OK, in_IsWarning ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);
            }
        }


        public void LoadReferenceImage(string in_Path)
        {
            ReferenceImageData.Texture = new Texture(in_Path);
            ReferenceImageData.Sprite = new KunaiSprite(ReferenceImageData.Texture);
            ReferenceImageData.Enabled = true;
        }

        public void LoadFile(string in_Path)
        {
            try
            {
                if (!File.Exists(in_Path))
                    return;
                ResetCsd();
                bool isSplitFile = IsPathSplitFile(@in_Path);
                //Reset what needs to be reset
                string root = Path.GetDirectoryName(Path.GetFullPath(@in_Path));
                SpriteHelper.ClearTextures();
                Config.WorkFilePath = in_Path;

                if (isSplitFile)
                {
                    // There's probably a better way to detect if a tls or dxl file exists, but 
                    // this should work.
                    bool isTlsFilePresent = File.Exists(Path.ChangeExtension(in_Path, "tls")) || File.Exists(Path.ChangeExtension(in_Path, ".tls.tpl"));
                    bool isDxlFilePresent = File.Exists(Path.ChangeExtension(in_Path, "dxl"));

                    if (isTlsFilePresent || isDxlFilePresent)
                    {
                        // Colors and Colors Ultimate have a unique situation where
                        // they have texture lists as tls (wii format) and dxl (literally just TextureList)
                        // instead of being combined into the file with the project
                        // as is the case literally everywhere else except for shadow!                    
                        HandleSplitCsdColors(in_Path, isDxlFilePresent, isTlsFilePresent);
                    }
                    else
                    {
                        //File probably uses TXD or has no file asssociated with it, try to continue anyway.
                        WorkProjectCsd = new CsdProject();
                        WorkProjectCsd.Name = Path.GetFileName(in_Path);
                        WorkProjectCsd.Project = GetProjectChunkSplit(in_Path, Endianness.Big);
                        WorkProjectCsd.Textures = new TextureListNN();
                        Application.ShowMessageBoxCross("Warning", "This file is split, but the program does not know where the textures are.\nThis file will be displayed with no textures.", 1);
                    }
                }
                else
                {
                    WorkProjectCsd = ResourceUtility.Open<CsdProject>(@in_Path);
                }


                //Start loading textures
                var csdTextureList = WorkProjectCsd.Textures;
                var csdFontList = WorkProjectCsd.Project.Fonts;
                List<string> missingTextures = new List<string>();

                //If the texture list isnt null, add the textures to SpriteHelper
                if (csdTextureList != null)
                {
                    foreach (ITexture texture in csdTextureList)
                    {
                        if(texture is TextureMirage)
                        {
                            TextureMirage mirageTex = (TextureMirage)texture;

                            //Some files may have embedded textures inside the file itself
                            if(mirageTex.Data != null)
                            {
                                if(mirageTex.Data.Length != 0)
                                {
                                    SpriteHelper.Textures.Add(new Texture($"texture{SpriteHelper.Textures.Count}", mirageTex.Data));
                                    continue;
                                }
                            }
                        }
                        string texPath = Path.Combine(@root, texture.Name);
                        SpriteHelper.Textures.Add(new Texture(texPath));

                        //This is used to warn the user about missing textures
                        if (!File.Exists(texPath))
                        {
                            missingTextures.Add(texture.Name);
                        }
                    }
                    if (missingTextures.Count > 0)
                    {
                        string textureNames = "";
                        foreach (string textureName in missingTextures)
                            textureNames += "-" + textureName + "\n";
                        Application.ShowMessageBoxCross("Warning", $"The file uses textures that could not be found, they will be replaced with squares.\n\nMissing Textures:\n{textureNames}", 1);
                    }
                }

                //Create all necessary crops for Kunai
                SpriteHelper.LoadCrops(WorkProjectCsd);

                //Create vis data (necessary for UI)
                VisibilityData = new CsdVisData(WorkProjectCsd);

            }
            catch (Exception ex)
            {
                //In case of any errors, dont handle it in debug, so that it can be debugged
#if !DEBUG
                Application.ShowMessageBoxCross("Error", $"An error occured whilst trying to load a file.\n{ex.Message}", 2);

#else
                throw;
#endif
            }
            //Send the OnReset message to all active windows.
            SendResetSignal();
        }

        public void ResetCsd()
        {
            Config.WorkFilePath = "";
            //Dispose the old file
            WorkProjectCsd?.BaseFile?.Dispose();
            WorkProjectCsd?.Dispose();
            WorkProjectCsd = null;
            //Reset vis data
            VisibilityData = null;
            //Clear quads (shouldnt be necessary)
            Renderer.Quads.Clear();
            SelectionData.SelectedCast = null;
            SelectionData.SelectedScene = null;
        }

        /// <summary>
        /// Resets all windows.
        /// </summary>
        private void SendResetSignal()
        {
            m_Window.ResetWindows(this);
        }

        /// <summary>
        /// Checks if the header of the file starts with FAPC/CPAF or not.
        /// </summary>
        /// <param name="in_Path">Path to file</param>
        /// <returns></returns>
        private bool IsPathSplitFile(string @in_Path)
        {
            BinaryObjectReader reader = new BinaryObjectReader(in_Path, Endianness.Little, Encoding.UTF8);
            uint sig = reader.ReadNative<uint>();
            reader.Dispose();
            return sig != BinaryPrimitives.ReverseEndianness(CsdPackage.Signature) && sig != CsdPackage.Signature;
        }
        /// <summary>
        /// Reads a split *ncp file, which is usually just the Project chunk.
        /// </summary>
        /// <param name="in_Path"></param>
        /// <param name="in_Endianness"></param>
        /// <returns></returns>
        private ProjectChunk GetProjectChunkSplit(string in_Path, Endianness in_Endianness)
        {
            using var reader = new BinaryObjectReader(@in_Path, in_Endianness, Encoding.UTF8);
            var infoChunk = reader.ReadObject<InfoChunk>();
            foreach (IChunk chunk in infoChunk.Chunks)
            {
                switch (chunk)
                {
                    case ProjectChunk project:
                        return project;
                }
            }
            return null;
        }

        /// <summary>
        /// Combines dxl file if its SCU, extracts tls textures if its Colors Wii.
        /// </summary>
        /// <param name="in_Path">Path to the Csd file</param>
        /// <param name="in_IsDxlFilePresent"></param>
        /// <param name="in_IsTlsFilePresent"></param>
        private void HandleSplitCsdColors(string in_Path, bool in_IsDxlFilePresent, bool in_IsTlsFilePresent)
        {
            string pathExtra = in_IsDxlFilePresent ? Path.ChangeExtension(in_Path, "dxl") : Path.ChangeExtension(in_Path, "tls");
            byte[] csdFile = File.ReadAllBytes(in_Path);
            byte[] textureList = File.ReadAllBytes(pathExtra);

            // TLS files on the Wii are files that contain textures
            // AFAIK, these textures have no names whatsoever,
            // they are just stored as 0,1,2,3,4,5 etc.
            // This extracts the TLS file (which is TPL) and
            // converts the TGA images to DDS, so that Kunai can read them.
            if (in_IsTlsFilePresent)
            {
                TPL tlsFile = TPL.Load(pathExtra);
                TextureListNN newTexList = new TextureListNN();

                string csdName = Path.GetFileNameWithoutExtension(in_Path);
                string parentDir = Directory.GetParent(in_Path).FullName;

                for (var i = 0; i < tlsFile.NumOfTextures; i++)
                {
                    string filePath = Path.Combine(parentDir, $"{csdName}_tex{i}.dds");
                    if (!File.Exists(filePath))
                    {
                        byte[] iPixelData = tlsFile.ExtractTextureBytes(i);
                        using Image<Bgra32> imgDds = Image.LoadPixelData<Bgra32>(iPixelData, tlsFile.GetTexture(i).TextureWidth, tlsFile.GetTexture(i).TextureHeight);

                        //This is an encoder for Bc DDS, don't touch
                        BcEncoder encoder = new BcEncoder();
                        encoder.OutputOptions.GenerateMipMaps = true;
                        encoder.OutputOptions.Quality = CompressionQuality.BestQuality;
                        encoder.OutputOptions.Format = CompressionFormat.Bc3;
                        encoder.OutputOptions.FileFormat = OutputFileFormat.Dds;

                        //Write
                        using FileStream fs = File.OpenWrite(filePath);
                        encoder.EncodeToStream(imgDds.CloneAs<Rgba32>(), fs);
                    }

                    newTexList.Add(new TextureNN($"{csdName}_tex{i}.dds"));
                }

                WorkProjectCsd = new CsdProject();
                WorkProjectCsd.Project = GetProjectChunkSplit(in_Path, Endianness.Big);
                WorkProjectCsd.Textures = newTexList;
            }

            // SCU is the only case where split XNCPs exist
            // They are far simpler than split GNCPs, as they
            // only have a separated TextureList.
            // All of this is based on ColoursXncpGen by PTKay
            if (in_IsDxlFilePresent)
            {
                //Merge both files using the same method as ColoursXncpGen
                byte[] output = FileManager.Combine(csdFile, textureList);
                using (var memstr = new MemoryStream(output))
                {
                    VirtualFile file = new VirtualFile(Path.GetFileName(in_Path), new VirtualDirectory(Directory.GetParent(in_Path).FullName));
                    file.BaseStream = memstr;
                    WorkProjectCsd = ResourceManager.Instance.Open<CsdProject>(file);
                }
            }
        }
        public double GetFPS()
        {
            return Math.Round(1.0 / m_DeltaTime);
        }
        /// <summary>
        /// Renders contents of a CsdProject to a GL texture for use in ImGui.
        /// (NOTE: if anyone wants to improve this, feel free to do so, it sucks.)
        /// </summary>
        /// <param name="in_CsdProject"></param>
        /// <param name="in_DeltaTime"></param>
        /// <exception cref="Exception"></exception>
        public void Render(CsdProject in_CsdProject, float in_DeltaTime)
        {
            m_DeltaTime = in_DeltaTime;
            if (ViewportColor.X == -1)
            {
                ViewportColor.X = SettingsManager.GetFloat("ViewColor_X", 0.6627450980f);
                ViewportColor.Y = SettingsManager.GetFloat("ViewColor_Y", 0.6627450980f);
                ViewportColor.Z = SettingsManager.GetFloat("ViewColor_Z", 0.66274509803f);
            }
            //If one or both of these are 0, it means the application is minimized.
            if (ScreenSize.X == 0 || ScreenSize.Y == 0)
                return;

            bool isSavingScreenshot = m_SaveScreenshotWhenRendered;

            Vector2 wsize = ScreenSize;
            Vector2Int wsizei;
            ManageRenderTexture(isSavingScreenshot, wsize, out wsizei);

            GLSingle.Ins.Enable(GLEnableCap.Blend);
            //eventually set to transparent in case its a screenshot
            if (IsFileLoaded)
                GLSingle.Ins.ClearColor(ViewportColor.X, ViewportColor.Y, ViewportColor.Z, 1);
            else
                GLSingle.Ins.ClearColor(ViewportColor.X / 3, ViewportColor.Y / 3, ViewportColor.Z / 3, 1);

            GLSingle.Ins.Clear(GLClearBufferMask.ColorBufferBit | GLClearBufferMask.DepthBufferBit);

            //Actually render the file
            if (in_CsdProject != null)
                RenderToViewport(in_CsdProject, in_DeltaTime, isSavingScreenshot);


            if (isSavingScreenshot)
            {
                //Save framebuffer to a pixel buffer
                byte[] buffer = new byte[wsizei.X * wsizei.Y * 4];
                unsafe
                {
                fixed(void* buf = buffer)
                    GLSingle.Ins.ReadPixels(0, 0, wsizei.X, wsizei.Y, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, buf);
                }

                Image<Rgba32> screenshot =
                    Image.LoadPixelData<Rgba32>(buffer, wsizei.X, wsizei.Y);

                //Flip vertically to fix orientation
                screenshot.Mutate(in_X => in_X.Flip(FlipMode.Vertical));

                var fileDialog = NativeFileDialogSharp.Dialog.FileSave("png");
                if (fileDialog.IsOk)
                {
                    string path = fileDialog.Path;
                    if (!Path.HasExtension(path))
                        path += ".png";
                    screenshot.SaveAsPng(path);
                }

                m_SaveScreenshotWhenRendered = false;
            }
            // unbind our bo so nothing else uses it
            GLSingle.Ins.BindFramebuffer(GLFramebufferTarget.Framebuffer, 0);
            GLSingle.Ins.Viewport(0, 0, m_Window.WindowSize.X, m_Window.WindowSize.Y); // back to full screen size

            UpdateWindows();
        }

        private void ManageRenderTexture(bool isSavingScreenshot, Vector2 wsize, out Vector2Int wsizei)
        {
            wsizei = new((int)wsize.X, (int)wsize.Y);
            if (m_ViewportData.FramebufferSize != wsizei || isSavingScreenshot)
            {
                m_ViewportData.FramebufferSize = wsizei;

                // create our frame buffer if needed
                if (m_ViewportData.FramebufferHandle == 0)
                {
                    m_ViewportData.FramebufferHandle = GLSingle.Ins.GenFramebuffer();
                    // bind our frame buffer
                    GLSingle.Ins.BindFramebuffer(GLFramebufferTarget.Framebuffer, m_ViewportData.FramebufferHandle);
                    //GLSingle.Ins.ObjectLabel(ObjectLabelIdentifier.Framebuffer, m_ViewportData.FramebufferHandle, 10, "GameWindow");
                }

                // bind our frame buffer
                GLSingle.Ins.BindFramebuffer(GLFramebufferTarget.Framebuffer, m_ViewportData.FramebufferHandle);

                if (m_ViewportData.CsdRenderTextureHandle > 0)
                    GLSingle.Ins.DeleteTexture(m_ViewportData.CsdRenderTextureHandle);

                m_ViewportData.CsdRenderTextureHandle = GLSingle.Ins.GenTexture();
                GLSingle.Ins.BindTexture(GLTextureTarget.Texture2D, m_ViewportData.CsdRenderTextureHandle);
                //GLSingle.Ins.ObjectLabel(ObjectLabelIdentifier.Texture, m_ViewportData.CsdRenderTextureHandle, 16, "GameWindow:Color");
                //IMPORTANT!
                //Rgba for screenshots, rgb for everything else
                var pixelInternalFormat = isSavingScreenshot ? GLInternalFormat.Rgba : GLInternalFormat.Rgb;
                var pixelFormat = isSavingScreenshot ? GLPixelFormat.Rgba : GLPixelFormat.Rgb;
                GLSingle.Ins.TexImage2D(GLTextureTarget.Texture2D, 0, pixelInternalFormat, wsizei.X, wsizei.Y, 0, pixelFormat, GLPixelType.UnsignedByte, IntPtr.Zero);
                GLSingle.Ins.TexParameterf(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter, (int)GLTextureMinFilter.Linear);
                GLSingle.Ins.TexParameterf(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter, (int)GLTextureMagFilter.Linear);
                GLSingle.Ins.FramebufferTexture2D(GLFramebufferTarget.Framebuffer, GLFramebufferAttachment.ColorAttachment0, GLTextureTarget.Texture2D, m_ViewportData.CsdRenderTextureHandle, 0);

                if (m_ViewportData.RenderbufferHandle > 0)
                    GLSingle.Ins.DeleteRenderbuffer(m_ViewportData.RenderbufferHandle);

                m_ViewportData.RenderbufferHandle = GLSingle.Ins.GenRenderbuffer();
                GLSingle.Ins.BindRenderbuffer(GLRenderbufferTarget.Renderbuffer, m_ViewportData.RenderbufferHandle);
                //GLSingle.Ins.ObjectLabel(ObjectLabelIdentifier.Renderbuffer, m_ViewportData.RenderbufferHandle, 16, "GameWindow:Depth");
                GLSingle.Ins.RenderbufferStorage(GLRenderbufferTarget.Renderbuffer, GLInternalFormat.DepthComponent32F, wsizei.X, wsizei.Y);

                GLSingle.Ins.FramebufferRenderbuffer(GLFramebufferTarget.Framebuffer, GLFramebufferAttachment.DepthAttachment, GLRenderbufferTarget.Renderbuffer, m_ViewportData.RenderbufferHandle);
                //GLSingle.Ins.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

                //texDepth = GLSingle.Ins.GenTexture();
                //GLSingle.Ins.BindTexture(GLTextureTarget.Texture2D, texDepth);
                //GLSingle.Ins.TexImage2D(GLTextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32f, 800, 600, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                //GLSingle.Ins.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, GLTextureTarget.Texture2D, texDepth, 0);

                // make sure the frame buffer is complete
                GLFramebufferStatus errorCode = (GLFramebufferStatus)GLSingle.Ins.CheckFramebufferStatus(GLFramebufferTarget.Framebuffer);
                if (errorCode == GLFramebufferStatus.IncompleteAttachment)
                    return;
                if (errorCode != GLFramebufferStatus.Complete)
                    throw new Exception();
            }
            else
            {
                // bind our frame and depth buffer
                GLSingle.Ins.BindFramebuffer(GLFramebufferTarget.Framebuffer, m_ViewportData.FramebufferHandle);
                GLSingle.Ins.BindRenderbuffer(GLRenderbufferTarget.Renderbuffer, m_ViewportData.RenderbufferHandle);
            }
            GLSingle.Ins.Viewport(0, 0, wsizei.X, wsizei.Y); // change the viewport to window
        }

        private void RenderToViewport(CsdProject in_CsdProject, float in_DeltaTime, bool in_ScreenshotMode)
        {
            Renderer.Width = (int)ViewportSize.X;
            Renderer.Height = (int)ViewportSize.Y;
            Renderer.Start();
            if (Config.PlayingAnimations)
                Config.Time += in_DeltaTime;

            if (ReferenceImageData.Enabled)
            {
                Renderer.DrawFullscreenQuad(ReferenceImageData.Sprite, ReferenceImageData.Opacity);
            }

            DrawNode(in_CsdProject.Project.Root, Config.Time);
            foreach (KeyValuePair<string, SceneNode> node in in_CsdProject.Project.Root.Children)
            {
                if (!VisibilityData.GetVisibility(node.Value).Active) continue;
                DrawNode(node.Value, Config.Time);
            }
            Renderer.End();
        }

        public void DrawNode(SceneNode in_Node, double in_DeltaTime)
        {
            CsdVisData.Node vis = VisibilityData.GetVisibility(in_Node);
            int idx = 0;
            foreach (var scene in in_Node.Scenes)
            {
                if (!vis.GetVisibility(scene.Value).Active) continue;
                DrawScene(scene.Value, vis, ref idx, in_DeltaTime);
                // = true;
            }
        }
        public void DrawScene(Scene in_Scene, CsdVisData.Node in_Vis, ref int in_Priority, double in_DeltaTime)
        {
            var vis = in_Vis.GetVisibility(in_Scene);
            foreach (var family in in_Scene.Families)
            {
                var transform = new SSpriteDrawData();
                transform.Scale = Vector2.One;
                transform.Color = new Vector4(1, 1, 1, 1);
                if (family.Casts.Count == 0)
                    continue;
                Cast cast = family.Casts[0];

                DrawCast(in_Scene, cast, transform, in_Priority, (float)(in_DeltaTime * in_Scene.FrameRate), vis);
                in_Priority += cast.Children.Count + 1;
            }
        }
        /// <summary>
        /// Applies animation values to casts.
        /// </summary>
        /// <param name="in_SpriteDraw"></param>
        /// <param name="in_Vis"></param>
        /// <param name="in_OutSpriteIndex"></param>
        /// <param name="in_UiElement"></param>
        /// <param name="in_Time"></param>
        private void ApplyAnimationValues(ref SSpriteDrawData in_SpriteDraw, ref CsdVisData.Scene in_Vis, ref float in_OutSpriteIndex, Cast in_UiElement, float in_Time)
        {
            //Redo this at some point
            foreach (CsdVisData.Animation animation in in_Vis.Animation)
            {
                if (!animation.Active) continue;

                foreach (FamilyMotion familyMotion in animation.Value.Value.FamilyMotions)
                {
                    foreach (CastMotion castMotion in familyMotion.CastMotions)
                    {
                        if (castMotion.Cast != in_UiElement || castMotion.Capacity == 0) continue;
                        foreach (KeyFrameList track in castMotion)
                        {
                            if (track.Count == 0)
                                continue;

                            switch (track.Property)
                            {
                                case KeyProperty.HideFlag:
                                    in_SpriteDraw.Hidden = track.GetSingle(in_Time) != 0;
                                    break;

                                case KeyProperty.PositionX:
                                    in_SpriteDraw.Position.X = track.GetSingle(in_Time);
                                    break;

                                case KeyProperty.PositionY:
                                    in_SpriteDraw.Position.Y = track.GetSingle(in_Time);
                                    break;

                                case KeyProperty.Rotation:
                                    in_SpriteDraw.Rotation = track.GetSingle(in_Time);
                                    break;

                                case KeyProperty.ScaleX:
                                    in_SpriteDraw.Scale.X = track.GetSingle(in_Time);
                                    break;

                                case KeyProperty.ScaleY:
                                    in_SpriteDraw.Scale.Y = track.GetSingle(in_Time);
                                    break;

                                case KeyProperty.SpriteIndex:
                                    in_OutSpriteIndex = track.GetSingle(in_Time);
                                    break;

                                case KeyProperty.Color:
                                    in_SpriteDraw.Color = track.GetColor(in_Time);
                                    break;

                                case KeyProperty.GradientTopLeft:
                                    in_SpriteDraw.GradientTopLeft = track.GetColor(in_Time);
                                    break;

                                case KeyProperty.GradientBottomLeft:
                                    in_SpriteDraw.GradientBottomLeft = track.GetColor(in_Time);
                                    break;

                                case KeyProperty.GradientTopRight:
                                    in_SpriteDraw.GradientTopRight = track.GetColor(in_Time);
                                    break;

                                case KeyProperty.GradientBottomRight:
                                    in_SpriteDraw.GradientBottomRight = track.GetColor(in_Time);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies inheritance flags to casts.
        /// </summary>
        /// <param name="in_SpriteDraw"></param>
        /// <param name="in_Inheritance"></param>
        /// <param name="in_Transform"></param>
        private void ApplyInheritance(ref SSpriteDrawData in_SpriteDraw, ElementInheritanceFlags in_Inheritance, SSpriteDrawData in_Transform)
        {
            // Inherit position
            if ((in_Inheritance & ElementInheritanceFlags.InheritXPosition) != 0)
                in_SpriteDraw.Position.X += in_Transform.Position.X;

            if ((in_Inheritance & ElementInheritanceFlags.InheritYPosition) != 0)
                in_SpriteDraw.Position.Y += in_Transform.Position.Y;

            // Inherit rotation
            if ((in_Inheritance & ElementInheritanceFlags.InheritRotation) != 0)
                in_SpriteDraw.Rotation += in_Transform.Rotation;

            // Inherit scale
            if ((in_Inheritance & ElementInheritanceFlags.InheritScaleX) != 0)
                in_SpriteDraw.Scale.X *= in_Transform.Scale.X;

            if ((in_Inheritance & ElementInheritanceFlags.InheritScaleY) != 0)
                in_SpriteDraw.Scale.Y *= in_Transform.Scale.Y;

            // Inherit color
            if ((in_Inheritance & ElementInheritanceFlags.InheritColor) != 0)
            {
                in_SpriteDraw.Color *= in_Transform.Color;
            }
        }
        private void DrawCast(Scene in_Scene, Cast in_UiElement, SSpriteDrawData in_Parent, int in_Priority, float in_Time, CsdVisData.Scene in_Vis)
        {
            float sprId = in_UiElement.Info.SpriteIndex;
            SSpriteDrawData sSpriteDrawData = new SSpriteDrawData(in_UiElement, in_Scene);
            float angle = in_Parent.Rotation * MathF.PI / 180.0f; //to radians

            ApplyAnimationValues(ref sSpriteDrawData, ref in_Vis, ref sprId, in_UiElement, in_Time);
            if (sSpriteDrawData.Hidden)
                return;

            var visibilityDataCast = in_Vis.GetVisibility(in_UiElement);
            if (visibilityDataCast == null)
            {
                Console.WriteLine("CRITICAL ERROR! Missing visibility for cast!!! Please fix!");
                return;
            }
            // Inherit position scale
            // TODO: Is this handled through flags?
            // UPDATE: might actually be something the game doesnt do
            sSpriteDrawData.Position.X *= in_Parent.Scale.X;
            sSpriteDrawData.Position.Y *= in_Parent.Scale.Y;

            // Rotate through parent transform
            float rotatedX = sSpriteDrawData.Position.X * MathF.Cos(angle) * in_Scene.AspectRatio + sSpriteDrawData.Position.Y * MathF.Sin(angle);
            float rotatedY = sSpriteDrawData.Position.Y * MathF.Cos(angle) - sSpriteDrawData.Position.X * MathF.Sin(angle) * in_Scene.AspectRatio;

            sSpriteDrawData.Position.X = rotatedX / in_Scene.AspectRatio;
            sSpriteDrawData.Position.Y = rotatedY;

            sSpriteDrawData.Position += in_UiElement.Origin;
            ApplyInheritance(ref sSpriteDrawData, (ElementInheritanceFlags)in_UiElement.InheritanceFlags.Value, in_Parent);
            ApplyPropertyMask(ref sSpriteDrawData, (CastPropertyMask)in_UiElement.Field2C.Value);
            var type = in_UiElement.Type;

            if (visibilityDataCast.Active && in_UiElement.Enabled)
            {
                switch (type)
                {
                    case Cast.EType.Sprite:
                        {
                            int spriteIdx1 = Math.Min(in_UiElement.SpriteIndices.Length - 1, (int)sprId);
                            int spriteIdx2 = Math.Min(in_UiElement.SpriteIndices.Length - 1, (int)sprId + 1);
                            KunaiSprite spr = sprId >= 0 ? SpriteHelper.TryGetSprite(in_UiElement.SpriteIndices[spriteIdx1]) : null;
                            KunaiSprite nextSpr = sprId >= 0 ? SpriteHelper.TryGetSprite(in_UiElement.SpriteIndices[spriteIdx2]) : null;

                            spr ??= nextSpr;
                            nextSpr ??= spr;
                            sSpriteDrawData.NextSprite = nextSpr;
                            sSpriteDrawData.SpriteFactor = sprId % 1;
                            sSpriteDrawData.Sprite = spr;
                            Renderer.DrawSprite(sSpriteDrawData);
                            break;
                        }
                    case Cast.EType.Font:
                        {
                            float xOffset = 0.0f;
                            if (string.IsNullOrEmpty(in_UiElement.Text)) in_UiElement.Text = "";
                            foreach (char character in in_UiElement.Text)
                            {

                                var font = WorkProjectCsd.Project.Fonts[in_UiElement.FontName];
                                if (font == null)
                                    continue;

                                KunaiSprite spr = null;

                                foreach (var mapping in font)
                                {
                                    if (mapping.SourceIndex != character)
                                        continue;

                                    spr = SpriteHelper.TryGetSprite(mapping.DestinationIndex);
                                    break;
                                }

                                if (spr == null)
                                    continue;

                                float width = spr.Dimensions.X / Renderer.Width;
                                float height = spr.Dimensions.Y / Renderer.Height;

                                var begin = (Vector2)in_UiElement.TopLeft;
                                var end = begin + new Vector2(width, height);

                                sSpriteDrawData.OverrideUvCoords = true;
                                sSpriteDrawData.TopLeft = new Vector2(begin.X + xOffset, begin.Y);
                                sSpriteDrawData.BottomLeft = new Vector2(begin.X + xOffset, end.Y);
                                sSpriteDrawData.TopRight = new Vector2(end.X + xOffset, begin.Y);
                                sSpriteDrawData.BottomRight = new Vector2(end.X + xOffset, end.Y);

                                sSpriteDrawData.Sprite = spr;
                                sSpriteDrawData.NextSprite = spr;
                                Renderer.DrawSprite(sSpriteDrawData);
                                xOffset += width + in_UiElement.FontKerning;
                            }
                            break;
                        }
                    default:
                        {
                            Renderer.DrawEmptyQuad(sSpriteDrawData);
                            break;
                        }
                }
                foreach (var child in in_UiElement.Children)
                    DrawCast(in_Scene, child, sSpriteDrawData, in_Priority++, in_Time, in_Vis);
            }

        }

        private void ApplyPropertyMask(ref SSpriteDrawData in_SSpriteDrawData, CastPropertyMask in_Field2C)
        {
            if ((in_Field2C & CastPropertyMask.ApplyTransform) == 0)
            {
                in_SSpriteDrawData.Position = Vector2.Zero;
            }
            else
            {
                if ((in_Field2C & CastPropertyMask.ApplyTranslationX) == 0)
                    in_SSpriteDrawData.Position.X = 0;

                if ((in_Field2C & CastPropertyMask.ApplyTranslationY) == 0)
                    in_SSpriteDrawData.Position.Y = 0;
            }
            if ((in_Field2C & CastPropertyMask.ApplyRotation) == 0)
                in_SSpriteDrawData.Rotation = 0;

            if ((in_Field2C & CastPropertyMask.ApplyScaleX) == 0)
                in_SSpriteDrawData.Scale.X = 1;

            if ((in_Field2C & CastPropertyMask.ApplyScaleY) == 0)
                in_SSpriteDrawData.Scale.Y = 1;

            if ((in_Field2C & CastPropertyMask.ApplyColor) == 0)
                in_SSpriteDrawData.Color = new Vector4(1, 1, 1, 1);

            if ((in_Field2C & CastPropertyMask.ApplyColorBl) == 0)
                in_SSpriteDrawData.GradientBottomLeft = new Vector4(1, 1, 1, 1);

            if ((in_Field2C & CastPropertyMask.ApplyColorBr) == 0)
                in_SSpriteDrawData.GradientBottomRight = new Vector4(1, 1, 1, 1);

            if ((in_Field2C & CastPropertyMask.ApplyColorTl) == 0)
                in_SSpriteDrawData.GradientTopLeft = new Vector4(1, 1, 1, 1);

            if ((in_Field2C & CastPropertyMask.ApplyColorTr) == 0)
                in_SSpriteDrawData.GradientTopRight = new Vector4(1, 1, 1, 1);
        }

        public uint GetViewportImageHandle()
        {
            return m_ViewportData.CsdRenderTextureHandle;
        }
        void RecursiveSetCropListNode(SceneNode in_Node, List<Sprite> in_Sprites, List<Vector2> in_TexSizes)
        {
            foreach (var s in in_Node.Scenes)
            {
                s.Value.Sprites = in_Sprites;
                s.Value.Textures = in_TexSizes;
                foreach (var a in s.Value.Motions)
                {

                    try
                    {
                        float maxFrame = int.MinValue;

                        foreach (var familyMotion in a.Value.FamilyMotions)
                        {
                            foreach (var castMotionList in familyMotion.CastMotions)
                            {
                                foreach (var keyframeList in castMotionList)
                                {
                                    foreach (var keyframe in keyframeList.Frames)
                                    {
                                        if (keyframe.Frame > maxFrame)
                                        {
                                            maxFrame = keyframe.Frame;
                                        }
                                    }
                                }
                            }
                        }

                        if (a.Value.EndFrame < maxFrame)
                            a.Value.EndFrame = maxFrame;
                    }
                    catch (InvalidOperationException e)
                    {
                        continue;
                    }
                }
            }
            foreach (var c in in_Node.Children)
            {
                RecursiveSetCropListNode(c.Value, in_Sprites, in_TexSizes);
            }
        }
        public EFileType GetFileType(string in_Path)
        {
            switch (Path.GetExtension(in_Path))
            {
                case ".xncp": return EFileType.CsdXncp;
                case ".yncp": return EFileType.CsdYncp;
                case ".gncp": return EFileType.CsdGncp;
                case ".sncp": return EFileType.CsdSncp;
            }
            return EFileType.CsdXncp;
        }
        public void SaveCurrentFile(string in_Path)
        {
            KunaiProjectFile file = new KunaiProjectFile();
            CsdPlugin plugin = new CsdPlugin();
            file = plugin.Import("");

            //List<Sprite> subImageList = new();
            //List<Vector2> sizes = new List<Vector2>();
            //SpriteHelper.BuildCropList(ref subImageList, ref sizes);
            //RecursiveSetCropListNode(WorkProjectCsd.Project.Root, subImageList, sizes);
            string filePath = string.IsNullOrEmpty(in_Path) ? Config.WorkFilePath : in_Path;

            var tempFolder = Directory.GetParent(filePath).FullName;

            file.Write(Path.Combine(tempFolder, "output.json"));
            var f = plugin.Export(file);
            f.Write(Path.Combine(tempFolder, "output.xncp"));
            switch (GetFileType(filePath))
            {
                case EFileType.CsdXncp:
                case EFileType.CsdSncp:
                    {
                        WorkProjectCsd.Endianness = Endianness.Little;
                        break;
                    }
                case EFileType.CsdYncp:
                case EFileType.CsdGncp:
                    {
                        WorkProjectCsd.Endianness = Endianness.Big;
                        break;
            
                    }
            }
            //VisibilityData.Apply();
            WorkProjectCsd.Write(filePath);
        }

        internal void UpdateWindows()
        {
            foreach (WindowBase window in Windows)
            {
                window.Renderer = this;
                window.Update(this);
            }
        }

        internal void SaveScreenshot()
        {
            m_SaveScreenshotWhenRendered = true;
        }
        void CreatePackageFile(IChunk in_Chunk, string in_Path, Endianness in_Endianness)
        {
            using BinaryObjectWriter infoWriter = new BinaryObjectWriter(in_Path, Endianness.Little, Encoding.UTF8);
            InfoChunk info = new()
            {
                Signature = BinaryHelper.MakeSignature<uint>(in_Endianness == Endianness.Little ? "NXIF" : "NYIF"),
            };
            info.Chunks.Add(in_Chunk);
            infoWriter.WriteObject(info);
        }
        internal void ExportProjectChunk(string in_Path, bool in_Ultimate)
        {
            string path = string.IsNullOrEmpty(in_Path) ? Config.WorkFilePath : in_Path;
            if (in_Ultimate)
            {
                CreatePackageFile(WorkProjectCsd.Project, path, Endianness.Little);
                CreatePackageFile(WorkProjectCsd.Textures, Path.ChangeExtension(path, "dxl"), Endianness.Little);
            }
            else
            {
                CreatePackageFile(WorkProjectCsd.Project, path, Endianness.Big);
                ShowMessageBoxCross("Warning", "This program can't export tls files.\nYou will have to create them yourself using BrawlBox.", true);
            }
        }
    }
}
