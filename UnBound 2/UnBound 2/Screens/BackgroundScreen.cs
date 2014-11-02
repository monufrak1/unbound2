#region File Description
//-----------------------------------------------------------------------------
// BackgroundScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements

using System;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

using Graphics3D;
using GameObjects;

#endregion

namespace GameStateManagement
{

    class BackgroundScreen : GameScreen
    {
        GraphicsDevice graphicsDevice;
        ContentManager content;
        SpriteBatch spriteBatch;
        SpriteFont font;

        int screenWidth;
        int screenHeight;

        // Engine flags
        bool shadowsEnabled;
        bool postProcessingEffectsEnabled;
        bool displayFPS;

        Color clearColor;

        RenderTarget2D mainRT;
        RenderTarget2D reflectionRT;
        RenderTarget2D occlusionRT;
        RenderTarget2D bloomRT;
        PostProcessingEffects postEffects;

        FootstepSFXPlayer footstepPlayer;
        FirstPersonCamera camera;

        // Renderers
        LightRenderer lightRenderer;
        TerrainRenderer terrainRenderer;
        SurfaceRenderer surfaceRenderer;
        WaterRenderer waterRenderer;
        BillboardRenderer billboardRenderer;
        MeshRenderer meshRenderer;
        OrbRenderer orbRenderer;

        // Level
        string levelFileName;
        UnboundLevel level;

        public BackgroundScreen()
            : base()
        {
            base.TransitionOnTime = new TimeSpan(0, 0, 4);

            // Get all level files in directory
            levelFileName = @"TestUnboundLevel.lvl";

            // Set graphics flags
            shadowsEnabled = true;
            postProcessingEffectsEnabled = true;
            displayFPS = false;
        }

        public override void LoadContent()
        {
            Initialize();

            ScreenManager.Game.ResetElapsedTime();
        }

        public override void UnloadContent()
        {
            level.StopAudio();
        }

        private void Initialize()
        {
            graphicsDevice = ScreenManager.GraphicsDevice;
            content = ScreenManager.Content;
            spriteBatch = ScreenManager.SpriteBatch;
            font = ScreenManager.Font;
            MeshManager.InitializeManager(graphicsDevice, content);

            screenWidth = graphicsDevice.Viewport.Width;
            screenHeight = graphicsDevice.Viewport.Height;

            clearColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            // Create Render Targets
            mainRT = new RenderTarget2D(graphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            reflectionRT = new RenderTarget2D(graphicsDevice, screenWidth / 2, screenHeight / 2, true, SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8);
            occlusionRT = new RenderTarget2D(graphicsDevice, screenWidth / 8, screenHeight / 8, false, SurfaceFormat.Color,
                DepthFormat.None);
            bloomRT = new RenderTarget2D(graphicsDevice, screenWidth / 8, screenHeight / 8, false, SurfaceFormat.Color,
                DepthFormat.None);

            postEffects = new PostProcessingEffects(graphicsDevice,
                content.Load<Effect>(@"Effects\PostProcessingEffects"));

            // Create renderers
            lightRenderer = new LightRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Light"));
            terrainRenderer = new TerrainRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Terrain"));
            surfaceRenderer = new SurfaceRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Surface"));
            waterRenderer = new WaterRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Water"));
            billboardRenderer = new BillboardRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Billboard"));
            meshRenderer = new MeshRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Mesh"));
            orbRenderer = new OrbRenderer(graphicsDevice,
                content.Load<Effect>(@"Effects\Orb"));

            // Create camera
            camera = new FirstPersonCamera();
            camera.AspectRatio = graphicsDevice.Viewport.AspectRatio;
            camera.AABBSize = new Vector2(1.0f, 8.0f);
            camera.DrawDistance = 10000.0f;
            camera.MoveSpeed = 25.0f;
            camera.FreeFlyEnabled = false;
            camera.PitchMinDegrees = -75.0f;
            camera.PitchMaxDegrees = 60.0f;
            camera.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
                                camera.AspectRatio, 0.1f, 10000.0f);

            // Load level data
            LoadLevel(@"Levels\" + levelFileName);
        }

        private void LoadLevel(string levelFileName)
        {
            // Load level
            level = new UnboundLevel(graphicsDevice, content, levelFileName);

            camera.Position = level.CameraStartPosition;
            camera.Look = level.CameraStartDirection;
            camera.DrawDistance = level.FogRange + level.FogStart;

            if (level.ActiveLight.Size <= 0.0f)
            {
                // Disable light scattering
                postProcessingEffectsEnabled = false;
            }

            footstepPlayer = new FootstepSFXPlayer(
                new SoundEffect[] { level.FootstepSFX1, level.FootstepSFX2 },
                0.75f, 0.25f);

            // Create sound players
            level.AmbientSFX.Play();
            level.WeatherSFX.Play();
            level.WaterSFX.Play();

            level.DayNightCycleSpeed = 0.1f;

            // Set initial effect parameters
            SharedEffectParameters.xProjectionMatrix = camera.Projection;
            SharedEffectParameters.xReflectionProjectionMatrix = camera.Projection;

            SharedEffectParameters.xSkyColor = level.SkyColor;
            SharedEffectParameters.xDarkSkyOffset = level.DarkSkyOffset;
            SharedEffectParameters.xFogStart = level.FogStart;
            SharedEffectParameters.xFogRange = level.FogRange;

            SharedEffectParameters.xWaterColor = level.WaterColor;
            SharedEffectParameters.xWaterHeight = level.WaterHeight;
            SharedEffectParameters.xDeepWaterFogDistance = level.DeepWaterFogDistance;

            SharedEffectParameters.xLightAmbient = level.ActiveLight.Ambient;
            SharedEffectParameters.xLightDiffuse = level.ActiveLight.Diffuse;
            SharedEffectParameters.xLightSpecular = level.ActiveLight.Specular;
            SharedEffectParameters.xLightDirection = level.ActiveLight.Direction;
            SharedEffectParameters.xLightProjectionMatrix = level.ActiveLight.ProjectionMatrix;
            SharedEffectParameters.xShadowsEnabled = shadowsEnabled;
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                               bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, false);

            UpdateGame(gameTime);
        }

