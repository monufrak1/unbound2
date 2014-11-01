using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;

using Graphics3D;

namespace GameObjects
{
    class UnboundLevel : Level
    {
        private List<Orb> orbList;

        private SoundEffect orbCollectionSFX;
        private SoundEffectInstance secretOrbSFX;
        private AudioListener audioListener;
        private AudioEmitter secretOrbAudioEmitter;

        public UnboundLevel(GraphicsDevice device, ContentManager content, string levelFileName)
            : base(device, content, levelFileName)
        {
            // Only use Outdoor levels
            if (type == LevelType.Outdoor)
            {
                // Create Orbs
                orbList = new List<Orb>();

                // Remove orb meshes and create orb objects
                List<Mesh> meshListCopy = new List<Mesh>(meshList);
                foreach (Mesh m in meshListCopy)
                {
                    bool removeMesh = false;
                    Vector3 pos = m.Position;
                    if (m.Scale == 1.0f)
                    {
                        // Set these orbs to float just above terrain height
                        pos.Y = terrain.GetHeight(pos.X, pos.Z) + cameraHeightOffset * 0.75f;
                    }

                    if (m.MeshFileName == "agilityOrb.m3d")
                    {
                        // Add orb
                        orbList.Add(new AgilityOrb(device, pos));
                        removeMesh = true;
                    }
                    else if (m.MeshFileName == "secretOrb.m3d")
                    {
                        // Add orb
                        orbList.Add(new SecretOrb(device, pos));
                        removeMesh = true;
                    }
                    else if (m.MeshFileName == "healthOrb.m3d")
                    {
                        // Add orb
                        orbList.Add(new HealthOrb(device, pos));
                        removeMesh = true;
                    }

                    if (removeMesh)
                    {
                        meshList.Remove(m);
                    }
                }

                // Create sound effects
                audioListener = new AudioListener();
                secretOrbAudioEmitter = new AudioEmitter();

                orbCollectionSFX = content.Load<SoundEffect>(@"SoundEffects\orbCollectSFX");
                secretOrbSFX = content.Load<SoundEffect>(@"SoundEffects\secretOrbSFX").CreateInstance();
                secretOrbSFX.Volume = 0.0f;
                secretOrbSFX.IsLooped = true;
                secretOrbSFX.Apply3D(audioListener, secretOrbAudioEmitter);
                secretOrbSFX.Play();
            }
        }

        public new void Update(FirstPersonCamera camera, float dt)
        {
            base.Update(camera, dt);

            audioListener.Position = camera.Position;
            audioListener.Forward = camera.Look;
            audioListener.Up = camera.Up;

            Orb closestSecretOrb = null;
            float secretOrbDistance = float.MaxValue;

            // Update orbs
            foreach (Orb orb in orbList)
            {
                orb.Update(dt);
                if (orb.IsAlive)
                {
                    if (orb is SecretOrb)
                    {
                        // Set secret orb distance
                        float dist = Vector3.Distance(camera.Position, orb.Position);
                        if (dist < secretOrbDistance)
                        {
                            secretOrbDistance = dist;
                            closestSecretOrb = orb;
                        }
                    }

                    // Collection Check
                    if (orb.CollisionSphere.Intersects(camera.AABB))
                    {
                        orb.IsAlive = false;
                        ActivePlayer.Profile.CollectOrb(orb);
                        orbCollectionSFX.Play();
                    }
                }
            }

            // Set secret orb SFX audio emitter
            if (closestSecretOrb != null)
            {
                secretOrbAudioEmitter.Position = closestSecretOrb.Position;
                secretOrbAudioEmitter.Forward = Vector3.Normalize(
                    Vector3.Subtract(closestSecretOrb.Position, camera.Position));
                secretOrbSFX.Volume = 1.0f - Math.Min((secretOrbDistance / 100.0f), 1.0f);
            }
            else
            {
                if (secretOrbSFX.Volume - dt >= 0.0f) secretOrbSFX.Volume -= dt;
            }
            
        }

        public void StopAudio()
        {
            AmbientSFX.Stop();
            secretOrbSFX.Stop();
        }

        public int NumOrbsAlive()
        {
            int numOrbsAlive = 0;
            foreach (Orb orb in orbList)
            {
                if (orb.IsAlive)
                {
                    numOrbsAlive++;
                }
            }

            return numOrbsAlive;
        }

        public int NumSecretOrbsAlive()
        {
            int numSecretOrbsAlive = 0;
            foreach(Orb orb in orbList)
            {
                if(orb is SecretOrb && orb.IsAlive)
                {
                    numSecretOrbsAlive++;
                }
            }

            return numSecretOrbsAlive;
        }

        public int NumAgilityOrbsAlive()
        {
            int numAgilityOrbsAlive = 0;
            foreach (Orb orb in orbList)
            {
                if (orb is AgilityOrb && orb.IsAlive)
                {
                    numAgilityOrbsAlive++;
                }
            }

            return numAgilityOrbsAlive;
        }

        public int NumHealthOrbsAlive()
        {
            int numHealthOrbsAlive = 0;
            foreach (Orb orb in orbList)
            {
                if (orb is HealthOrb && orb.IsAlive)
                {
                    numHealthOrbsAlive++;
                }
            }

            return numHealthOrbsAlive;
        }

        public int TotalSecretOrbs()
        {
            int totalSecretOrbs = 0;
            foreach (Orb orb in orbList)
            {
                if (orb is SecretOrb)
                {
                    totalSecretOrbs++;
                }
            }

            return totalSecretOrbs;
        }

        public int TotalAgilityOrbs()
        {
            int totalAgilityOrbs = 0;
            foreach (Orb orb in orbList)
            {
                if (orb is AgilityOrb)
                {
                    totalAgilityOrbs++;
                }
            }

            return totalAgilityOrbs;
        }

        public int TotalHealthOrbs()
        {
            int totalHealthOrbs = 0;
            foreach (Orb orb in orbList)
            {
                if (orb is HealthOrb)
                {
                    totalHealthOrbs++;
                }
            }

            return totalHealthOrbs;
        }

        public int TotalOrbs()
        {
            return orbList.Count;
        }

        public int NumOrbsCollected()
        {
            return orbList.Count() - NumOrbsAlive();
        }

        public int NumSecretOrbsCollected()
        {
            return TotalSecretOrbs() - NumSecretOrbsAlive();
        }

        public int NumAgilityOrbsCollected()
        {
            return TotalAgilityOrbs() - NumAgilityOrbsAlive();
        }

        public int NumHealthOrbsCollected()
        {
            return TotalHealthOrbs() - NumHealthOrbsAlive();
        }

        // Properties
        public List<Orb> OrbList
        {
            get { return orbList; }
        }
    }
}
