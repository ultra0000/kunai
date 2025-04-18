using HekonrayBase;
using HekonrayBase.Base;
using Hexa.NET.ImGui;
using Kunai.Modal;
using Kunai.ShurikenRenderer;
using Shuriken.Rendering;
using System;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using TeamSpettro.SettingsSystem;

using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Kunai.Window
{
    public class CropEditor : Singleton<CropEditor>, IWindow
    {
        public static float ZoomFactor = 1;
        public static bool Enabled = false;
        int m_SelectedIndex = 0;
        int m_SelectedSpriteIndex = 0;
        private CropGenerator cropGenerator = new CropGenerator();
        private bool m_ShowAllCoords 
        {
            get { return SettingsManager.GetBool("ShowAllCoordsCrop"); }
            set { SettingsManager.SetBool("ShowAllCoordsCrop", value); }
        }
        public void OnReset(IProgramProject in_Renderer)
        {
            m_SelectedIndex = 0;
            m_SelectedSpriteIndex = 0;
        }
        private void DrawQuadList(SCenteredImageData in_Data)
        {
            var cursorpos = ImGui.GetItemRectMin();
            Vector2 screenPos = in_Data.Position + in_Data.ImagePosition - new Vector2(3, 2);
            var viewSize = in_Data.ImageSize;

            for (int i = 0; i < SpriteHelper.Textures[m_SelectedIndex].CropIndices.Count; i++)
            {
                int spriteIdx = SpriteHelper.Textures[m_SelectedIndex].CropIndices[i];
                var sprite = SpriteHelper.Crops[spriteIdx];
                var qTopLeft = sprite.Crop.TopLeft;
                var qTopRight = new Vector2(sprite.Crop.BottomRight.X, sprite.Crop.TopLeft.Y);
                var qBotLeft = new Vector2(sprite.Crop.TopLeft.X, sprite.Crop.BottomRight.Y);
                var qBotRight = sprite.Crop.BottomRight;
                Vector2 pTopLeft = screenPos + new Vector2(qTopLeft.X * viewSize.X, qTopLeft.Y * viewSize.Y);
                Vector2 pBotRight = screenPos + new Vector2(qBotRight.X * viewSize.X, qBotRight.Y * viewSize.Y);
                Vector2 pTopRight = screenPos + new Vector2(qTopRight.X * viewSize.X, qTopRight.Y * viewSize.Y);
                Vector2 pBotLeft = screenPos + new Vector2(qBotLeft.X * viewSize.X, qBotLeft.Y * viewSize.Y);

                Vector2 mousePos = ImGui.GetMousePos();

                if (m_ShowAllCoords)
                    ImGui.GetWindowDrawList().AddQuad(pTopLeft, pTopRight, pBotRight, pBotLeft, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), 1.5f);

                if (KunaiMath.IsPointInRect(mousePos, pTopLeft, pTopRight, pBotRight, pBotLeft) || i == m_SelectedSpriteIndex)
                {
                    //Add selection box
                    ImGui.GetWindowDrawList().AddQuad(pTopLeft, pTopRight, pBotRight, pBotLeft, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.3f, 0, 1)), 3);
                    if (ImGui.IsMouseClicked(0))
                    {
                        m_SelectedSpriteIndex = i;
                    }
                    //if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    //{
                    //    Vector2 mouseInWindow = mousePos - in_WindowPos;
                    //
                    //    Vector2 adjustedMousePos = mouseInWindow - screenPos;
                    //    quad.OriginalData.OriginCast.Position = adjustedMousePos / Renderer.ViewportSize;
                    //}
                }

            }
        }
        
        public void Render(IProgramProject in_Renderer)
        {
            var renderer = (KunaiProject)in_Renderer;
            if (Enabled)
            {
                if (ImGui.Begin("Crop", ref Enabled, ImGuiWindowFlags.MenuBar))
                {
                    var padding = ImGui.GetStyle().ItemSpacing;
                    var avgSizeWin = (ImGui.GetWindowSize().X / 4) - padding.X;
                    DrawMenuBar();
                    ZoomFactor = Math.Clamp(ZoomFactor, 0.5f, 5);
                    CropListLeft(renderer, avgSizeWin);
                    ImGui.SameLine();
                    ImageViewportTexture(renderer, avgSizeWin);
                    ImGui.SameLine();
                    CropRegionEditor(renderer, avgSizeWin);
                }
                ImGui.End();
                cropGenerator.in_TextureIndex = m_SelectedIndex;
                cropGenerator.Draw();
            }
        }

        private void CropRegionEditor(KunaiProject renderer, float in_AvgSizeWin)
        {
            if (ImGui.BeginListBox("##texturelist2", new Vector2(in_AvgSizeWin, -1)))
            {
                if (SpriteHelper.Textures.Count > m_SelectedIndex)
                {
                    var texture = SpriteHelper.Textures[m_SelectedIndex];
                    var sprite = SpriteHelper.Crops[texture.CropIndices[m_SelectedSpriteIndex]];
                    Vector2 spriteStart = sprite.Start;
                    Vector2 spriteSize = sprite.Dimensions;
                    ImGui.SeparatorText("Texture Info");
                    ImGui.Text($"Name: {texture.Name}");
                    ImGui.Text($"Width: {texture.Width}");
                    ImGui.Text($"Height: {texture.Height}");
                    ImGui.SeparatorText("Crop");
                    ImGui.Text($"Currently editing: Crop ({m_SelectedSpriteIndex})");
                    ImGui.DragFloat2("Position", ref spriteStart, "%.0f");
                    ImGui.DragFloat2("Dimension", ref spriteSize, "%.0f");
                    spriteStart.X = Math.Clamp(spriteStart.X, 0, texture.Size.X);
                    spriteStart.Y = Math.Clamp(spriteStart.Y, 0, texture.Size.Y);

                    spriteSize.X = Math.Clamp(spriteSize.X, 1, texture.Size.X);
                    spriteSize.Y = Math.Clamp(spriteSize.Y, 1, texture.Size.Y);
                    sprite.Start = spriteStart;
                    sprite.Dimensions = spriteSize;
                    sprite.Recalculate();
                }
                ImGui.EndListBox();
            }
        }
        private void ImageViewportTexture(KunaiProject in_Renderer, float in_AvgSizeWin)
        {
            if (!in_Renderer.IsFileLoaded)
                return;
            Vector2 availableSize = new Vector2(ImGui.GetWindowSize().X / 2, ImGui.GetContentRegionAvail().Y);
            Vector2 viewportPos = ImGui.GetWindowPos() + ImGui.GetCursorPos();

            var textureSize = SpriteHelper.Textures[m_SelectedIndex].Size;

            Vector2 imageSize;
            if (textureSize.X > textureSize.Y)
                imageSize = new Vector2(availableSize.Y, (textureSize.Y / textureSize.X) * availableSize.Y);
            else
                imageSize = new Vector2(availableSize.X, (textureSize.X / textureSize.Y) * availableSize.X);

            //Texture Image
            var size2 = ImGui.GetContentRegionAvail().X - in_AvgSizeWin;
            ImKunai.ImageViewport("##cropEdit", new Vector2(size2, -1), SpriteHelper.Textures[m_SelectedIndex].Size.Y / SpriteHelper.Textures[m_SelectedIndex].Size.X, ZoomFactor, new ImTextureID(SpriteHelper.Textures[m_SelectedIndex].GlTex.Id), DrawQuadList, new Vector4(0.5f, 0.5f, 0.5f, 1));

            bool windowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) && ImGui.IsItemHovered();
            if (windowHovered)
                ZoomFactor += ImGui.GetIO().MouseWheel / 5;
        }

        private void CropListLeft(KunaiProject in_Renderer, float in_AvgSizeWin)
        {
            if (ImGui.BeginListBox("##texturelist", new Vector2(in_AvgSizeWin, -1)))
            {
                int idx = 0;
                var result = ImKunai.TextureSelector(in_Renderer, true);
                if (result.TextureIndex != -2)
                    m_SelectedIndex = result.TextureIndex;
                if (result.SpriteIndex != -2)
                    m_SelectedSpriteIndex = result.SpriteIndex;
                ImGui.EndListBox();
            }
        }

        private void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("Add"))
                {
                    if (ImGui.MenuItem("Add Texture"))
                    {
                        var res = NativeFileDialogSharp.Dialog.FileOpen("dds");
                        if (res.IsOk)
                        {
                            if (!SpriteHelper.DoesTextureExist(res.Path))
                            {
                                SpriteHelper.AddTexture(new Texture(res.Path));
                            }
                            else
                            {
                                Application.ShowMessageBoxCross("Error", "A texture with this exact name already exists!\nPlease rename the target texture and try again.");
                            }
                        }
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Export Crops"))
                    {
                        var res = NativeFileDialogSharp.Dialog.FileSave("png");
                        if (res.IsOk)
                        {
                            GenerateImage(res.Path);
                        }
                    }

                    if (ImGui.MenuItem("Generate Crops"))
                    {
                        ImGui.OpenPopup("##generatecrops");
                        cropGenerator.SetEnabled(true);
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Show all crops on texture", m_ShowAllCoords))
                        m_ShowAllCoords = !m_ShowAllCoords;

                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }
        }

        private void GenerateImage(string in_Path)
        {
            var options = new DrawingOptions();
            options.GraphicsOptions = new GraphicsOptions { Antialias = false };
            var imageSize = SpriteHelper.Textures[m_SelectedIndex].Size;
            Image<Rgba32> image = new Image<Rgba32>( (int)imageSize.X, (int)imageSize.Y);
            Random random = new Random();

            Color[] possibleColors =
            [
                new(new Abgr32(255, 255, 255, 100)),
                new(new Abgr32(255, 0, 0, 100)),
                new(new Abgr32(255, 255, 0, 100)),
                new(new Abgr32(255, 0, 255, 100)),
                new(new Abgr32(0, 255, 0, 100)),
                new(new Abgr32(0, 255, 255, 100)),
                new(new Abgr32(0, 0, 255, 100)),
                new(new Abgr32(255, 0, 255, 100))
            ];
            ;
            Color color = new Color(new Abgr32(255, 0, 0, 100));
            Color color2 = new Color(new Abgr32(0, 255, 0, 100));
            for (int i = 0; i < SpriteHelper.Textures[m_SelectedIndex].CropIndices.Count; i++)
            {
                int spriteIdx = SpriteHelper.Textures[m_SelectedIndex].CropIndices[i];
                var sprite = SpriteHelper.Crops[spriteIdx];
                
                
                // Stroke width
                float strokeWidth = 2f; // Adjust as needed

                // Shrink the square inward to keep the stroke inside
                float inset = strokeWidth / 2;
                
                var qTopLeft = sprite.Crop.TopLeft * imageSize;
                var qTopRight = new Vector2(sprite.Crop.BottomRight.X, sprite.Crop.TopLeft.Y) * imageSize;
                var qBotLeft = new Vector2(sprite.Crop.TopLeft.X, sprite.Crop.BottomRight.Y) * imageSize;
                var qBotRight = sprite.Crop.BottomRight * imageSize;
                
                
                Vector2 topLeftInner = qTopLeft + new Vector2(inset, inset);
                Vector2 topRightInner = qTopRight + new Vector2(-inset, inset);
                Vector2 bottomRightInner = qBotRight + new Vector2(-inset, -inset);
                Vector2 bottomLeftInner = qBotLeft + new Vector2(inset, -inset);
                
                var outlinePathBuilder = new PathBuilder();
                outlinePathBuilder.AddLine(qTopLeft, qTopRight);
                outlinePathBuilder.AddLine(qTopRight, qBotRight);
                outlinePathBuilder.AddLine(qBotRight, qBotLeft);
                outlinePathBuilder.AddLine(qBotLeft, qTopLeft);
                var outlinePath = outlinePathBuilder.Build();
                
                var polygon = new Polygon(new LinearLineSegment(new PointF[]  { qTopLeft, qTopRight, qBotRight, qBotLeft }));
                var colorToUse = possibleColors[random.Next(0, possibleColors.Length)];
                
                image.Mutate(in_Ctx => 
                    in_Ctx.Fill(options, colorToUse, outlinePath)
                );
                
            }
            image.SaveAsPng(in_Path);
        }
    }
}
