using HekonrayBase;

namespace Kunai.ShurikenRenderer
{

    public partial class KunaiProject
    {
        public struct SViewportData
        {
            public uint CsdRenderTextureHandle;
            public Vector2Int FramebufferSize;
            public uint RenderbufferHandle;
            public uint FramebufferHandle;
        }
    }
}
