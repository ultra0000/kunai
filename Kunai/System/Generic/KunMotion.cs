using SharpNeedle.Framework.Ninja.Csd.Motions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public struct KunMotion
    {
        public string Name;
        public float StartFrame;
        public float EndFrame;
        public List<List<KunCastMotion>> CastMotions;        
    }
}
