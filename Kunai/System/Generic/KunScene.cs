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

        public int GetFamilyIndex(KunCast in_KunCast)
        {
            for (int i = 0; i < Casts.Count; i++)
            {
                List<KunCast> fam = Casts[i];
                foreach (var cast in fam)
                {
                    if (cast == in_KunCast)
                        return i;

                }
            }
            return -1;
        }
    }
}
