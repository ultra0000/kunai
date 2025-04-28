using SharpNeedle.Framework.Ninja.Csd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Kunai.Generic
{
    public class CsdCastExtension : KunExtension
    {
        public Vector2 Origin;
        public uint Field00 { get; set; }
        public bool Enabled { get; set; }
        public Vector2 TopLeft { get; set; }
        public Vector2 BottomLeft { get; set; }
        public Vector2 TopRight { get; set; }
        public Vector2 BottomRight { get; set; }
        public Vector2 CseSize;
        public Vector2 CsePosition;
        public uint Field58 { get; set; }
        public uint Field5C { get; set; }
        public uint Field70 { get; set; }
        public CsdCastExtension()
        {
            ExtName = "Csd";
        }
        public CsdCastExtension(Cast in_Cast)
        {
            Origin = in_Cast.Origin;
            ExtName = "Csd";
            Field00 = in_Cast.Field00;
            Enabled = in_Cast.Enabled;
            TopLeft = in_Cast.TopLeft;
            BottomLeft = in_Cast.BottomLeft;
            TopRight = in_Cast.TopRight;
            BottomRight = in_Cast.BottomRight;
            CseSize = new Vector2(in_Cast.Width, in_Cast.Height);
            Field58 = in_Cast.Field58;
            Field5C = in_Cast.Field5C;
            Field70 = in_Cast.Field70;
            CsePosition = in_Cast.Position;
        }
    }
}
