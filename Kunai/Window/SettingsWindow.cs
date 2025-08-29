using HekonrayBase;
using HekonrayBase.Base;
using HekonrayBase.Settings;
using Hexa.NET.ImGui;
using IconFonts;
using Kunai.ShurikenRenderer;
using Shuriken.Rendering;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using TeamSpettro.SettingsSystem;

namespace Kunai.Window
{
    public class SettingsWindow : Singleton<CropEditor>, IWindow
    {
        public static bool Enabled = false;
        public static bool ShowNullCasts = true;
        public static bool ScreenCoordinates = true;
        bool m_ThemeIsDark = SettingsManager.GetBool("IsDarkThemeEnabled");
        public static string AddQuotesIfRequired(string in_Path)
        {
            return !string.IsNullOrWhiteSpace(in_Path) ?
                in_Path.Contains(" ") && (!in_Path.StartsWith("\"") && !in_Path.EndsWith("\"")) ?
                    "\"" + in_Path + "\"" : in_Path :
                    string.Empty;
        }
        public static void ExecuteAsAdmin(string in_FileName)
        {
            //If the user cancels the UAC prompt, it'll throw an exception
            //so just do nothing in case that happens
            try
            {

                in_FileName = AddQuotesIfRequired(in_FileName);
                Process proc = new Process();
                proc.StartInfo.FileName = in_FileName;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";
                proc.Start();
            }
            catch (Exception)
            {
                // ignored
            }
        }
        public void OnReset(IProgramProject in_Renderer)
        {
            ShowNullCasts = SettingsManager.GetBool("ShowNullCasts", true);
            ScreenCoordinates = SettingsManager.GetBool("UseScreenCoordinates", true);
        }

        public void Render(IProgramProject in_Renderer)
        {
            var renderer = (KunaiProject)in_Renderer;
            if (Enabled)
            {
                ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("Settings", ref Enabled))
                {
                    int currentTheme = m_ThemeIsDark ? 1 : 0;
                    var color = renderer.ViewportColor;
                    ImGui.SeparatorText("Appearance");
                    if (ImGui.Combo("Theme", ref currentTheme, ["Light", "Dark"], 2))
                    {
                        m_ThemeIsDark = currentTheme == 1;
                        SettingsManager.SetBool("IsDarkThemeEnabled", m_ThemeIsDark);
                        ImGuiThemeManager.SetTheme(m_ThemeIsDark);
                    }
                    ImGui.SetItemTooltip("Sets the theme of the interface.");
                    if(ImGui.Checkbox("Show Coordinates as Pixels", ref ScreenCoordinates))
                    {
                        SettingsManager.SetBool("UseScreenCoordinates", ScreenCoordinates);
                    }
                    ImGui.SetItemTooltip("If this is enabled, values for translations and offsets\nwill be converted to pixel coordinates.\nIf this is disabled, the raw values from the file\nwill be shown instead.\n(This setting doesn't affect animations.)");
                    ImGui.SeparatorText("Viewport");
                    if (ImGui.Checkbox("Show Null Casts", ref ShowNullCasts))
                    {
                        SettingsManager.SetBool("ShowNullCasts", ShowNullCasts);
                    }
                    ImGui.SetItemTooltip("If this is enabled, Null casts will have an indicator\nin the viewport similar to the one in CellSpriteEditor.");
                    if (ImGui.ColorEdit3("Viewport Color", ref color))
                    {
                        renderer.SetViewportColor(color);
                    }
                    ImGui.SeparatorText("Utilities");
                    if(ImGui.Button("Associate file extensions", new Vector2(-1, 32)))
                        ExecuteAsAdmin(@Path.Combine(@Program.Path, "FileTypeRegisterService.exe"));
                    
                    ImGui.SetItemTooltip("Associate xncp, yncp, gncp and sncp files with Kunai.\n(Make sure to place the program in a folder where it won't be moved)");
                    ImGui.End();
                }
            }
        }
    }
}
