using SharpNeedle.Framework.Ninja.Csd.Motions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public class CsdKeyframeExtension : KunExtension
    {
        public AspectRatioCorrection? Correction { get; set; }
        public CsdKeyframeExtension(KeyFrame in_Frame)
        {
            if (in_Frame.Correction != null)
                Correction = in_Frame.Correction;
        }
    }
}
