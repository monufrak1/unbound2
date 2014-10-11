using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Graphics3D;

namespace GameObjects
{
    public abstract class Orb
    {
        private static float ORB_RADIUS = 3.0f;

        // Geometry data is shared for all orbs
        private static GraphicsDevice device;
        private static VertexBuffer vertexBuffer;
        private static IndexBuffer indexBuffer;
        private static int numVertices;
        private static int numIndices;
        private static bool geometryLoaded = false;

        private static Random rand = new Random();
        private float randomOffset;

        protected BoundingSphere boundingSphere;
        protected BoundingSphere collisionSphere;
        protected Vector3 position;
        protected Vector4 color;
        protected float rotationAngle;

        protected bool isAlive = true;

        protected Matrix worldMatrix;



        public Orb(GraphicsDevice device, Vector3 position)
        {
            Orb.device = device;

            this.position = position;
            this.color = Vector4.One;

            this.isAlive = true;

            randomOffset = (float)rand.NextDouble() + 1.0f;

            // Create geometry if needed
            if(!geometryLoaded)
            {
                GenerateGeometry();
                geometryLoaded = true;
            }
        }

        private static void GenerateGeometry()
        {
            float X = 0.525731f;
            float Z = 0.850651f;

            VertexPosNorm[] vertices = new VertexPosNorm[]
            {
                new VertexPosNorm(new Vector3(-X, 0.0f, -Z), Vector3.Zero),
                new VertexPosNorm(new Vector3(X, 0.0f, -Z), Vector3.Zero),  
                new VertexPosNorm(new Vector3(-X, 0.0f, Z), Vector3.Zero),
                new VertexPosNorm(new Vector3(X, 0.0f, Z), Vector3.Zero),    
                new VertexPosNorm(new Vector3(0.0f, Z, -X), Vector3.Zero),
                new VertexPosNorm(new Vector3(0.0f, Z, X), Vector3.Zero), 
                new VertexPosNorm(new Vector3(0.0f, -Z, -X), Vector3.Zero),
                new VertexPosNorm(new Vector3(0.0f, -Z, X), Vector3.Zero),    
                new VertexPosNorm(new Vector3(Z, X, 0.0f), Vector3.Zero),
                new VertexPosNorm(new Vector3(-Z, X, 0.0f), Vector3.Zero), 
                new VertexPosNorm(new Vector3(Z, -X, 0.0f), Vector3.Zero),
                new VertexPosNorm(new Vector3(-Z, -X, 0.0f), Vector3.Zero)
            };


            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].pos.Normalize();
                vertices[i].norm = vertices[i].pos;
                vertices[i].pos *= ORB_RADIUS;
            }

            int[] indices = new int[] 
            {
                1,4,0,  4,9,0,  4,5,9,  8,5,4,  1,8,4,    
                1,10,8, 10,3,8, 8,3,5,  3,2,5,  3,7,2,    
                3,10,7, 10,6,7, 6,11,7, 6,0,11, 6,1,0, 
                10,1,6, 11,0,9, 2,11,9, 5,2,9,  11,2,7 
            };

