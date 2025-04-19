using SharpNeedle.Framework.Ninja.Csd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public class CsdNNTexture : KunExtension
    {
        public uint Field00 { get; set; }
        public ushort Field08 { get; set; } = 5; // Always 5
        public ushort Field0A { get; set; } = 1; // Always 1
        public uint Field0C { get; set; }
        public uint Field10 { get; set; }
        public CsdNNTexture(TextureNN in_Tex)
        {
            Field00 = in_Tex.Field00;
            Field08 = in_Tex.Field08;
            Field0A = in_Tex.Field0A;
            Field0C = in_Tex.Field0C;
            Field10 = in_Tex.Field10;
        }
    }
}
