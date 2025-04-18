using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using Kunai.ShurikenRenderer;
using Hexa.NET.Utilities.Text;
using SharpNeedle.Framework.Ninja.Csd.Motions;
using System.Collections.Generic;
using System.Numerics;
using IconFonts;
using HekonrayBase.Base;
using HekonrayBase;
using Kunai.Window.Modal;
using System;

namespace Kunai.Window
{
    public class AnimationsWindow : Singleton<AnimationsWindow>, IWindow
    {
        public struct SKeyframePropertyInfo
        {
            public string Icon;
            public string Name;
            public Vector4 Color;

            public SKeyframePropertyInfo(string in_Icon, string in_Name, Vector4 in_Color)
            {
                Icon = in_Icon;
                Name = in_Name;
                Color = in_Color;
            }
        }
        private static List<ImPlotPoint> ms_Points = new List<ImPlotPoint>();
        private KeyframeAddMenu keyframeAddMenu = new KeyframeAddMenu();
        private CastMotionAddMenu castMotionAddMenu = new CastMotionAddMenu();
        private CsdVisData.Animation currentMot;
        bool notCreatingNewCast;
        private void DrawMotionElement(CsdVisData.Animation in_SceneMotion)
        {
            bool selected = false;
            bool node = ImKunai.VisibilityNode(in_SceneMotion.Value.Key, ref in_SceneMotion.Active, ref selected, in_ShowArrow: true, in_RightClickAction: delegate { MotionAddCast(in_SceneMotion); });
            
            if (node)
            {
                for (int i = 0; i < in_SceneMotion.Value.Value.FamilyMotions.Count; i++)
                {
                    FamilyMotion familyMotion = in_SceneMotion.Value.Value.FamilyMotions[i];
                    DrawFamilyMotionElement(familyMotion, i);
                }
                ImGui.TreePop();
            }
        }

        private void MotionAddCast(CsdVisData.Animation in_SceneMotion)
        {
            if (ImGui.MenuItem("Add cast"))
            {
                var scene = KunaiProject.Instance.SelectionData.SelectedScene;
                castMotionAddMenu.motionKey = scene.Animation.IndexOf(in_SceneMotion);
                castMotionAddMenu.SetEnabled(true);
            }
        }

        public static SIconData GetDisplayNameAndIcon(KeyProperty in_Property)
        {
            switch (in_Property)
            {
                case KeyProperty.HideFlag:
                    return NodeIconResource.HideFlag;
                case KeyProperty.PositionX:
                    return NodeIconResource.PositionX;
                case KeyProperty.PositionY:
                    return NodeIconResource.PositionY;
                case KeyProperty.Rotation:
                    return NodeIconResource.Rotation;
                case KeyProperty.ScaleX:
                    return NodeIconResource.ScaleX;
                case KeyProperty.ScaleY:
                    return NodeIconResource.ScaleY;
                case KeyProperty.SpriteIndex:
                    return NodeIconResource.SpriteIndex;
                case KeyProperty.Color:
                    return NodeIconResource.Color;
                case KeyProperty.GradientTopLeft:
                    return NodeIconResource.GradientTopLeft;
                case KeyProperty.GradientBottomLeft:
                    return NodeIconResource.GradientBottomLeft;
                case KeyProperty.GradientTopRight:
                    return NodeIconResource.GradientTopRight;
                case KeyProperty.GradientBottomRight:
                    return NodeIconResource.GradientBottomRight;
            }

            return NodeIconResource.HideFlag;
        }
        private void DrawFamilyMotionElement(FamilyMotion in_FamilyMotion, int in_FamIndex)
        {
            for (int i = 0; i < in_FamilyMotion.CastMotions.Count; i++)
            {
                CastMotion castMotion = in_FamilyMotion.CastMotions[i];
                if (castMotion.Count == 0) continue;
                ImGui.PushID($"##{castMotion.Cast.Name}_{in_FamIndex}_anim_{i}");
                bool treeNodeOpen = ImGui.TreeNode(castMotion.Cast.Name);
                if(ImGui.IsItemHovered())
                {
                    if (ImGui.BeginTooltip())
                    {
                        ImGui.Text($"Fam Idx {in_FamIndex}");
                        ImGui.EndTooltip();
                    }
                }

                if (ImGui.BeginPopupContextItem())
                {
                    ImGui.SeparatorText(castMotion.Cast.Name);
                    if (ImGui.MenuItem("Add track"))
                    {
                        keyframeAddMenu.motion = castMotion;
                        keyframeAddMenu.SetEnabled(true);
                    }
                    if (ImGui.MenuItem("Delete"))
                    {
                        castMotion.Clear();
                        in_FamilyMotion.CastMotions[i] = castMotion;
                    }
                    ImGui.EndPopup();
                }
                if (treeNodeOpen)
                {
                    for (int t = 0; t < castMotion.Count; t++)
                    {
                        ImGui.PushID($"##{castMotion.Cast.Name}anim_{i}_{t}");
                        KeyFrameList track = castMotion[t];
                        var info = GetDisplayNameAndIcon(track.Property);
                        if (ImKunai.AnimationTreeNode(info))
                        {
                            KunaiProject.Instance.SelectionData.TrackAnimation = track;
                        }
                        if (ImGui.BeginPopupContextItem())
                        {
                            ImGui.SeparatorText(info.Name);
                            if (ImGui.MenuItem("Delete"))
                            {
                                castMotion.Remove(track);
                            }
                            ImGui.EndPopup();
                        }
                        ImGui.PopID();

                        
                    }
                    ImGui.TreePop();
                }
                ImGui.PopID();
                
            }
        }


