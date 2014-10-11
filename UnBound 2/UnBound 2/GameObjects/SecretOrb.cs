using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameObjects
{
    public class SecretOrb : Orb
    {
        public SecretOrb(GraphicsDevice device, Vector3 position)
            : base(device, position)
        {
            this.color = new Vector4(0.5f, 0.75f, 1.0f, 1.0f); // CRYSTAL
        }
    }
}
