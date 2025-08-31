using Kunai.ShurikenRenderer;
using Hexa.NET.ImGui;
using SharpNeedle.Framework.Ninja.Csd;
using Shuriken.Rendering;
using KunaiSprite = Shuriken.Rendering.KunaiSprite;
using Hexa.NET.Utilities.Text;
using Vector2 = System.Numerics.Vector2;
using System.Collections.Generic;
using System.Numerics;
using System;
using HekonrayBase.Base;
using HekonrayBase;
using SharpNeedle.Structs;

namespace Kunai.Window
{
    enum EAlignmentPivot
    {
        None,
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
    [Flags]
    enum CastPropertyMask : uint
    {
        None = 0x0000,
        ApplyTransform = 0x0001,
        ApplyTranslationX = 0x0002,
        ApplyTranslationY = 0x0004,
        ApplyRotation = 0x0008,
        ApplyScaleX = 0x0010,
        ApplyScaleY = 0x0020,
        Flag7 = 0x0040,
        ApplyColor = 0x0080,
        ApplyColorTl = 0x0100,
        ApplyColorBl = 0x0200,
        ApplyColorTr = 0x0400,
        ApplyColorBr = 0x0800,
        Flag13 = 0x1000,
        Flag14 = 0x2000,
        Flag15 = 0x4000
    }
    public class InspectorWindow : Singleton<InspectorWindow>, IWindow
    {
        public enum ESelectionType
        {
            None,
            Scene,
            Node,
            Cast
        }
        public static ESelectionType SelectionType;
        static bool ms_IsEditingCrop;

        public static void SelectCast(CsdVisData.Cast in_Cast)
        {
            ms_IsEditingCrop = false;
            KunaiProject.Instance.SelectionData.SelectedCast = in_Cast;
            SelectionType = ESelectionType.Cast;
        }
        public static void SelectScene(CsdVisData.Scene in_Scene)
        {
            KunaiProject.Instance.SelectionData.SelectedScene = in_Scene;
            SelectionType = ESelectionType.Scene;
        }


        public void DrawSceneInspector()
        {
            CsdVisData.Scene selectedScene = KunaiProject.Instance.SelectionData.SelectedScene;

            if (selectedScene == null)
                return;

            selectedScene.DrawInspector();            
        }

        public void DrawCastInspector()
        {
            /// Before you ask
            /// ImGui bindings for C# are mega ass.
            /// Keep that in mind.
            var selectedCast = KunaiProject.Instance.SelectionData.SelectedCast;
            if (selectedCast == null)
                return;
            selectedCast.DrawInspector();
        }
        

        internal static void Reset()
        {
            KunaiProject.Instance.SelectionData.SelectedCast = null;
            KunaiProject.Instance.SelectionData.SelectedScene = new();
        }

        public void OnReset(IProgramProject in_Renderer)
        {
        }

        public void Render(IProgramProject in_Renderer)
        {
            var renderer = (KunaiProject)in_Renderer;
            ImGui.SetNextWindowPos(new System.Numerics.Vector2((ImGui.GetWindowViewport().Size.X / 4.5f) * 3.5f, MenuBarWindow.MenuBarHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(ImGui.GetWindowViewport().Size.X / 4.5f, ImGui.GetWindowViewport().Size.Y / 1.5f - MenuBarWindow.MenuBarHeight), ImGuiCond.Always);
            if (ImGui.Begin("Inspector", MainWindow.WindowFlags))
            {
                if (renderer.WorkProjectCsd != null)
                {
                    SpriteHelper.FontNames.Clear();
                    foreach (KeyValuePair<string, Font> font in renderer.WorkProjectCsd.Project.Fonts)
                    {
                        SpriteHelper.FontNames.Add(font.Key);
                    }
                    switch (SelectionType)
                    {
                        case ESelectionType.Scene:
                            {
                                DrawSceneInspector();
                                break;
                            }
                        case ESelectionType.Cast:
                            {
                                DrawCastInspector();
                                break;
                            }
                    }
                }
                ImGui.End();
            }
        }
    }
}