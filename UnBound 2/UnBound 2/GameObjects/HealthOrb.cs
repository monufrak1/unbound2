using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameObjects
{
    public class HealthOrb : Orb
    {
        public HealthOrb(GraphicsDevice device, Vector3 position)
            : base(device, position)
        {
            this.color = new Vector4(1.0f, 0.25f, 0.35f, 1.0f); // RED
        }
    }
}
