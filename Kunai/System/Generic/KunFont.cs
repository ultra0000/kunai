using SharpNeedle.Framework.Ninja.Csd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public struct KunFont
    {
        public struct XmlCharacter
        {
            public int SourceIndex;
            public int DestinationIndex;
        }
        public string Name;
        public List<XmlCharacter> Characters;
    }
}
