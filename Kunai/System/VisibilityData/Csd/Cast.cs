using HekonrayBase;
using Hexa.NET.ImGui;
using Kunai.ShurikenRenderer;
using Kunai.Window;
using SharpNeedle.Framework.Ninja.Csd;
using SharpNeedle.Framework.Ninja.Csd.Motions;
using SharpNeedle.Structs;
using Shuriken.Rendering;
using System;
using System.Numerics;
using System.Text;
using CsdCast = SharpNeedle.Framework.Ninja.Csd.Cast;

namespace Kunai
{
    public partial class CsdVisData
    {
        public class Cast : TVisibility<CsdCast>
        {
            public int Id;
            public Scene Parent;
            private bool m_IsEditingCrop;

            public Cast(CsdCast in_Cast, Scene in_Parent)
            {
                Id = new Random().Next(0, 1000);
                Value = in_Cast;
                Parent = in_Parent;
            }

            public override void DrawInspector()
            {
                Vector2 screenMultiplier = (SettingsWindow.ScreenCoordinates ? KunaiProject.Instance.ViewportSize : Vector2.One);
                ImGui.SeparatorText("Cast");
                string[] typeStrings = { "Null (No Draw)", "Sprite", "Font" };
                string[] blendingStr = { "NRM", "ADD" };
                string[] filteringStr = { "NONE", "LINEAR" };

                CastPropertyMask unknownFlags = (CastPropertyMask)((BitSet<uint>)Value.Field2C).Value;
                ElementMaterialFlags materialFlags = (ElementMaterialFlags)Value.MaterialFlags.Value;
                CastInfo info = Value.Info;
                ElementInheritanceFlags inheritanceFlags = (ElementInheritanceFlags)Value.InheritanceFlags.Value;
                
                Vector2 aspectRatioCorrection = (Vector2)Value.Position;
                bool mirrorX = materialFlags.HasFlag(ElementMaterialFlags.MirrorX);
                bool mirrorY = materialFlags.HasFlag(ElementMaterialFlags.MirrorY);
                Vector2 topLeftVert = Value.TopLeft * KunaiProject.Instance.ViewportSize;
                Vector2 topRightVert = Value.TopRight * KunaiProject.Instance.ViewportSize;
                Vector2 bottomLeftVert = Value.BottomLeft * KunaiProject.Instance.ViewportSize;
                Vector2 bottomRightVert = Value.BottomRight * KunaiProject.Instance.ViewportSize;
                
                bool inheritPosX = inheritanceFlags.HasFlag(ElementInheritanceFlags.InheritXPosition);
                bool inheritPosY = inheritanceFlags.HasFlag(ElementInheritanceFlags.InheritYPosition);
                bool inheritRot = inheritanceFlags.HasFlag(ElementInheritanceFlags.InheritRotation);
                bool inheritCol = inheritanceFlags.HasFlag(ElementInheritanceFlags.InheritColor);
                bool inheritScaleX = inheritanceFlags.HasFlag(ElementInheritanceFlags.InheritScaleX);
                bool inheritScaleY = inheritanceFlags.HasFlag(ElementInheritanceFlags.InheritScaleY);
                int spriteIndex = (int)info.SpriteIndex;
                //string text = Value.Text;
                //float kerning = Value.FontKerning * 100;
                string fontname = Value.FontName;

                int indexFont = SpriteHelper.FontNames.IndexOf(fontname);

                Value.Name = HKGUI.DrawInput("Name", Value.Name);
                Value.Type = (CsdCast.EType)HKGUI.DrawComboBox("Type", (int)Value.Type, typeStrings);
                ImGui.SeparatorText("Status");
                Value.Enabled = HKGUI.DrawInput("Enabled", Value.Enabled);
                ImGui.SameLine();
                info.HideFlag = (uint)(HKGUI.DrawInput("Hidden", (info.HideFlag == 0 ? false : true)) == false ? 0 : 1);
                //ImGui.Checkbox("Enabled", ref enabled);
                //ImGui.Checkbox("Hidden", ref hideflag);

                if (ImGui.CollapsingHeader("Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var cursorPosAlign = ImGui.GetCursorPosY();
                    if (PivotHelper.DrawAlignmentGridRadio(ref Value))
                    {
                        topLeftVert = Value.TopLeft * KunaiProject.Instance.ViewportSize;
                        topRightVert = Value.TopRight * KunaiProject.Instance.ViewportSize;
                        bottomLeftVert = Value.BottomLeft * KunaiProject.Instance.ViewportSize;
                        bottomRightVert = Value.BottomRight * KunaiProject.Instance.ViewportSize;
                        //translation = Value.Info.Translation;
                    }
                    ImGui.SameLine();

                    ImGui.SetCursorPosY(cursorPosAlign);
                    ImGui.BeginGroup();
                    ImGui.SeparatorText("Invert UV");
                    ImGui.PushID("mirrorH");
                    ImGui.Checkbox("H", ref mirrorX);
                    ImGui.SetItemTooltip("Mirror the cast horizontally.");
                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.Checkbox("V", ref mirrorY);
                    ImGui.SetItemTooltip("Mirror the cast vertically.");
                    ImGui.EndGroup();

                    ImGui.BeginGroup();
                    ImGui.SeparatorText("Rect Size");
                    var widthHeight = HKGUI.DrawInput("Quad Size", new Vector2(Value.Width, Value.Height), HKGUIInputFlags.Drag, "This does not change any value in the tool,\nthis is a leftover from the CellSprite Editor.");
                    Value.Width = (uint)widthHeight.X;
                    Value.Height = (uint)widthHeight.Y;
                    //ImGui.InputFloat2("Quad Size", ref rectSize);
                    //ImGui.SetItemTooltip("This does not change any value in the tool, this is a leftover from the CellSprite Editor.");
                    ImGui.EndGroup();

                    ImGui.SeparatorText("Vertices");
                    ImGui.SetItemTooltip("These 4 values determine the 4 points that generate the quad (3D element) that the cast will render on. Use with caution");
                    ImGui.DragFloat2("Top Left", ref topLeftVert);
                    ImGui.DragFloat2("Top Right", ref topRightVert);
                    ImGui.DragFloat2("Bottom Left", ref bottomLeftVert);
                    ImGui.DragFloat2("Bottom Right", ref bottomRightVert);

                }
                if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    info.Rotation = HKGUI.DrawInput("Rotation", info.Rotation, HKGUIInputFlags.Drag, "Rotation in degrees.");
                    Value.Origin = HKGUI.DrawInput("Origin", Value.Origin * screenMultiplier, HKGUIInputFlags.Drag, "Value used to offset the translation of the cast.\nThis cannot be changed by animations.") / screenMultiplier;
                    info.Translation = HKGUI.DrawInput("Translation", info.Translation * screenMultiplier, HKGUIInputFlags.Drag, "Position of the cast.") / screenMultiplier;
                    info.Scale = HKGUI.DrawInput("Scale", info.Scale, HKGUIInputFlags.Drag);
                }
                if (ImGui.CollapsingHeader("Color", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    info.Color = HKGUI.DrawColorInput("Color", info.Color.ToVec4().Invert()).Invert().ToSharpNeedleColor();
                    info.GradientTopLeft = HKGUI.DrawColorInput("Top Left", info.GradientTopLeft.ToVec4().Invert()).Invert().ToSharpNeedleColor();
                    info.GradientTopRight = HKGUI.DrawColorInput("Top Right", info.GradientTopRight.ToVec4().Invert()).Invert().ToSharpNeedleColor();
                    info.GradientBottomLeft = HKGUI.DrawColorInput("Bottom Left", info.GradientBottomLeft.ToVec4().Invert()).Invert().ToSharpNeedleColor();
                    info.GradientBottomRight = HKGUI.DrawColorInput("Bottom Right", info.GradientBottomRight.ToVec4().Invert()).Invert().ToSharpNeedleColor();
                }
                if (ImGui.CollapsingHeader("Inheritance", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Checkbox("Inherit Horizontal Position", ref inheritPosX);
                    ImGui.Checkbox("Inherit Vertical Position", ref inheritPosY);
                    ImGui.Checkbox("Inherit Rotation", ref inheritRot);
                    ImGui.Checkbox("Inherit Color", ref inheritCol);
                    ImGui.Checkbox("Inherit Width", ref inheritScaleX);
                    ImGui.Checkbox("Inherit Height", ref inheritScaleY);
                }

                if (Value.Type == CsdCast.EType.Font)
                {
                    if (ImGui.CollapsingHeader("Text", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        //make combo box eventually
                        if (ImGui.Combo("Font", ref indexFont, SpriteHelper.FontNames.ToArray(), SpriteHelper.FontNames.Count))
                        {
                            fontname = SpriteHelper.FontNames[indexFont];
                        }
                        ImGui.PushID("textInput");
                        Value.Text = HKGUI.DrawInput("Text", Value.Text);
                        ImGui.PopID();
                        Value.FontKerning = HKGUI.DrawInput("Kerning", Value.FontKerning * 100, HKGUIInputFlags.Drag) / 100;
                    }
                }
                if (ImGui.CollapsingHeader("Property Mask", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    //if(ImGui.TreeNodeEx("Field2C"))
                    //{
                    bool flag1Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyTransform);
                    bool flag2Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyTranslationX);
                    bool flag3Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyTranslationY);
                    bool flag4Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyRotation);
                    bool flag5Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyScaleX);
                    bool flag6Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyScaleY);
                    bool flag7Active = !unknownFlags.HasFlag(CastPropertyMask.Flag7);
                    bool flag8Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyColor);
                    bool flag9Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyColorTl);
                    bool flag10Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyColorBl);
                    bool flag11Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyColorTr);
                    bool flag12Active = !unknownFlags.HasFlag(CastPropertyMask.ApplyColorBr);
                    bool flag13Active = !unknownFlags.HasFlag(CastPropertyMask.Flag13);
                    bool flag14Active = !unknownFlags.HasFlag(CastPropertyMask.Flag14);
                    bool flag15Active = !unknownFlags.HasFlag(CastPropertyMask.Flag15);

                    ImGui.Checkbox("Ignore Transform", ref flag1Active);
                    if (!flag1Active)
                    {
                        ImGui.Indent();
                        ImGui.Checkbox("Ignore Horizontal Translation", ref flag2Active);
                        ImGui.Checkbox("Ignore Vertical Translation", ref flag3Active);
                        ImGui.Unindent();
                    }
                    ImGui.Checkbox("Ignore Rotation", ref flag4Active);
                    ImGui.Checkbox("Ignore Horizontal Scale", ref flag5Active);
                    ImGui.Checkbox("Ignore Vertical Scale", ref flag6Active);
                    ImGui.Checkbox("Flag7", ref flag7Active);
                    ImGui.Checkbox("Ignore Color", ref flag8Active);
                    ImGui.Checkbox("Ignore Color TL", ref flag9Active);
                    ImGui.Checkbox("Ignore Color BL", ref flag10Active);
                    ImGui.Checkbox("Ignore Color TR", ref flag11Active);
                    ImGui.Checkbox("Ignore Color BR", ref flag12Active);
                    ImGui.Checkbox("Flag13", ref flag13Active);
                    ImGui.Checkbox("Flag14", ref flag14Active);
                    ImGui.Checkbox("Flag15", ref flag15Active);

                    if (!flag1Active) unknownFlags |= CastPropertyMask.ApplyTransform; else unknownFlags &= ~CastPropertyMask.ApplyTransform;
                    if (!flag2Active) unknownFlags |= CastPropertyMask.ApplyTranslationX; else unknownFlags &= ~CastPropertyMask.ApplyTranslationX;
                    if (!flag3Active) unknownFlags |= CastPropertyMask.ApplyTranslationY; else unknownFlags &= ~CastPropertyMask.ApplyTranslationY;
                    if (!flag4Active) unknownFlags |= CastPropertyMask.ApplyRotation; else unknownFlags &= ~CastPropertyMask.ApplyRotation;
                    if (!flag5Active) unknownFlags |= CastPropertyMask.ApplyScaleX; else unknownFlags &= ~CastPropertyMask.ApplyScaleX;
                    if (!flag6Active) unknownFlags |= CastPropertyMask.ApplyScaleY; else unknownFlags &= ~CastPropertyMask.ApplyScaleY;
                    if (!flag7Active) unknownFlags |= CastPropertyMask.Flag7; else unknownFlags &= ~CastPropertyMask.Flag7;
                    if (!flag8Active) unknownFlags |= CastPropertyMask.ApplyColor; else unknownFlags &= ~CastPropertyMask.ApplyColor;
                    if (!flag9Active) unknownFlags |= CastPropertyMask.ApplyColorTl; else unknownFlags &= ~CastPropertyMask.ApplyColorTl;
                    if (!flag10Active) unknownFlags |= CastPropertyMask.ApplyColorBl; else unknownFlags &= ~CastPropertyMask.ApplyColorBl;
                    if (!flag11Active) unknownFlags |= CastPropertyMask.ApplyColorTr; else unknownFlags &= ~CastPropertyMask.ApplyColorTr;
                    if (!flag12Active) unknownFlags |= CastPropertyMask.ApplyColorBr; else unknownFlags &= ~CastPropertyMask.ApplyColorBr;
                    if (!flag13Active) unknownFlags |= CastPropertyMask.Flag13; else unknownFlags &= ~CastPropertyMask.Flag13;
                    if (!flag14Active) unknownFlags |= CastPropertyMask.Flag14; else unknownFlags &= ~CastPropertyMask.Flag14;
                    if (!flag15Active) unknownFlags |= CastPropertyMask.Flag15; else unknownFlags &= ~CastPropertyMask.Flag15;

                    Value.Field2C = (uint)unknownFlags;
                    //}
                }
                if(ImGui.CollapsingHeader("Aspect Ratio Correction"))
                {
                    ImGui.TextWrapped("All of these fields are unknown, if you can figure them out, tell me.");
                    Value.Field58 = HKGUI.DrawInput("Field58", Value.Field58);
                    Value.Field5C = HKGUI.DrawInput("Field5C", Value.Field5C);
                    ImGui.InputFloat2("Field68/6C", ref aspectRatioCorrection);
                    Value.Field70 = HKGUI.DrawInput("Field70", Value.Field70);
                }
                if (Value.Type != 0)
                {

                    if (ImGui.CollapsingHeader("Material", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        int blendingType = materialFlags.HasFlag(ElementMaterialFlags.AdditiveBlending) ? 1 : 0;
                        int filterType = materialFlags.HasFlag(ElementMaterialFlags.LinearFiltering) ? 1 : 0;
                        float width = ImGui.GetWindowSize().X / 2 - 80;
                        ImGui.PushItemWidth(width);
                        if (ImGui.Combo("Blending", ref blendingType, blendingStr, 2))
                        {
                            materialFlags = materialFlags.SetFlag(ElementMaterialFlags.AdditiveBlending, blendingType == 1);
                        }
                        ImGui.SameLine();
                        ImGui.PushItemWidth(width);
                        if (ImGui.Combo("Filtering", ref filterType, filteringStr, 2))
                        {
                            materialFlags = materialFlags.SetFlag(ElementMaterialFlags.LinearFiltering, filterType == 1);
                        }
                        ImGui.BeginDisabled(Value.Type != CsdCast.EType.Sprite);
                        ImGui.InputInt("Selected Sprite", ref spriteIndex);
                        spriteIndex = Math.Clamp(spriteIndex, -1, 32); //can go over 32 for scu

                        if (ImKunai.BeginListBoxCustom("##listpatterns", new Vector2(ImGui.GetContentRegionAvail().X, 160)))
                        {
                            //Draw Pattern selector
                            for (int i = 0; i < Value.SpriteIndices.Length; i++)
                            {
                                int patternIdx = Math.Min(Value.SpriteIndices.Length - 1, (int)Value.SpriteIndices[i]);
                                //Avoid stylecolor issue if the index gets changed
                                int sprIndexCopy = spriteIndex;
                                //Draw button with a different color if its the currently active pattern
                                if (i == sprIndexCopy) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.7f, 0.7f, 0.7f));
                                // Draw sprite preview if it isnt set to the default square
                                if (patternIdx == -1)
                                {
                                    ImKunai.EmptyTextureButton(i);
                                }
                                else
                                {
                                    KunaiSprite spriteReference = SpriteHelper.TryGetSprite(Value.SpriteIndices[i]);
                                    if (spriteReference != null)
                                    {

                                        var uvCoords = spriteReference.GetImGuiUv();

                                        bool isPressed;
                                        if (spriteReference.Texture.GlTex == null)
                                        {
                                            isPressed = ImKunai.EmptyTextureButton(i);
                                        }
                                        else
                                        {
                                            isPressed = ImKunai.SpriteImageButton($"##pattern{i}", spriteReference);
                                        }
                                        if (isPressed)
                                            spriteIndex = i;

                                    }
                                    if (i != Value.SpriteIndices.Length - 1)
                                        ImGui.SameLine();
                                }
                                if (i == sprIndexCopy)
                                    ImGui.PopStyleColor();
                            }
                        }
                        ImKunai.EndListBoxCustom();
                        if (!m_IsEditingCrop)
                        {
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.Button("Edit current pattern", new Vector2(-1, 32)))
                            {
                                m_IsEditingCrop = true;
                            }
                        }
                        else
                        {
                            if (ImGui.BeginListBox("##listpatternsselection", new Vector2(-1, 250)))
                            {
                                var result = ImKunai.TextureSelector(KunaiProject.Instance, false);
                                if (result.IsCropSelected())
                                {
                                    //Avoid a crash if a user decides to not change this
                                    if (spriteIndex == -1)
                                        spriteIndex = 0;
                                    Value.SpriteIndices[spriteIndex] = result.GetSpriteIndex();
                                }

                                ImGui.EndListBox();
                            }
                            if (ImGui.Button("Stop editing pattern", new Vector2(-1, 32)))
                            {
                                m_IsEditingCrop = false;
                            }

                        }
                        ImGui.EndDisabled();
                    }
                }
                //Value.Field58 = (uint)field58;
                //Value.Field5C = (uint)field5c;
                //Value.Field70 = (uint)field70;
                ////Value.Name = name;
                //Value.Field00 = (uint)field00;
                //Value.Type = (CsdCast.EType)type;
                //Value.Enabled = enabled;
                //info.HideFlag = (uint)(hideflag ? 1 : 0);
                //ADD EDIT FOR HIDE FLAG
                if (mirrorX) materialFlags |= ElementMaterialFlags.MirrorX; else materialFlags &= ~ElementMaterialFlags.MirrorX;
                if (mirrorY) materialFlags |= ElementMaterialFlags.MirrorY; else materialFlags &= ~ElementMaterialFlags.MirrorY;
                
                Value.TopLeft = topLeftVert / KunaiProject.Instance.ViewportSize;
                Value.TopRight = topRightVert / KunaiProject.Instance.ViewportSize;
                Value.BottomLeft = bottomLeftVert / KunaiProject.Instance.ViewportSize;
                Value.BottomRight = bottomRightVert / KunaiProject.Instance.ViewportSize;
                Value.Position = aspectRatioCorrection;
                //Value.Origin = origin / screenMultiplier;
                //info.Rotation = rotation;
                //info.Translation = translation / screenMultiplier;
                //info.Color = color.Invert().ToSharpNeedleColor();
                //info.GradientTopLeft = colorTl.Invert().ToSharpNeedleColor();
                //info.GradientTopRight = colorTr.Invert().ToSharpNeedleColor();
                //info.GradientBottomLeft = colorBl.Invert().ToSharpNeedleColor();
                //info.GradientBottomRight = colorBr.Invert().ToSharpNeedleColor();
                info.SpriteIndex = spriteIndex;
                //info.Scale = scale;

                if (inheritPosX) inheritanceFlags |= ElementInheritanceFlags.InheritXPosition; else inheritanceFlags &= ~ElementInheritanceFlags.InheritXPosition;
                if (inheritPosY) inheritanceFlags |= ElementInheritanceFlags.InheritYPosition; else inheritanceFlags &= ~ElementInheritanceFlags.InheritYPosition;
                if (inheritRot) inheritanceFlags |= ElementInheritanceFlags.InheritRotation; else inheritanceFlags &= ~ElementInheritanceFlags.InheritRotation;
                if (inheritCol) inheritanceFlags |= ElementInheritanceFlags.InheritColor; else inheritanceFlags &= ~ElementInheritanceFlags.InheritColor;
                if (inheritScaleX) inheritanceFlags |= ElementInheritanceFlags.InheritScaleX; else inheritanceFlags &= ~ElementInheritanceFlags.InheritScaleX;
                if (inheritScaleY) inheritanceFlags |= ElementInheritanceFlags.InheritScaleY; else inheritanceFlags &= ~ElementInheritanceFlags.InheritScaleY;
                Value.InheritanceFlags = (uint)inheritanceFlags;
                Value.Info = info;
                Value.MaterialFlags = (uint)materialFlags;
                Value.FontName = fontname;
                //Value.Text = text;
                //Value.FontKerning = kerning / 100;
            }
            public override TVisHierarchyResult DrawHierarchy()
            {
                SIconData icon = new();
                switch (Value.Type)
                {
                    case CsdCast.EType.Sprite:
                        {
                            icon = NodeIconResource.CastSprite;
                            break;
                        }
                    case CsdCast.EType.Null:
                        {
                            icon = NodeIconResource.CastNull;
                            break;
                        }
                    case CsdCast.EType.Font:
                        {
                            icon = NodeIconResource.CastFont;
                            break;
                        }
                }
                bool selectedCast = false;
                bool returnVal = ImKunai.VisibilityNode(Value.Name, ref Active, ref selectedCast, CastRightClickAction, in_ShowArrow: Value.Children.Count > 0, in_Icon: icon, in_Id: $"##{Value.Name}_{Id}");
                return new TVisHierarchyResult(returnVal, selectedCast);
            }

            private void CastRightClickAction()
            {
                if (ImGui.BeginMenu("New Cast..."))
                {
                    if (ImGui.MenuItem("Null Cast"))
                    {
                        CreationHelper.CreateNewCast(this, CsdCast.EType.Null);
                    }

                    if (ImGui.MenuItem("Sprite Cast"))
                    {
                        CreationHelper.CreateNewCast(this, CsdCast.EType.Sprite);
                    }

                    if (ImGui.MenuItem("Font Cast"))
                    {
                        CreationHelper.CreateNewCast(this, CsdCast.EType.Font);
                    }

                    ImGui.EndMenu();
                }
                if(ImGui.MenuItem("Duplicate"))
                {
                    CreationHelper.DuplicateCast(this);
                }

                if (ImGui.Selectable("Delete")) Parent.Remove(this);
            }
        }
    }
}