        private void DrawPlot(KunaiProject in_Renderer)
        {
            unsafe
            {
                ImPlotPoint mousePosPlot = new();
                if (ImPlot.BeginPlot("##Bezier", new System.Numerics.Vector2(ImGui.GetWindowSize().X / 1.73f, -1)))
                {
                    const int bufferSize = 256;
                    byte* buffer = stackalloc byte[bufferSize];
                    StrBuilder sb = new(buffer, bufferSize);
                    sb.Append($"##anim");
                    sb.End();
                    var selectedScene = KunaiProject.Instance.SelectionData.SelectedScene;
                    ImPlot.SetupAxisLimits(ImAxis.X1, 0, 60);
                    ImPlot.SetupAxisLimits(ImAxis.Y1, 0, 10);
                    if (selectedScene != null)
                    {
                        if (in_Renderer.SelectionData.TrackAnimation != null)
                        {
                            double time = in_Renderer.Config.Time * selectedScene.Value.Value.FrameRate;
                            ms_Points.Clear();
                            //Line for the anim time
                            if (ImPlot.DragLineX(0, &time, new Vector4(1, 1, 1, 1), 1))
                            {
                                in_Renderer.Config.Time = (float)(time / selectedScene.Value.Value.FrameRate);
                            }

                            bool isFloatValue = in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.Color
                                && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientBottomRight
                                && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientBottomLeft
                                && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientTopLeft
                                && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientTopRight;
                            //Animation keyframe points
                            for (int i = 0; i < in_Renderer.SelectionData.TrackAnimation.Frames.Count; i++)
                            {
                                ImPlotPoint point = new ImPlotPoint(in_Renderer.SelectionData.TrackAnimation.Frames[i].Frame, isFloatValue ? in_Renderer.SelectionData.TrackAnimation.Frames[i].Value.Float : 0);
                                ms_Points.Add(point);
                                bool isClicked = false;
                                if (ImPlot.DragPoint(i, &point.X, &point.Y, in_Renderer.SelectionData.KeyframeSelected == in_Renderer.SelectionData.TrackAnimation.Frames[i] ? new System.Numerics.Vector4(1, 0.9f, 1, 1) : new System.Numerics.Vector4(0, 0.9f, 0, 1), 8, ImPlotDragToolFlags.None, &isClicked))
                                {
                                    if (isFloatValue)
                                        in_Renderer.SelectionData.TrackAnimation.Frames[i].Value = new SharpNeedle.Framework.Ninja.Csd.Motions.KeyFrame.Union((float)point.Y);
                                    in_Renderer.SelectionData.TrackAnimation.Frames[i].Frame = (int)point.X;
                                }
                                if (isClicked)
                                    in_Renderer.SelectionData.KeyframeSelected = in_Renderer.SelectionData.TrackAnimation.Frames[i];
                            }
                            //var p1 = points.ToArray()[0];
                            //ImPlot.PlotLine("##bez", &p1.X, &p1.Y, points.Count, ImPlotLineFlags.Loop, 0, sizeof(ImPlotPoint));

                            //    ImPlotPoint p1 = new ImPlotPoint(.05f, .05f);
                            //ImPlotPoint p2 = new ImPlotPoint(1, 1);
                            //ImPlot.DragPoint(0, &p1.X, &p1.Y, new System.Numerics.Vector4(0, 0.9f, 0, 1), 4, flags, &test, &test, &test);
                            //ImPlot.DragPoint(1, &p2.X, &p2.Y, new System.Numerics.Vector4(1, 0.5f, 1, 1), 4, flags, &test, &test, &test);
                            //
                            //ImPlot.PlotLine("##h1", &p1.X, &p1.Y, 2, 0, 0, sizeof(ImPlotPoint));
                            //ImPlot.PlotLine("##h1", &p2.X, &p2.Y, 2, 0, 0, sizeof(ImPlotPoint));

                        }
                    }

                    mousePosPlot = ImPlot.GetPlotMousePos();
                }
                ImPlot.EndPlot();

                if (ImGui.BeginPopupContextItem())
                {
                    var selectedScene = KunaiProject.Instance.SelectionData.SelectedScene;
                    if (selectedScene != null)
                    {
                        if (in_Renderer.SelectionData.TrackAnimation != null)
                        {
                            if (ImGui.MenuItem("Add Keyframe"))
                            {
                                var frame = new KeyFrame();
                                bool isFloatValue = in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.Color
                                    && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientBottomRight
                                    && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientBottomLeft
                                    && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientTopLeft
                                    && in_Renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientTopRight;
                                frame.Frame = (int)mousePosPlot.X;
                                if (isFloatValue)
                                    frame.Value = (KeyFrame.Union)mousePosPlot.Y;
                                in_Renderer.SelectionData.TrackAnimation.Add(frame);
                            }
                            if (in_Renderer.SelectionData.KeyframeSelected != null)
                            {
                                if (ImGui.MenuItem("Delete Keyframe"))
                                {
                                    in_Renderer.SelectionData.TrackAnimation.Remove(in_Renderer.SelectionData.KeyframeSelected);
                                    in_Renderer.SelectionData.KeyframeSelected = null;
                                }
                            }
                        }
                    }
                    ImGui.EndPopup();
                }
            }
        }
        private void DrawKeyframeInspector()
        {
            if (ImGui.BeginListBox("##animlist2", new System.Numerics.Vector2(-1, -1)))
            {
                var renderer = KunaiProject.Instance;
                ImGui.SeparatorText("Keyframe");
                if (renderer.SelectionData.KeyframeSelected == null)
                    ImGui.TextWrapped("Select a keyframe in the timeline to edit its values.");
                else
                {
                    var keyframe = renderer.SelectionData.KeyframeSelected;
                    int frame = (int)keyframe.Frame;
                    var val = keyframe.Value;
                    var valColor = keyframe.Value.Color.ToVec4().Invert();
                    var interp = (int)keyframe.Interpolation;
                    bool correctionExists = keyframe.Correction != null;
                    ImGui.InputInt("Frame", ref frame);
                    bool isFloatValue = renderer.SelectionData.TrackAnimation.Property != KeyProperty.Color
                       && renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientBottomRight
                       && renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientBottomLeft
                       && renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientTopLeft
                       && renderer.SelectionData.TrackAnimation.Property != KeyProperty.GradientTopRight;

                    if (isFloatValue)
                    {
                        ImGui.InputFloat("Value", ref val.Float);
                        keyframe.Value = val.Float;
                    }
                    else
                    {
                        if (ImGui.ColorEdit4("Value", ref valColor))
                            keyframe.Value = valColor.Invert().ToSharpNeedleColor();
                    }

                    if (ImGui.Combo("Interpolation", ref interp, ["Const", "Linear", "Hermite"], 3))
                        keyframe.Interpolation = (InterpolationType)interp;

                    if (correctionExists)
                    {
                        var correction = keyframe.Correction.Value;
                        var centerCor = correction.Center;
                        var offsetCor = correction.Offset;
                        ImGui.InputFloat2("Asp Center", ref centerCor);
                        ImGui.InputFloat2("Asp Offset", ref offsetCor);
                        correction.Center = centerCor;
                        correction.Offset = offsetCor;
                        keyframe.Correction = correction;
                    }

                    keyframe.Frame = frame;
                }
                ImGui.EndListBox();
            }
        }

