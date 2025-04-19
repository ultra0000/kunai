using Newtonsoft.Json;
using SharpNeedle.Framework.Ninja.Csd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public struct KunTexture
    {
        public string Name;
        [JsonIgnore]
        public byte[] TextureData = [];
        public KunExtension? Extension;

        public KunTexture()
        {
        }
    }
}
