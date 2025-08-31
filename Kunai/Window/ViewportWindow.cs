using Hexa.NET.ImGui;
using IconFonts;
using Kunai.ShurikenRenderer;
using System;
using System.Numerics;
using SharpNeedle.Framework.Ninja.Csd;
using System.Collections.Generic;
using System.Linq;
using HekonrayBase.Base;
using HekonrayBase;
using Kunai;
using Hexa.NET.ImGuizmo;
using TeamSpettro.SettingsSystem;

namespace Kunai.Window
{
    public class ViewportWindow : Singleton<ViewportWindow>, IWindow
    {
        private float m_ZoomFactor = 1;
        private int m_CurrentAspectRatio = 0;

        public void OnReset(IProgramProject in_Renderer)
        {
        }
        public void Render(IProgramProject in_Renderer)
        {
            var renderer = (KunaiProject)in_Renderer;
            var size1 = ImGui.GetWindowViewport().Size.X / 4.5f;
            var windowPos = new Vector2(size1, MenuBarWindow.MenuBarHeight);
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(size1 * 2.5f, ImGui.GetWindowViewport().Size.Y / 1.5f - MenuBarWindow.MenuBarHeight), ImGuiCond.Always);
            if (ImGui.Begin("Viewport", MainWindow.WindowFlags))
            {
                bool windowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);
                if (windowHovered)
                    m_ZoomFactor += ImGui.GetIO().MouseWheel / 5;
                m_ZoomFactor = Math.Clamp(m_ZoomFactor, 0.5f, 5);