        Vector3 offsetVec = new Vector3(1000.0f, 600.0f, 1000.0f);
        private void UpdateGame(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Rotate camera around level
            camera.Position = Vector3.Transform(level.CameraStartPosition + offsetVec,
                Matrix.CreateRotationY((float)gameTime.TotalGameTime.TotalSeconds / 20));
            camera.Look = Vector3.Subtract(Vector3.Zero, camera.Position);

            level.Update(camera, dt);

            // Update effect parameters
            SharedEffectParameters.xTime = (float)gameTime.TotalGameTime.TotalSeconds;
            SharedEffectParameters.xEyePosW = camera.Position;
            SharedEffectParameters.xEyeDirection = camera.Look;
            SharedEffectParameters.xViewMatrix = camera.View;
            SharedEffectParameters.xLightViewMatrix = level.ActiveLight.ViewMatrix;
            SharedEffectParameters.xReflectionViewMatrix = level.Water.CalculateReflectionMatrix(camera.Position, camera.Look);

            SharedEffectParameters.xLightPosition = level.ActiveLight.Position;
            SharedEffectParameters.xLightDirection = level.ActiveLight.Direction;
            SharedEffectParameters.xLightAmbient = level.ActiveLight.Ambient;
            SharedEffectParameters.xLightDiffuse = level.ActiveLight.Diffuse;
            SharedEffectParameters.xLightSpecular = level.ActiveLight.Specular;
            SharedEffectParameters.xShadowsEnabled = shadowsEnabled;
            SharedEffectParameters.xSkyColor = level.SkyColor;

            SharedEffectParameters.xFogStart = level.FogStart;
            SharedEffectParameters.xFogRange = level.FogRange;

            meshRenderer.UpdateEffectVariables();
            billboardRenderer.UpdateEffectVariables();
            orbRenderer.UpdateEffectVariables();
        }