        public void OnReset(IProgramProject in_Renderer)
        {
        }

        float m_Test;
        public void Render(IProgramProject in_Renderer)
        {
            var renderer = (KunaiProject)in_Renderer;
            var size1 = ImGui.GetWindowViewport().Size.X / 4.5f;
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(size1, ImGui.GetWindowViewport().Size.Y / 1.5f), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(size1 * 2.5f, ImGui.GetWindowViewport().Size.Y / 3), ImGuiCond.Always);
            if (ImGui.Begin("Animations", MainWindow.WindowFlags | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.SameLine();
                ImKunai.TextFontAwesome(FontAwesome6.Stopwatch);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60);
                if (ImGui.DragFloat("##animTime", ref renderer.Config.Time, 0.01f))
                {
                    if (renderer.Config.Time < 0)
                        renderer.Config.Time = 0.0f;
                }
                ImGui.SameLine();
                var windowSize = ImGui.GetWindowSize();
                var groupWidth = size1; // Adjust as needed


                //ImGui.PushFont(ImGuiController.FontAwesomeFont);
                if (ImGui.Button(FontAwesome6.Camera))
                {
                    renderer.SaveScreenshot();
                }
                ImGui.SameLine();


                // Center group by setting the cursor position
                ImGui.SetCursorPosX((windowSize.X - groupWidth) * 0.7f);
                ImGui.BeginGroup();
                if (ImGui.Button(FontAwesome6.Stop))
                {
                    renderer.Config.PlayingAnimations = false;
                    renderer.Config.Time = 0;
                }
                ImGui.SameLine();
                if (ImGui.Button(renderer.Config.PlayingAnimations ? FontAwesome6.Pause : FontAwesome6.Play))
                    renderer.Config.PlayingAnimations = !renderer.Config.PlayingAnimations;

                ImGui.SameLine();
                if (ImGui.Button(FontAwesome6.RotateRight))
                {
                    renderer.Config.Time = 0;
                }
                //ImGui.PopFont();
                ImGui.EndGroup();
                AnimList();
                ImGui.SameLine();
                DrawPlot(renderer);
                ImGui.SameLine();
                DrawKeyframeInspector();
                keyframeAddMenu.Draw();
                castMotionAddMenu.Draw();
                ImGui.End();
            }
        }

        private void AnimList()
        {
            //The list of anims, anim tracks and cast animations
            if (ImGui.BeginListBox("##animlist", new System.Numerics.Vector2(ImGui.GetWindowSize().X / 5, -1)))
            {
                var selectedScene = KunaiProject.Instance.SelectionData.SelectedScene;
                if (selectedScene != null)
                {
                    foreach (CsdVisData.Animation sceneMotion in selectedScene.Animation)
                    {
                        DrawMotionElement(sceneMotion);
                    }
                }
                ImGui.EndListBox();
            }
        }
    }
}