                ImKunai.TextFontAwesome(FontAwesome6.MagnifyingGlass);
                ImGui.SameLine();

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetContentRegionAvail().X / 5));
                ImGui.SliderFloat("##zoom", ref m_ZoomFactor, 0.5f, 5);
                ImGui.SameLine();
                ImKunai.TextFontAwesome(FontAwesome6.Display);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##aspectratio", ref m_CurrentAspectRatio, ["16:9", "4:3"], 2))
                {
                    if (m_CurrentAspectRatio == 0)
                        renderer.ViewportSize = new Vector2(1280, 720);
                    else
                        renderer.ViewportSize = new Vector2(640, 480);
                }

                ImKunai.ImageViewport("##viewportcenter", 
                    new Vector2(-1, -1),
                    renderer.ViewportSize.Y / renderer.ViewportSize.X,
                    m_ZoomFactor,
                    new ImTextureID(renderer.GetViewportImageHandle()),
                    DrawQuadList);
                
                ImGui.End();
            }
        }

        private void DrawQuadList(SCenteredImageData in_Data)
        {
            var renderer = KunaiProject.Instance;
            var viewSize = in_Data.ImageSize;
            Vector2 screenPos = in_Data.Position + in_Data.ImagePosition - new Vector2(3, 2);
            if (!renderer.IsFileLoaded)
            {
                var size = ImGui.CalcTextSize("Open a XNCP, YNCP, GNCP or SNCP file to edit it.");
                ImGui.GetWindowDrawList().AddText((screenPos + new Vector2(0.5f * viewSize.X, 0.5f * viewSize.Y)) - (size/2), 0xFFFFFFFF, "Open a XNCP, YNCP, GNCP or SNCP file to edit it.");                
            }
            var cursorpos = ImGui.GetItemRectMin();

            List<Cast> possibleSelections = new List<Cast>();

            //ImGui.GetWindowDrawList().AddCircle(screenPos, 10, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));
            foreach (var quad in renderer.Renderer.Quads)
            {
                if (quad.OriginalData.Unselectable)
                    continue;
                
                var qTopLeft = quad.TopLeft.Position;
                var qBotRight = quad.BottomRight.Position;
                var qTopRight = quad.TopRight.Position;
                var qBotLeft = quad.BottomLeft.Position;
                Vector2 pTopLeft = screenPos + new Vector2(qTopLeft.X * viewSize.X, qTopLeft.Y * viewSize.Y);
                Vector2 pBotRight = screenPos + new Vector2(qBotRight.X * viewSize.X, qBotRight.Y * viewSize.Y);
                Vector2 pTopRight = screenPos + new Vector2(qTopRight.X * viewSize.X, qTopRight.Y * viewSize.Y);
                Vector2 pBotLeft = screenPos + new Vector2(qBotLeft.X * viewSize.X, qBotLeft.Y * viewSize.Y);

                
                Vector2 mousePos = ImGui.GetMousePos();
                var cast = quad.OriginalData.OriginCast;
                //Vector2 pcenter = screenPos + new Vector2(quadCenter.X * vwSize.X, quadCenter.Y * vwSize.Y);
                //ImGui.GetWindowDrawList().AddCircle(pcenter, 10, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));

                //Null cast indicator
                if (cast.Type == Cast.EType.Null && SettingsWindow.ShowNullCasts)
                {
                    Vector2 center = KunaiMath.CenterOfRect(pTopLeft, pTopRight, pBotRight, pBotLeft);
                    float extents = 10 * m_ZoomFactor;
                    var color = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0.2f, 1, 1));
                    
                    Vector2 extentsLineV = new Vector2(0, extents);
                    Vector2 extentsLineH = new Vector2(extents, 0);
                    Vector2 extentsQuadV = new Vector2(0, extents/3.0f);
                    Vector2 extentsQuadH = new Vector2(extents/ 3.0f, 0);
                    pTopLeft  = center - extentsQuadV - extentsQuadH;
                    pTopRight = center - extentsQuadV + extentsQuadH;
                    pBotRight = center + extentsQuadV + extentsQuadH;
                    pBotLeft  = center + extentsQuadV - extentsQuadH;
                    ImGui.GetWindowDrawList().AddQuadFilled(center - extentsQuadV - extentsQuadH, center - extentsQuadV + extentsQuadH, center + extentsQuadV + extentsQuadH, center + extentsQuadV - extentsQuadH, 0xFFFFFFFF);
                    ImGui.GetWindowDrawList().AddQuad(pTopLeft, pTopRight,pBotRight, pBotLeft, color, 1);

                    ImGui.GetWindowDrawList().AddLine(center - extentsLineV, center + extentsLineV, color,1);
                    ImGui.GetWindowDrawList().AddLine(center - extentsLineH, center + extentsLineH, color,1);
                }

                
                //Check if the mouse is inside the quad
                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
                {
                    if (KunaiMath.IsPointInRect(mousePos, pTopLeft, pTopRight, pBotRight, pBotLeft))
                    {
                        //Add selection box
                        ImGui.GetWindowDrawList().AddQuad(pTopLeft, pTopRight, pBotRight, pBotLeft, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0.2f, 1, 1)), 1);

                        if (ImGui.IsMouseClicked(0))
                        {
                            possibleSelections.Add(quad.OriginalData.OriginCast);
                        }
                        
                        //if (ImGui.IsMouseDragging(0))
                        //{
                        //    dragging = true;
                        //    dragStartMousePos = mousePos;
                        //    Vector2 center = KunaiMath.CenterOfRect(pTopLeft, pTopRight, pBotRight, pBotLeft);
                        //    Vector2 quadCenter = (center - screenPos) / viewSize;
                        //    dragStartQuadCenter = quadCenter;
                        //}
                        //if (dragging && ImGui.IsMouseDragging(0))
                        //{
                        //    Vector2 center = KunaiMath.CenterOfRect(pTopLeft, pTopRight, pBotRight, pBotLeft);
                        //    
                        //    Vector2 quadCenter = (center - screenPos) / viewSize;
                        //    Vector2 mouseDelta = (mousePos - dragStartMousePos) / viewSize; // Convert delta to quad space
                        //    Vector2 newCenter = dragStartQuadCenter + mouseDelta;

                        //    quad.OriginalData.OriginCast.Info = quad.OriginalData.OriginCast.Info with
                        //    {
                        //        Translation = newCenter
                        //    };
                        //}
                    }

                }
                if (renderer.Config.ShowQuads)
                    ImGui.GetWindowDrawList().AddQuad(pTopLeft, pTopRight, pBotRight, pBotLeft, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1)));
                
            }

            if (possibleSelections.Count > 0)
            {
                List<Cast> selections = possibleSelections.OrderBy(in_X => in_X.Type).ThenByDescending(in_X => in_X.Priority)
                    .ToList();

                InspectorWindow.SelectCast(KunaiProject.Instance.VisibilityData.GetCast(selections[0]));
            }
        }
    }
}
