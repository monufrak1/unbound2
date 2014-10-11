using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Graphics3D
{
    class ParticleEmitter
    {
        private Vector3 position;
        private Texture2D particleTexture;

        private int maxParticles;
        private float maxParticleAge;
        private int numActiveParticles;
        private List<Particle> activeParticles;

        private ParticleEmitterUpdater particleUpdater;

        private BillboardRenderer billboardRenderer;

        private GraphicsDevice device;
        private Random rand;

        public ParticleEmitter(GraphicsDevice device, ContentManager content,
                               Vector3 position, Texture2D particleTexture,
                               ParticleEmitterUpdater particleUpdater,
                               int maxParticles, float maxParticleAge)
        {
            this.device = device;

            this.position = position;
            this.particleTexture = particleTexture;
            this.particleUpdater = particleUpdater;
            this.maxParticles = maxParticles;
            this.maxParticleAge = maxParticleAge;

            rand = new Random();
            billboardRenderer = new BillboardRenderer(device, content.Load<Effect>(@"Effects\Billboard"));

            // Create particle list
            Reset();
        }

        public void EmitParticles(int numParticles, Vector3 direction, Vector2 size, float speed)
        {
            for (int i = 0; i < numParticles; i++)
            {
                EmitParticle(direction, size, speed);
            }
        }

        public void EmitParticle(Vector3 direction, Vector2 size, float speed)
        {
            // Find 'dead' particle to emit
            Particle p = null;
            for (int i = 0; i < activeParticles.Count; i++)
            {
                if (!activeParticles[i].IsAlive)
                {
                    p = activeParticles[i];
                    break;
                }
            }

            // Emit a new particle
            if (p != null)
            {
                // Set this particle's data
                p.IsAlive = true;
                p.Position = position;
                p.Direction = direction;
                p.Size = size;
                p.Speed = speed;
                p.Age = 0.0f;
            }
        }

        public void EmitRandomParticles(int numParticles, Vector3 direction, Vector2 size, float speed)
        {
            for (int i = 0; i < numParticles; i++)
            {
                // Emit a new particle with random changes
                Vector3 randDirection = direction;
                randDirection.X += (float)(rand.NextDouble()) - 0.5f;
                randDirection.Y += (float)(rand.NextDouble()) - 0.5f;
                randDirection.Z += (float)(rand.NextDouble()) - 0.5f;

                Vector2 randSize = Vector2.One;
                randSize.X = (float)(rand.NextDouble() * size.X) + size.X * 0.25f;
                randSize.Y = (float)(rand.NextDouble() * size.Y) + size.Y * 0.25f;

                float randSpeed = speed;
                randSpeed += (float)(rand.NextDouble()) - (speed * 0.25f);

                EmitParticle(randDirection, randSize, randSpeed);
            }
        }

        public void Reset()
        {
            // Create list if needed
            if (activeParticles == null)
            {
                activeParticles = new List<Particle>(maxParticles);
                for (int i = 0; i < maxParticles; i++)
                {
                    // Create particle
                    activeParticles.Add(new Particle(device, Vector3.Zero, Vector3.Zero,
                        Vector2.Zero, 0.0f, ParticleTexture));
                }
            }
            else
            {
                // Set all particles to 'dead'
                foreach (Particle p in activeParticles)
                {
                    p.IsAlive = false;
                }
            }
        }

        public void Update(float dt)
        {
            numActiveParticles = 0;

            // Update all particles with particle updater
            foreach (Particle p in activeParticles)
            {
                if (p.IsAlive)
                {
                    particleUpdater.UpdateParticle(p, dt);

                    if (p.Age > maxParticleAge)
                    {
                        // Mark this particle as 'dead'
                        p.IsAlive = false;
                    }
                    else
                    {
                        numActiveParticles++;
                    }
                }
            }

            billboardRenderer.UpdateEffectVariables();
        }

        public void DrawParticles(FirstPersonCamera camera)
        {
            // Draw each particle
            foreach (Particle p in activeParticles)
            {
                if (p.IsAlive)
                {
                    // Draw the particle's billboard
                    if(camera.ViewFrustum.Intersects(p.ParticleBillboard.AABB))
                    {
                        billboardRenderer.DrawParticle(p.ParticleBillboard, Vector3.Up, camera.Look);
                    }
                }
            }
        }

        // PROPERTIES
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Texture2D ParticleTexture
        {
            get { return particleTexture; }
        }

        public int MaxParticles
        {
            get { return maxParticles; }
            set 
            { 
                maxParticles = value; 
            
                // Build new list of particles
                activeParticles = null;
                Reset();
            }
        }

        public float MaxParticleAge
        {
            get { return maxParticleAge; }
            set { maxParticleAge = value; }
        }

        public int NumActiveParticles
        {
            get { return numActiveParticles; }
        }
    }
}