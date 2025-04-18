using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hexa.NET.OpenGL;

namespace Kunai.ShurikenRenderer
{
    public class ShaderProgram
    {
        public uint Id { get; private set; } = 0;
        public string Name { get; private set; }

        /// <summary>
        /// Compiles the shader program using the specified vertex and fragment
        /// programs.
        /// </summary>
        /// <param name="vertexPath">The path to the vertex program.</param>
        /// <param name="fragmentPath">The path to the fragment program.</param>
        public void Compile(string in_Name, string in_VertexPath, string in_FragmentPath)
        {
            Name = in_Name;
            string vertexSource = "";
            string fragmentSource = "";

            using (StreamReader reader = new StreamReader(in_VertexPath))
            {
                vertexSource = reader.ReadToEnd();
            };

            using (StreamReader reader = new StreamReader(in_FragmentPath))
            {
                fragmentSource = reader.ReadToEnd();
            };

            // Create shaders
            uint vertexShader = GLSingle.Ins.CreateShader(GLShaderType.VertexShader);
            GLSingle.Ins.ShaderSource(vertexShader, vertexSource);
            GLSingle.Ins.CompileShader(vertexShader);

            string vLog = GLSingle.Ins.GetShaderInfoLog(vertexShader);
            if (!string.IsNullOrEmpty(vLog))
                Console.WriteLine(vLog);

            uint fragmentShader = GLSingle.Ins.CreateShader(GLShaderType.FragmentShader);
            GLSingle.Ins.ShaderSource(fragmentShader, fragmentSource);
            GLSingle.Ins.CompileShader(fragmentShader);

            string fLog = GLSingle.Ins.GetShaderInfoLog(fragmentShader);
            if (!string.IsNullOrEmpty(fLog))
                Console.WriteLine(fLog);

            // Link shaders to program
            Id = GLSingle.Ins.CreateProgram();
            GLSingle.Ins.AttachShader(Id, vertexShader);
            GLSingle.Ins.AttachShader(Id, fragmentShader);
            GLSingle.Ins.LinkProgram(Id);

            // Cleanup
            GLSingle.Ins.DetachShader(Id, vertexShader);
            GLSingle.Ins.DetachShader(Id, fragmentShader);
            GLSingle.Ins.DeleteShader(vertexShader);
            GLSingle.Ins.DeleteShader(fragmentShader);
        }

        public void SetUniform(string in_Attribute, int in_Value)
        {
            GLSingle.Ins.Uniform1i(GLSingle.Ins.GetUniformLocation(Id, in_Attribute), in_Value);
        }

        public void SetUniform(string in_Attribute, float in_Value)
        {
            GLSingle.Ins.Uniform1f(GLSingle.Ins.GetUniformLocation(Id, in_Attribute), in_Value);
        }

        public void SetMatrix4(string in_Name, HekonrayBase.Mathematics.Matrix3 in_Mat)
        {
            //GLSingle.Ins.UniformMatrix4fv(GLSingle.Ins.GetUniformLocation(Id, in_Name), true, ref in_Mat);
        }

        public void SetBool(string in_Name, bool in_Value)
        {
            GLSingle.Ins.Uniform1i(GLSingle.Ins.GetUniformLocation(Id, in_Name), in_Value ? 1 : 0);
        }

        public void Use()
        {
            GLSingle.Ins.UseProgram(Id);
        }

        public ShaderProgram(string in_Name, string in_VertexPath, string in_FragmentPath)
        {
            Compile(in_Name, in_VertexPath, in_FragmentPath);
        }

        public ShaderProgram()
        {

        }
    }
}
