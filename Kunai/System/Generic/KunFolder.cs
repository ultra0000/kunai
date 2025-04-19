using SharpNeedle.Framework.Ninja.Csd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public struct KunSceneNode
    {
        public string Name;
        public List<KunScene> Scenes;
        public List<KunSceneNode> Children;
        
    }
}
