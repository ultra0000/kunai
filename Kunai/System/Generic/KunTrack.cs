using SharpNeedle.Framework.Ninja.Csd.Motions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public struct KunTrack
    {
        public uint Field00;
        public List<KunFrame> Frames;
        public TrackType TrackType;       
    }
}