        public override void Draw(GameTime gameTime)
        {
            BoundingFrustum viewFrustum = camera.ViewFrustum;
            BoundingFrustum lightViewFrustum = level.ActiveLight.ViewFrustum;

            level.SortMeshList(camera.Position, viewFrustum);

            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Draw shadows
            DrawShadows(lightViewFrustum);

            // Draw reflections
            DrawReflections(viewFrustum);

            // Draw main
            DrawMain(viewFrustum);

            // Draw SpriteBatch data
            spriteBatch.Begin();

            if (!postProcessingEffectsEnabled)
            {
                // Draw the frame
                graphicsDevice.SetRenderTarget(null);
                spriteBatch.Draw(mainRT, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            }

            if (displayFPS)
            {
                spriteBatch.DrawString(font, fps.ToString(),
                    new Vector2(screenWidth * 0.05f, screenHeight * 0.05f), Color.Yellow);
            }

            spriteBatch.End();

            CalcFPS(gameTime);

            // If the game is transitioning on or off, fade it out to black.
            if (base.TransitionPosition > 0.0f)
            {
                ScreenManager.FadeBackBufferToBlack(1.0f - TransitionAlpha);
            }
        }

        private void DrawShadows(BoundingFrustum lightViewFrustum)
        {
            if (shadowsEnabled)
            {
                graphicsDevice.SetRenderTarget(level.ActiveLight.ShadowMap);
                graphicsDevice.Clear(Color.Black);

                foreach (Mesh mesh in level.MeshList)
                {
                    if (lightViewFrustum.Intersects(mesh.AABB))
                    {
                        meshRenderer.DrawMeshShadow(mesh, lightViewFrustum);
                    }
                }

                SharedEffectParameters.xShadowMap = level.ActiveLight.ShadowMap;
            }
        }

        private void DrawReflections(BoundingFrustum viewFrustum)
        {
            if (level.WaterHeight != Level.WATER_DISABLED_HEIGHT &&
                level.Water.TransparencyRatio > 0.0f)
            {
                graphicsDevice.SetRenderTarget(reflectionRT);
                graphicsDevice.Clear(Color.Black);

                BoundingFrustum reflViewFrustum = new BoundingFrustum(SharedEffectParameters.xReflectionViewMatrix
                    * SharedEffectParameters.xReflectionProjectionMatrix);

                if (level.Type == LevelType.Outdoor)
                {
                    terrainRenderer.DrawReflection(level.Terrain);
                }

                // Draw mesh reflections
                foreach (Mesh mesh in level.MeshList)
                {
                    if (reflViewFrustum.Intersects(mesh.AABB))
                    {
                        meshRenderer.DrawMeshReflection(mesh, reflViewFrustum, level.WaterHeight);
                    }
                }

                level.Sky.DrawCloudsReflection();
            }
        }

        private void DrawMain(BoundingFrustum viewFrustum)
        {
            graphicsDevice.SetRenderTarget(mainRT);
            graphicsDevice.Clear(clearColor);

            if (level.Type == LevelType.Outdoor)
            {
                terrainRenderer.Draw(level.Terrain);
                terrainRenderer.Draw(level.TerrainPlane);
            }
            else
            {
                surfaceRenderer.DrawSurface(level.Surface);
            }

            foreach (Mesh mesh in level.MeshList)
            {
                if (viewFrustum.Intersects(mesh.AABB))
                {
                    meshRenderer.DrawMesh(mesh, viewFrustum);
                }
            }

            foreach (Billboard billboard in level.BillboardList)
            {
                billboardRenderer.DrawLighting(billboard, camera.Up, camera.Look);
            }

            // Draw grass
            foreach (Billboard billboard in level.GrassBillboards)
            {
                if (camera.ViewFrustum.Intersects(billboard.AABB))
                {
                    billboardRenderer.DrawLightingWithWind(billboard, Vector3.Up, camera.Look);
                }
            }

            level.Sky.DrawClouds();

            if (level.WaterHeight != Level.WATER_DISABLED_HEIGHT)
            {
                waterRenderer.Draw(level.Water, reflectionRT, mainRT);
            }

            orbRenderer.CreateRefractionMap(mainRT);
            foreach (Orb orb in level.OrbList)
            {
                if (viewFrustum.Intersects(orb.BoundingSphere))
                {
                    orbRenderer.DrawWithRefraction(orb);
                }
            }

            // Draw particles
            level.DrawParticleEffects(camera);

            // Draw post processing
            if (postProcessingEffectsEnabled)
            {
                graphicsDevice.RasterizerState = RasterizerState.CullNone;

                graphicsDevice.SetRenderTarget(occlusionRT);
                graphicsDevice.Clear(Color.Black);

                // Draw light into occlusion map
                lightRenderer.Draw(level.ActiveLight, camera.Up, camera.Look);

                // Copy Occlusion data from the Alpha channel of main render target
                postEffects.DrawCopyOcclusion(mainRT);

                graphicsDevice.SetRenderTarget(bloomRT);
                graphicsDevice.Clear(Color.Black);

                // Draw lightscattering into bloom map
                postEffects.DrawLightScattering(occlusionRT);

                // Apply bloom lighting to output the final image
                graphicsDevice.SetRenderTarget(null);
                postEffects.ApplyBloom(mainRT, bloomRT);
            }
        }

        private int fps;
        private int frames;
        private float timer;
        private void CalcFPS(GameTime gameTime)
        {
            frames++;

            if (gameTime.TotalGameTime.TotalSeconds - timer >= 1.0f)
            {
                fps = frames;
                frames = 0;
                timer = (float)gameTime.TotalGameTime.TotalSeconds;
            }
        }
    }
}
