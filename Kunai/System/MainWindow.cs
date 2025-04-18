using Hexa.NET.ImGui;
using Kunai.ShurikenRenderer;
using Kunai.Window;
using System.IO;
using System;
using System.Runtime.InteropServices;
using TeamSpettro.SettingsSystem;
using HekonrayBase;
using HekonrayBase.Settings;
using HekonrayBase.Base;
using System.Numerics;
using Hexa.NET.GLFW;


namespace Kunai
{
    public class MainWindow : HekonrayWindow
    {
        private IntPtr m_IniName;
        private KunaiProject KunaiProject => (KunaiProject)Project;
        public static ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

        public MainWindow(Version in_OpenGlVersion, Vector2Int in_WindowSize) : base(in_OpenGlVersion, in_WindowSize)
        {
            ApplicationName = "Kunai";
            Title = ApplicationName;
        }

        public override void OnLoad()
        {
            Project = KunaiProject.Instance;
            KunaiProject.SetWindowParameters(this, new System.Numerics.Vector2(ClientSize.X, ClientSize.Y));
            OnActionWithArgs = LoadFromArgs;
            TeamSpettro.Resources.Initialize(Path.Combine(Program.Path, "config.json"));
            
            base.OnLoad();
        
            ImGuiThemeManager.SetTheme(SettingsManager.GetBool("IsDarkThemeEnabled", false));
            // Example #10000 for why ImGui.NET is kinda bad
            // This is to avoid having imgui.ini files in every folder that the program accesses
            unsafe
            {
                m_IniName = Marshal.StringToHGlobalAnsi(Path.Combine(Program.Path, "imgui.ini"));
                ImGuiIOPtr io = ImGui.GetIO();
                io.IniFilename = (byte*)m_IniName;
            }
            Windows.Add(ModalHandler.Instance);
            Windows.Add(new Window.MenuBarWindow());
            Windows.Add(new AnimationsWindow());
            Windows.Add(new HierarchyWindow());
            Windows.Add(new InspectorWindow());
            Windows.Add(new ViewportWindow());
            Windows.Add(new CropEditor());
            Windows.Add(new SettingsWindow());
            SettingsWindow.Instance.OnReset(null);
        }

        private void LoadFromArgs(string[] in_Args)
        {
            KunaiProject.LoadFile(in_Args[0]);
        }

        //protected override void OnResize(ResizeEventArgs in_E)
        //{
        //    base.OnResize(in_E);
        //    if(KunaiProject != null)
        //        KunaiProject.ScreenSize = new System.Numerics.Vector2(ClientSize.X, ClientSize.Y);
        //}
        //
        public override void OnRenderImGuiFrame()
        {
            if(ShouldRender())
            {
                base.OnRenderImGuiFrame();
        
                float deltaTime = (float)(GetDeltaTime());
                KunaiProject.Render(KunaiProject.WorkProjectCsd, (float)deltaTime);
        
                if (KunaiProject.IsFileLoaded)
                    Title = ApplicationName + $" - [{KunaiProject.Config.WorkFilePath}]";
                else
                    Title = ApplicationName;
            }
        }
    }
}
