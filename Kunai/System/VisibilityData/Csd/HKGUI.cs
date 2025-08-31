using Hexa.NET.ImGui;
using System.Numerics;

namespace HekonrayBase
{
    public enum HKGUIInputFlags : int
    {
        None = 0,
        Drag = 1
    }
    /// <summary>
    /// Inspired by UnityEditor's GUILayout
    /// </summary>
    public static class HKGUI
    {
        private static bool ms_HasChanged;

        public static bool HasValueChanged()
        {
            return ms_HasChanged;
        }
        public static int DrawComboBox(string in_Label, int in_Current, string[] in_Options)
        {
            ms_HasChanged = ImGui.Combo(in_Label, ref in_Current, in_Options, in_Options.Length);
            return in_Current;
        }

        public static Vector4 DrawColorInput(string in_Label, Vector4 in_Vector)
        {
            ms_HasChanged = ImGui.ColorEdit4(in_Label, ref in_Vector);
            return in_Vector;
        }
        /// <summary>
        /// Draws the correct input box for the type that is passed.
        /// (Add an additional ref bool to detect changes)
        /// Supports: bool, string, Vector3, Vector4
        /// </summary>
        /// <typeparam name="T">Type of argument</typeparam>
        /// <param name="in_Label">Label text</param>
        /// <param name="in_Value">Value to pass to input box</param>
        /// <returns>The modified value</returns>
        public static T DrawInput<T>(string in_Label, T in_Value, HKGUIInputFlags in_Flags = 0, string in_Tooltip = "")
        {
            T returnVal = in_Value;
            bool out_IsClicked = false;
            switch (in_Value)
            {
                case string str:
                    {
                        out_IsClicked = ImGui.InputText(in_Label, ref str, 100, ImGuiInputTextFlags.AutoSelectAll);
                        returnVal = (T)(object)str;
                        break;
                    }
                case Vector2 vec2:
                    {
                        if (in_Flags == 0)
                            out_IsClicked = ImGui.InputFloat2(in_Label, ref vec2);
                        else if (in_Flags == HKGUIInputFlags.Drag)
                            out_IsClicked = ImGui.DragFloat2(in_Label, ref vec2);
                        returnVal = (T)(object)vec2;
                        break;
                    }
                case Vector3 vec3:
                    {
                        if (in_Flags == 0)
                            out_IsClicked = ImGui.InputFloat3(in_Label, ref vec3);
                        else if (in_Flags == HKGUIInputFlags.Drag)
                            out_IsClicked = ImGui.DragFloat3(in_Label, ref vec3);
                        returnVal = (T)(object)vec3;
                        break;
                    }
                case Vector4 vec4:
                    {
                        if (in_Flags == 0)
                            out_IsClicked = ImGui.InputFloat4(in_Label, ref vec4);
                        else if (in_Flags == HKGUIInputFlags.Drag)
                            out_IsClicked = ImGui.DragFloat4(in_Label, ref vec4);
                        returnVal = (T)(object)vec4;
                        break;
                    }
                case bool toggle:
                    {
                        out_IsClicked = ImGui.Checkbox(in_Label, ref toggle);
                        returnVal = (T)(object)toggle;
                        break;
                    }
                case int integer:
                    {
                        if (in_Flags == 0)
                            out_IsClicked = ImGui.InputInt(in_Label, ref integer);
                        else if (in_Flags == HKGUIInputFlags.Drag)
                            out_IsClicked = ImGui.DragInt(in_Label, ref integer);
                        returnVal = (T)(object)integer;
                        break;
                    }
                case float floater:
                    {
                        if(in_Flags == 0)
                            out_IsClicked = ImGui.InputFloat(in_Label, ref floater);
                        else if (in_Flags == HKGUIInputFlags.Drag)
                            out_IsClicked = ImGui.DragFloat(in_Label, ref floater);
                        returnVal = (T)(object)floater;
                        break;
                    }
            }
            if(!string.IsNullOrEmpty(in_Tooltip))
                ImGui.SetItemTooltip(in_Tooltip);

            ms_HasChanged = out_IsClicked;
            return returnVal;
        }
    }
}