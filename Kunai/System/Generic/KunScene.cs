using SharpNeedle.Framework.Ninja.Csd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public struct KunScene
    {
        public string Name;
        public int Priority;
        public int Framerate;
        public float AspectRatio;
        public List<List<KunCast>> Casts;
        public List<KunMotion> Motions;
        public KunExtension? Extension;
    }
}
