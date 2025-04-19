using System.IO;
using Vector2 = System.Numerics.Vector2;
using Kunai.ShurikenRenderer;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Numerics;
using Kunai;
using Hexa.NET.OpenGL;

namespace Shuriken.Rendering
{
    public class Renderer
    {
        public readonly string ShadersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Shaders");

        private uint m_Vao;
        private uint m_Vbo;
        private uint m_Ebo;
        private uint[] m_Indices;
        public Dictionary<string, ShaderProgram> ShaderDictionary;

        private Vertex[] m_Buffer;
        private List<Quad> m_Quads;
        public List<Quad> Quads
        {
            get { return m_Quads; }
        }

        private bool m_Additive;
        private bool m_LinearFiltering = true;
        private int m_TextureId = -1;
        private ShaderProgram m_Shader;

        public readonly int MaxVertices = 10000;
        public int MaxQuads => MaxVertices / 4;
        public int MaxIndices => MaxQuads * 6;

        public int NumVertices { get; private set; }
        public int NumQuads => m_Quads.Count;
        public int NumIndices { get; private set; }
        public int BufferPos { get; private set; }
        public bool BatchStarted { get; private set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool Additive
        {
            get => m_Additive;
            set
            {
                m_Additive = value;
                GLSingle.Ins.BlendFunc(GLBlendingFactor.SrcAlpha, m_Additive ? GLBlendingFactor.One : GLBlendingFactor.OneMinusSrcAlpha);
            }
        }

        public bool LinearFiltering
        {
            get => m_LinearFiltering;
            set
            {
                m_LinearFiltering = value;

                GLSingle.Ins.TexParameterf(GLTextureTarget.Texture2D, GLTextureParameterName.MinFilter,
                    m_LinearFiltering ? (int)GLTextureMinFilter.Linear : (int)GLTextureMinFilter.Nearest);

                GLSingle.Ins.TexParameterf(GLTextureTarget.Texture2D, GLTextureParameterName.MagFilter,
                    m_LinearFiltering ? (int)GLTextureMagFilter.Linear : (int)GLTextureMagFilter.Nearest);
            }
        }


        public int TextureId
        {
            get => m_TextureId;
            set
            {
                m_TextureId = value;
                m_Shader.SetBool("hasTexture", m_TextureId != -1);
            }
        }

        public Renderer(int in_Width, int in_Height)
        {
            ShaderDictionary = new Dictionary<string, ShaderProgram>();

            ShaderProgram basicShader = new ShaderProgram("basic", Path.Combine(ShadersDir, "basic.vert"), Path.Combine(ShadersDir, "basic.frag"));
            ShaderDictionary.Add(basicShader.Name, basicShader);

            // setup vertex indices
            m_Indices = new uint[MaxIndices];
            uint offset = 0;
            for (uint index = 0; index < MaxIndices; index += 6)
            {
                m_Indices[index + 0] = offset + 0;
                m_Indices[index + 1] = offset + 1;
                m_Indices[index + 2] = offset + 2;

                m_Indices[index + 3] = offset + 1;
                m_Indices[index + 4] = offset + 2;
                m_Indices[index + 5] = offset + 3;

                offset += 4;
            }

            m_Buffer = new Vertex[MaxVertices];
            m_Quads = new List<Quad>(MaxQuads);
            Init();

            Width = in_Width;
            Height = in_Height;
        }
        public List<Quad> GetQuads()
        {
            return m_Quads;
        }

        private unsafe void Init()
        {
            // 2 floats for pos, 2 floats for UVs, 4 floats for color
            int stride = Unsafe.SizeOf<Vertex>();

            GLSingle.Ins.GenVertexArrays(1, ref m_Vao);
            GLSingle.Ins.BindVertexArray(m_Vao);

            GLSingle.Ins.GenBuffers(1, ref m_Vbo);
            GLSingle.Ins.GenBuffers(1, ref m_Ebo);

            GLSingle.Ins.BindBuffer(GLBufferTargetARB.ArrayBuffer, m_Vbo);
            GLSingle.Ins.BufferData(GLBufferTargetARB.ArrayBuffer, MaxVertices, null, GLBufferUsageARB.DynamicDraw);

            GLSingle.Ins.BindBuffer(GLBufferTargetARB.ElementArrayBuffer, m_Ebo);
            fixed(void* data1 = m_Indices)
                GLSingle.Ins.BufferData(GLBufferTargetARB.ElementArrayBuffer, MaxIndices, data1, GLBufferUsageARB.StaticDraw);

            // position
            GLSingle.Ins.EnableVertexAttribArray(0);
            GLSingle.Ins.VertexAttribPointer(0, 2, GLVertexAttribPointerType.Float, false, stride, 0);

            // uv
            GLSingle.Ins.EnableVertexAttribArray(1);
            GLSingle.Ins.VertexAttribPointer(1, 2, GLVertexAttribPointerType.Float, false, stride, 2 * sizeof(float));

            // color
            GLSingle.Ins.EnableVertexAttribArray(2);
            GLSingle.Ins.VertexAttribPointer(2, 4, GLVertexAttribPointerType.Float, false, stride, 4 * sizeof(float));

            GLSingle.Ins.BindBuffer(GLBufferTargetARB.ArrayBuffer, 0);
            GLSingle.Ins.BindVertexArray(0);
        }

        /// <summary>
        /// Resets the number of quads, vertices, and indices.
        /// </summary>
        private void ResetRenderStats()
        {
            NumIndices = 0;
            NumVertices = 0;
        }

        /// <summary>
        /// Starts a new rendering batch.
        /// </summary>
        public void BeginBatch()
        {
            BufferPos = 0;
            BatchStarted = true;

            ResetRenderStats();
        }

        /// <summary>
        /// Ends the current rendering batch and flushes the vertex buffer
        /// </summary>
        public void EndBatch()
        {
            if (BufferPos > 0)
            {
                GLSingle.Ins.BindVertexArray(m_Vao);
                GLSingle.Ins.BindBuffer(GLBufferTargetARB.ArrayBuffer, m_Vbo);
                GLSingle.Ins.BindBuffer(GLBufferTargetARB.ElementArrayBuffer, m_Ebo);
                unsafe
                {
                    fixed(void* buffer = m_Buffer)
                        GLSingle.Ins.BufferSubData(GLBufferTargetARB.ArrayBuffer, nint.Zero, BufferPos * Unsafe.SizeOf<Vertex>(), buffer);
                }
                Flush();
            }

            BatchStarted = false;
        }

        private void Flush()
        {
            GLSingle.Ins.DrawElements(GLPrimitiveType.Triangles, NumIndices, GLDrawElementsType.UnsignedInt, 0);
        }

        /// <summary>
        /// Pushes the quad parameters onto the vertex buffer.
        /// </summary>
        /// <param name="q">The quad to push to the buffer.</param>
        public void PushQuad(Quad in_Quad)
        {
            /// SharpNeedle uses ARGB color, this inverts it so that colors look right
            m_Buffer[BufferPos++] = in_Quad.TopLeft.WithInvertedColor();
            m_Buffer[BufferPos++] = in_Quad.BottomLeft.WithInvertedColor();
            m_Buffer[BufferPos++] = in_Quad.TopRight.WithInvertedColor();
            m_Buffer[BufferPos++] = in_Quad.BottomRight.WithInvertedColor();
            NumIndices += 6;
        }
        public void DrawEmptyQuad(SSpriteDrawData in_DrawData)
        {
            /// TODO: wtf is NextSprite or SpriteFactor?
            var quad = new Quad();
            var aspect = new Vector2(in_DrawData.AspectRatio, 1.0f);

            float scale = 0.0001f;
            var topLeft = in_DrawData.OverrideUvCoords ? in_DrawData.TopLeft : in_DrawData.OriginCast.TopLeft;
            var bottomLeft = in_DrawData.OverrideUvCoords ? in_DrawData.BottomLeft : in_DrawData.OriginCast.BottomLeft;
            var topRight = in_DrawData.OverrideUvCoords ? in_DrawData.TopRight : in_DrawData.OriginCast.TopRight;
            var bottomRight = in_DrawData.OverrideUvCoords ? in_DrawData.BottomRight : in_DrawData.OriginCast.BottomRight;

            quad.TopLeft.Position = in_DrawData.Position + ((topLeft * scale * aspect) / aspect);
            quad.BottomLeft.Position = in_DrawData.Position + ((bottomLeft * scale * aspect) / aspect);
            quad.TopRight.Position = in_DrawData.Position + ((topRight * scale * aspect) / aspect);
            quad.BottomRight.Position = in_DrawData.Position + ((bottomRight * scale * aspect) / aspect);
            quad.OriginalData = in_DrawData;

            m_Quads.Add(quad);
        }
        public void DrawSprite(SSpriteDrawData in_DrawData)
        {
            /// TODO: wtf is NextSprite or SpriteFactor?
            var quad = new Quad();
            var aspect = new Vector2(in_DrawData.AspectRatio, 1.0f);
            float rotation = in_DrawData.Rotation * MathF.PI / 180.0f;
            var topLeft = in_DrawData.OverrideUvCoords ? in_DrawData.TopLeft : in_DrawData.OriginCast.TopLeft;
            var bottomLeft = in_DrawData.OverrideUvCoords ? in_DrawData.BottomLeft : in_DrawData.OriginCast.BottomLeft;
            var topRight = in_DrawData.OverrideUvCoords ? in_DrawData.TopRight : in_DrawData.OriginCast.TopRight;
            var bottomRight = in_DrawData.OverrideUvCoords ? in_DrawData.BottomRight :  in_DrawData.OriginCast.BottomRight;

            quad.TopLeft.Position = in_DrawData.Position + ((topLeft * in_DrawData.Scale * aspect).Rotate(rotation) / aspect);
            quad.BottomLeft.Position = in_DrawData.Position + ((bottomLeft * in_DrawData.Scale * aspect).Rotate(rotation) / aspect);
            quad.TopRight.Position = in_DrawData.Position + ((topRight * in_DrawData.Scale * aspect).Rotate(rotation) / aspect);
            quad.BottomRight.Position = in_DrawData.Position + ((bottomRight * in_DrawData.Scale * aspect).Rotate(rotation) / aspect);

            if (in_DrawData.Sprite != null && in_DrawData.NextSprite != null)
            {
                var begin = new Vector2(
                    in_DrawData.Sprite.Start.X / in_DrawData.Sprite.Texture.Width,
                    in_DrawData.Sprite.Start.Y / in_DrawData.Sprite.Texture.Height);

                var nextBegin = new Vector2(
                    in_DrawData.NextSprite.Start.X / in_DrawData.NextSprite.Texture.Width,
                    in_DrawData.NextSprite.Start.Y / in_DrawData.NextSprite.Texture.Height);

                var end = begin + new Vector2(
                    in_DrawData.Sprite.Dimensions.X / in_DrawData.Sprite.Texture.Width,
                    in_DrawData.Sprite.Dimensions.Y / in_DrawData.Sprite.Texture.Height);

                var nextEnd = nextBegin + new Vector2(
                    in_DrawData.NextSprite.Dimensions.X / in_DrawData.NextSprite.Texture.Width,
                    in_DrawData.NextSprite.Dimensions.Y / in_DrawData.NextSprite.Texture.Height);

                begin = (1.0f - in_DrawData.SpriteFactor) * begin + in_DrawData.SpriteFactor * nextBegin;
                end = (1.0f - in_DrawData.SpriteFactor) * end + in_DrawData.SpriteFactor * nextEnd;

                if ((in_DrawData.Flags & ElementMaterialFlags.MirrorX) != 0) (begin.X, end.X) = (end.X, begin.X); // Mirror X
                if ((in_DrawData.Flags & ElementMaterialFlags.MirrorY) != 0) (begin.Y, end.Y) = (end.Y, begin.Y); // Mirror Y

                quad.TopLeft.Uv = begin;
                quad.TopRight.Uv = new Vector2(end.X, begin.Y);
                quad.BottomLeft.Uv = new Vector2(begin.X, end.Y);
                quad.BottomRight.Uv = end;
                quad.Texture = in_DrawData.Sprite.Texture;
            }

            quad.TopLeft.Color = in_DrawData.Color * in_DrawData.GradientTopLeft;
            quad.TopRight.Color = in_DrawData.Color * in_DrawData.GradientTopRight;
            quad.BottomLeft.Color = in_DrawData.Color * in_DrawData.GradientBottomLeft;
            quad.BottomRight.Color = in_DrawData.Color * in_DrawData.GradientBottomRight;

            quad.ZIndex = in_DrawData.ZIndex;
            quad.Additive = (in_DrawData.Flags & ElementMaterialFlags.AdditiveBlending) != 0;
            quad.LinearFiltering = (in_DrawData.Flags & ElementMaterialFlags.LinearFiltering) != 0;
            quad.OriginalData = in_DrawData;

            m_Quads.Add(quad);
        }
        public void DrawFullscreenQuad(KunaiSprite in_Spr, float in_Transparency)
        {
            /// TODO: wtf is NextSprite or SpriteFactor?
            var quad = new Quad();
            var topLeft = Vector2.Zero;
            var bottomRight = Vector2.One;

                var begin = new Vector2(
                    in_Spr.Start.X / in_Spr.Texture.Width,
                    in_Spr.Start.Y / in_Spr.Texture.Height);

                var end = begin + new Vector2(
                    in_Spr.Dimensions.X / in_Spr.Texture.Width,
                    in_Spr.Dimensions.Y / in_Spr.Texture.Height);

                begin = (1.0f) * begin;
                end = (1.0f) * end;

                quad.TopLeft.Uv = begin;
                quad.TopRight.Uv = new Vector2(end.X, begin.Y);
                quad.BottomLeft.Uv = new Vector2(begin.X, end.Y);
                quad.BottomRight.Uv = end;
            quad.TopRight.Position = new Vector2(1, 0);
            quad.BottomLeft.Position = new Vector2(0, 1);
            quad.BottomRight.Position = Vector2.One;
                quad.Texture = in_Spr.Texture;

            Vector4 color = new Vector4(in_Transparency, 1, 1, 1);
            quad.TopLeft.Color = color;
            quad.TopRight.Color = color;
            quad.BottomLeft.Color = color;
            quad.BottomRight.Color = color;

            quad.ZIndex = 0;
            quad.Additive = false;
            quad.LinearFiltering = true;
            quad.OriginalData.Unselectable = true;

            m_Quads.Add(quad);
        }
        public void SetShader(ShaderProgram in_Param)
        {
            m_Shader = in_Param;
            m_Shader.Use();
        }

        /// <summary>
        /// Clears the quad buffer and starts a new rendering batch.
        /// </summary>
        public void Start()
        {
            m_Quads.Clear();

            GLSingle.Ins.ActiveTexture(GLTextureUnit.Texture0);
            BeginBatch();
        }

        /// <summary>
        /// Draws the quads in the quad buffer.
        /// </summary>
        public void End()
        {

            foreach (var quad in m_Quads)
            {
                int id = -1;
                uint glId = quad.Texture?.GlTex?.Id ?? uint.MaxValue;
                if (glId != uint.MaxValue)
                    id = (int)glId;

                if (id != TextureId || Additive != quad.Additive || LinearFiltering != quad.LinearFiltering || NumVertices >= MaxVertices)
                {
                    EndBatch();
                    BeginBatch();

                    quad.Texture?.GlTex?.Bind();

                    TextureId = (int)id;
                    Additive = quad.Additive;
                    LinearFiltering = quad.LinearFiltering;
                }

                PushQuad(quad);
            }

            if (BatchStarted)
                EndBatch();
        }
    }
}
