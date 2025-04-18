using HekonrayBase;
using HekonrayBase.Base;
using Hexa.NET.ImGui;
using IconFonts;
using Kunai.ShurikenRenderer;
using SharpNeedle.Framework.Ninja.Csd;
using Shuriken.Rendering;
using System;
using System.Numerics;

namespace Kunai.Window
{
    public class HierarchyWindow : Singleton<HierarchyWindow>, IWindow
    {
        private static TempSearchBox ms_SearchBox = new TempSearchBox();
        private static bool CastControl(CsdVisData.Cast in_Vis, Cast in_Cast, bool in_IsLeaf)
        {
            TVisHierarchyResult control = in_Vis.DrawHierarchy();
            if (control.selected)
            {
                InspectorWindow.SelectScene(in_Vis.Parent);
                InspectorWindow.SelectCast(in_Vis);
            }
            return control.open;

        }
        private static void RecursiveCastWidget(CsdVisData.Scene in_Scene, Cast in_Cast)
        {
            var vis = in_Scene.GetVisibility(in_Cast);
            if (vis == null)
                return;

            //Casts
            if (CastControl(vis, in_Cast, in_Cast.Children.Count > 0))
            {
                for (int x = 0; x < in_Cast.Children.Count; x++)
                {
                    RecursiveCastWidget(in_Scene, in_Cast.Children[x]);
                }
                ImGui.TreePop();
            }
        }
        private static void DrawSceneNode(CsdVisData.Node in_VisNode)
        {
            TVisHierarchyResult result = in_VisNode.DrawHierarchy();
            //Scene Node
            if (result.open)
            {
                foreach (var inNode in in_VisNode.Children)
                {
                    DrawSceneNode(inNode);
                }
                for (int i = 0; i < in_VisNode.Scene.Count; i++)
                {
                    CsdVisData.Scene scene = in_VisNode.Scene[i];

                    // If the user is searching, show only the casts with the searched name
                    // if the user isn't searching, show a tree view of the scene
                    // will all the casts and their own children
                    if (ms_SearchBox.IsSearching)
                    {
                        // If there's no cast in the scene with the name, skip the scene
                        foreach (var cast in scene.Casts)
                        {
                            ms_SearchBox.Update(cast.Value.Name);
                            if (ms_SearchBox.MatchResult())
                            {
                                if (CastControl(cast, cast.Value, false))
                                    ImGui.TreePop();
                            }
                        }

                    }
                    else
                    {
                        TVisHierarchyResult sceneControl = scene.DrawHierarchy();
                        //Scene
                        if (sceneControl.open)
                        {
                            for (int x = 0; x < scene.Value.Value.Families.Count; x++)
                            {
                                var family = scene.Value.Value.Families[x];
                                var castFamilyRoot = family.Casts[0];
                                RecursiveCastWidget(scene, castFamilyRoot);
                            }
                            ImGui.TreePop();
                        }
                        if (sceneControl.selected)
                        {
                            InspectorWindow.SelectScene(scene);
                        }
                    }
                    
                }

                ImGui.TreePop();
            }
        }

        public void OnReset(IProgramProject in_Renderer)
        {
        }

        public void Render(IProgramProject in_Renderer)
        {
            var renderer = (KunaiProject)in_Renderer;
            ImGui.SetNextWindowPos(new Vector2(0, MenuBarWindow.MenuBarHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSize(
                new Vector2(ImGui.GetWindowViewport().Size.X / 4.5f,
                    ImGui.GetWindowViewport().Size.Y - MenuBarWindow.MenuBarHeight), ImGuiCond.Always);
            if (ImGui.Begin("Hierarchy", MainWindow.WindowFlags))
            {
                ms_SearchBox.Render();

                ImGui.BeginDisabled(true);
                //ImGui.PushFont(ImGuiController.FontAwesomeFont);
                Vector2 size = new Vector2(24, 24);
                if (ImGui.Button(NodeIconResource.SceneNode.Icon, size))
                {

                }
                ImGui.SameLine();
                if (ImGui.Button(NodeIconResource.Scene.Icon, size))
                {

                }
                ImGui.SameLine();
                if (ImGui.Button(NodeIconResource.CastNull.Icon, size))
                {

                }
                ImGui.SameLine();
                if (ImGui.Button(FontAwesome6.TurnUp, size))
                {

                }
                ImGui.SameLine();
                if (ImGui.Button(FontAwesome6.TurnDown, size))
                {

                }
                //ImGui.PopFont();
                ImGui.EndDisabled();
                ImKunai.ItemRowsBackground(new Vector4(20, 20, 20, 64));
                if(ImKunai.BeginListBoxCustom("##hierarchylist", new Vector2(-1,-1)))
                {
                    if (renderer.WorkProjectCsd != null)
                    {
                        foreach (var f in renderer.VisibilityData.Nodes)
                        {
                            DrawSceneNode(f);
                        }
                    }
                    ImKunai.EndListBoxCustom();
                }
            }
            ImGui.End();
        }
    }
}
