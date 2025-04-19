using SharpNeedle.Framework.Ninja.Csd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public class CsdSceneExtension : KunExtension
    {
        public List<Vector2> Textures = new List<Vector2>();
        public List<Sprite> Sprites = new List<Sprite>();
        public CsdSceneExtension(Scene in_Scene)
        {
            Textures = in_Scene.Textures;
            Sprites = in_Scene.Sprites;
        }
    }
}