            SubdivideGeometry(ref vertices, ref indices);

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].pos.Normalize();
                vertices[i].norm = vertices[i].pos;
                vertices[i].pos *= ORB_RADIUS;
            }

            numVertices = vertices.Length;
            numIndices = indices.Length;

            // Create vertex buffer
            vertexBuffer = new VertexBuffer(device, VertexPosNorm.VertexLayout, 
                numVertices, BufferUsage.WriteOnly);    
            vertexBuffer.SetData(vertices);

            // Create index buffer
            indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits,
                numIndices, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
        }

        private static void SubdivideGeometry(ref VertexPosNorm[] vertices, ref int[] indices)
        {
            List<VertexPosNorm> newVertices = new List<VertexPosNorm>();
            List<int> newIndices = new List<int>();

            for (int subdivide = 0; subdivide < 3; subdivide++)
            {
                List<VertexPosNorm> vin = new List<VertexPosNorm>(vertices);
                List<int> iin = new List<int>(indices);

                //       v1
                //       *
                //      / \
                //     /   \
                //  m0*-----*m1
                //   / \   / \
                //  /   \ /   \
                // *-----*-----*
                // v0    m2     v2

                int numTris = iin.ToArray().Length / 3;
                for (int i = 0; i < numTris; ++i)
                {
                    Vector3 v0 = vin[iin[i * 3 + 0]].pos;
                    Vector3 v1 = vin[iin[i * 3 + 1]].pos;
                    Vector3 v2 = vin[iin[i * 3 + 2]].pos;

                    Vector3 m0 = 0.5f * (v0 + v1);
                    Vector3 m1 = 0.5f * (v1 + v2);
                    Vector3 m2 = 0.5f * (v0 + v2);

                    newVertices.Add(new VertexPosNorm(v0, Vector3.Zero)); // 0
                    newVertices.Add(new VertexPosNorm(v1, Vector3.Zero)); // 1
                    newVertices.Add(new VertexPosNorm(v2, Vector3.Zero)); // 2
                    newVertices.Add(new VertexPosNorm(m0, Vector3.Zero)); // 3
                    newVertices.Add(new VertexPosNorm(m1, Vector3.Zero)); // 4
                    newVertices.Add(new VertexPosNorm(m2, Vector3.Zero)); // 5

                    newIndices.Add(i * 6 + 0);
                    newIndices.Add(i * 6 + 3);
                    newIndices.Add(i * 6 + 5);

                    newIndices.Add(i * 6 + 3);
                    newIndices.Add(i * 6 + 4);
                    newIndices.Add(i * 6 + 5);

                    newIndices.Add(i * 6 + 5);
                    newIndices.Add(i * 6 + 4);
                    newIndices.Add(i * 6 + 2);

                    newIndices.Add(i * 6 + 3);
                    newIndices.Add(i * 6 + 1);
                    newIndices.Add(i * 6 + 4);
                }

                vertices = newVertices.ToArray();
                indices = newIndices.ToArray();
            }
        }

        public void Update(float dt)
        {
            if (isAlive)
            {
                rotationAngle += dt;
                float yOffset = (float)Math.Cos(rotationAngle * randomOffset);
                
                Matrix rotationMtx = Matrix.CreateRotationY(rotationAngle);
                Matrix translationMtx = Matrix.CreateTranslation(new Vector3(position.X,
                    position.Y + yOffset, position.Z));

                // Update world matrix
                worldMatrix = rotationMtx * translationMtx;

                // Update bounding sphere
                boundingSphere = new BoundingSphere(position, ORB_RADIUS);
                collisionSphere = new BoundingSphere(position, ORB_RADIUS * 3.0f);
            }
        }

        public bool FrustumCull(BoundingFrustum viewFrustum)
        {
            return viewFrustum.Intersects(boundingSphere);
        }

        // PROPERTIES
        public VertexBuffer Vertices
        {
            get { return vertexBuffer; }
        }

        public IndexBuffer Indices
        {
            get { return indexBuffer; }
        }

        public int NumVertices
        {
            get { return numVertices; }
        }

        public int NumIndices
        {
            get { return numIndices; }
        }
   
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector4 Color
        {
            get { return color; }
        }

        public bool IsAlive
        {
            get { return isAlive; }
            set { isAlive = value; }
        }

        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
        }

        public BoundingSphere BoundingSphere
        {
            get { return boundingSphere; }
        }

        public BoundingSphere CollisionSphere
        {
            get { return collisionSphere; }
        }
    }

    public class OrbRenderer
    {
        private GraphicsDevice device;
        private Effect effect;
        private SpriteBatch spriteBatch;
        private RenderTarget2D refractionMap;
        private Rectangle screenRect;

        public OrbRenderer(GraphicsDevice device, Effect effect)
        {
            this.device = device;
            this.effect = effect;

            spriteBatch = new SpriteBatch(device);
            refractionMap = new RenderTarget2D(device, device.Viewport.Width, device.Viewport.Height,
                false, SurfaceFormat.Color, DepthFormat.None);

            screenRect = new Rectangle(0, 0, refractionMap.Width, refractionMap.Height);
        }

        public void UpdateEffectVariables()
        {
            // Common Data
            effect.Parameters["g_EyePosW"].SetValue(SharedEffectParameters.xEyePosW);
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);
            effect.Parameters["g_LightView"].SetValue(SharedEffectParameters.xLightViewMatrix);
            effect.Parameters["g_LightProjection"].SetValue(SharedEffectParameters.xLightProjectionMatrix);

            // Light data
            effect.Parameters["g_LightDir"].SetValue(SharedEffectParameters.xLightDirection);
            effect.Parameters["g_LightAmbient"].SetValue(SharedEffectParameters.xLightAmbient);
            effect.Parameters["g_LightDiffuse"].SetValue(SharedEffectParameters.xLightDiffuse);
            effect.Parameters["g_LightSpecular"].SetValue(SharedEffectParameters.xLightSpecular);

            // Fog data
            effect.Parameters["g_FogStart"].SetValue(SharedEffectParameters.xFogStart);
            effect.Parameters["g_FogRange"].SetValue(SharedEffectParameters.xFogRange);
            effect.Parameters["g_SkyColor"].SetValue(SharedEffectParameters.xSkyColor);
            effect.Parameters["g_DarkSkyOffset"].SetValue(SharedEffectParameters.xDarkSkyOffset);
        }

        public void Draw(Orb orb)
        {
            // Set effect variables
            effect.Parameters["g_World"].SetValue(orb.WorldMatrix);
            effect.Parameters["g_OrbColor"].SetValue(orb.Color);

            // Draw Orb
            device.SetVertexBuffer(orb.Vertices);
            device.Indices = orb.Indices;

            effect.CurrentTechnique = effect.Techniques["OrbTech"];

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 
                    0, 0, orb.NumVertices, 0, orb.NumIndices/3);
            }
        }

        public void CreateRefractionMap(RenderTarget2D refractionRT)
        {
            // Copy data from refractionRT to a texture
            device.SetRenderTarget(refractionMap);
            spriteBatch.Begin();

            spriteBatch.Draw(refractionRT, screenRect, Color.White);

            spriteBatch.End();
            device.SetRenderTarget(refractionRT);
            device.DepthStencilState = DepthStencilState.Default;
        }

        public void DrawWithRefraction(Orb orb)
        {
            // Set effect variables
            effect.Parameters["g_World"].SetValue(orb.WorldMatrix);
            effect.Parameters["g_OrbColor"].SetValue(orb.Color);
            effect.Parameters["g_RefractionMap"].SetValue(refractionMap);

            // Draw Orb
            device.SetVertexBuffer(orb.Vertices);
            device.Indices = orb.Indices;

            effect.CurrentTechnique = effect.Techniques["OrbRefractionTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, orb.NumVertices, 0, orb.NumIndices / 3);
            }
        }

        public void DrawOcclusion(Orb orb)
        {
            // Set effect variables
            effect.Parameters["g_OrbColor"].SetValue(orb.Color);
            effect.Parameters["g_World"].SetValue(orb.WorldMatrix);
            effect.Parameters["g_View"].SetValue(SharedEffectParameters.xViewMatrix);
            effect.Parameters["g_Projection"].SetValue(SharedEffectParameters.xProjectionMatrix);

            // Draw Orb
            device.SetVertexBuffer(orb.Vertices);
            device.Indices = orb.Indices;

            effect.CurrentTechnique = effect.Techniques["OrbOcclusionTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, orb.NumVertices, 0, orb.NumIndices / 3);
            }
        }

        public void DrawShadow(Orb orb)
        {
            RasterizerState originalRasterizerState = device.RasterizerState;
            BlendState originalBlendState = device.BlendState;

            device.RasterizerState = RasterizerState.CullNone;
            device.BlendState = BlendState.Opaque;

            // Set effect variables
            effect.Parameters["g_World"].SetValue(orb.WorldMatrix);

            // Draw Orb
            device.SetVertexBuffer(orb.Vertices);
            device.Indices = orb.Indices;

            effect.CurrentTechnique = effect.Techniques["OrbShadowTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, orb.NumVertices, 0, orb.NumIndices / 3);
            }

            device.BlendState = originalBlendState;
            device.RasterizerState = originalRasterizerState;
        }

        public void DrawBloom(Orb orb)
        {
            // Set effect variables
            effect.Parameters["g_World"].SetValue(orb.WorldMatrix);
            effect.Parameters["g_OrbColor"].SetValue(orb.Color);

            // Draw Orb
            device.SetVertexBuffer(orb.Vertices);
            device.Indices = orb.Indices;

            effect.CurrentTechnique = effect.Techniques["OrbBloomTech"];

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, orb.NumVertices, 0, orb.NumIndices / 3);
            }
        }
    }
}
