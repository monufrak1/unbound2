using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameObjects
{
    public class AgilityOrb : Orb
    {
        public AgilityOrb(GraphicsDevice device, Vector3 position)
            : base(device, position)
        {
            this.color = new Vector4(0.5f, 1.0f, 0.75f, 1.0f); // GREEN
        }
    }
}