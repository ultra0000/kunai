using Hexa.NET.ImGui;
using Kunai.ShurikenRenderer;
using SharpNeedle.Framework.Ninja.Csd;
using SharpNeedle.Framework.Ninja.Csd.Motions;
using System;
using System.Numerics;

namespace Kunai.Window.Modal
{
    public class CastMotionAddMenu : ModalWindow
    {
        Vector2 m_ModalSize = new Vector2(500, 800);
        public int textureIndex;
        public int motionKey;
        public CsdVisData.Animation motion
        {
            get
            {
                return KunaiProject.Instance.SelectionData.SelectedScene.Animation[motionKey];
            }
            set
            {
                KunaiProject.Instance.SelectionData.SelectedScene.Animation[motionKey] = value;
            }
        }
        private CsdVisData.Scene scene;
        int m_SelectedProp;
        int m_SelectedCast;
        public override void Setup()
        {
            name = "##castmotadd";
            size = m_ModalSize;
        }
        public bool IsAlreadyAdded(Cast in_Cast)
        {
            foreach (var fam in scene.Value.Value.Families)
            {
                foreach(var famCasts in fam.Casts)
                {
                    if (famCasts == in_Cast)
                    {
                        foreach (var animfam in motion.Value.Value.FamilyMotions)
                        {
                            foreach(var animCast in animfam.CastMotions)
                            {
                                if (animCast.Cast == in_Cast)
                                {
                                    if(animCast.Count != 0)
                                        return true;
                                }
                            }
                        }
                    }
                }                
            }
            return false;
        }
        public int[] GetCastMotionFromCast(Cast in_Cast)
        {
            for (int i = 0; i < motion.Value.Value.FamilyMotions.Count; i++)
            {
                FamilyMotion family = motion.Value.Value.FamilyMotions[i];
                for (int a = 0; a < family.CastMotions.Count; a++)
                {
                    CastMotion cast = family.CastMotions[a];
                    if (cast.Cast == in_Cast)
                        return [i, a];
                }
            }
            return [-1,-1];
        }
        public override void DrawContents()
        {
            var size = ImGui.GetContentRegionAvail();
            var size2 = (m_ModalSize.Y - 200) / 2;
            if (ImGui.BeginListBox("##castlist", new Vector2(-1, size2)))
            {
                scene = KunaiProject.Instance.SelectionData.SelectedScene;
                for (int i = 0; i < scene.Casts.Count; i++)
                {
                    CsdVisData.Cast cast = scene.Casts[i];
                    ImGui.BeginDisabled(IsAlreadyAdded(cast.Value));
                    var result = cast.DrawHierarchy();

                    if (result.open)
                        ImGui.TreePop();
                    if (result.selected)
                        m_SelectedCast = i;
                    ImGui.EndDisabled();
                }
                ImGui.EndListBox();
            }
            if (ImGui.BeginListBox("##tracklist", new Vector2(-1, size2)))
            {
                for (int i = 0; i < 12; i++)
                {
                    var info = AnimationsWindow.GetDisplayNameAndIcon((KeyProperty)i);

                    if (ImKunai.AnimationTreeNode(info))
                    {
                        m_SelectedProp = i;
                    }
                }
                ImGui.EndListBox();
            }
            ImGui.Separator();
            if (ImGui.Button("Execute"))
            {
                var cast = scene.Casts[m_SelectedCast];

                var data = GetCastMotionFromCast(cast.Value);

                var list = new KeyFrameList();
                list.Frames.Add(new KeyFrame());
                list.Property = (KeyProperty)m_SelectedProp;
                motion.Value.Value.FamilyMotions[data[0]].CastMotions[data[1]].Add(list);

                SetEnabled(false);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                SetEnabled(false);
                ImGui.CloseCurrentPopup();
            }
        }
    }
}