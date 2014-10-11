using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Graphics3D;
using GameObjects;

namespace GameStateManagement
{
    class DayNightCycleDemo : GameScreen
    {
        GraphicsDevice graphicsDevice;
        ContentManager content;
        SpriteBatch spriteBatch;

        SpriteFont font;
        Color clearColor;

        int screenWidth;
        int screenHeight;

        bool shadowsEnabled;
        bool postProcessingEffectsEnabled;
        bool displayFPS;
        bool hudEnabled;

        RenderTarget2D mainRT;
        RenderTarget2D reflectionRT;
        RenderTarget2D occlusionRT;
        RenderTarget2D bloomRT;
        PostProcessingEffects postEffects;

        // Renderers
        LightRenderer lightRenderer;
        TerrainRenderer terrainRenderer;
        SurfaceRenderer surfaceRenderer;
        WaterRenderer waterRenderer;
        BillboardRenderer billboardRenderer;
        MeshRenderer meshRenderer;
        OrbRenderer orbRenderer;

        FirstPersonCamera camera;
        float cameraMoveSpeed;
        float cameraSwimSpeed;

        bool drawSlidingText;
        float drawSlidingTextTimer;
        string slidingText = "Rock Climbing Skill Not Unlocked!";

        float swimResetCameraTimer;
        bool drawSwimResetCameraText;
        float drawSwimResetCameraTextTimer;
        const string swimResetCameraText = "Swim Skill Not Unlocked!";

        float slideSpeed;

        bool isSliding;
        bool isSwimming;
        bool isJumping;
        bool jumpCanceled;

        float jumpStrength;
        float maxJumpStrength;
        bool jumpButtonPressed;
        float jumpTimer;
        float jumpAmount;
        Vector3 jumpVelocity;

        UnboundLevel level;

        FootstepSFXPlayer footstepPlayer;
        FootstepSFXPlayer swimmingFootstepPlayer;
        SoundEffect jumpLandingSFX;
        SoundEffect jumpSplashSFX;
        SoundEffectInstance jumpWindSFXInstance;

        KeyboardState keyboardState;
        KeyboardState prevKeyboardState;

        GamePadState gamePadState;
        GamePadState prevGamePadState;

        Random rand;
        float pauseAlpha;

        public DayNightCycleDemo()
        {
            rand = new Random();
        }

        public override void LoadContent()
        {
            Initialize();

            ScreenManager.Game.ResetElapsedTime();
        }

        public override void UnloadContent()
        {
            // Stop music and sound effects
            MediaPlayer.Stop();
            level.AmbientSFX.Stop();
            level.WeatherSFX.Stop();
            level.WaterSFX.Stop();
            level.StopAudio();
        }

        private void Initialize()
        {
            graphicsDevice = ScreenManager.GraphicsDevice;
            content = ScreenManager.Content;

            spriteBatch = new SpriteBatch(graphicsDevice);
            MeshManager.InitializeManager(graphicsDevice, content);
            font = content.Load<SpriteFont>("font");

            screenWidth = graphicsDevice.Viewport.Width;
            screenHeight = graphicsDevice.Viewport.Height;

            // Zero out all channels for post processing
            clearColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            // Create Render Targets
            mainRT = new RenderTarget2D(graphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            reflectionRT = new RenderTarget2D(graphicsDevice, screenWidth / 4, screenHeight / 4, true, SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8);
            occlusionRT = new RenderTarget2D(graphicsDevice, screenWidth / 4, screenHeight / 4, false, SurfaceFormat.Color,
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

            shadowsEnabled = true;
            postProcessingEffectsEnabled = true;
            displayFPS = true;
            hudEnabled = true;

            LoadLevel(@"Levels\TestUnboundLevel.lvl");
        }

        private void LoadLevel(string levelFileName)
        {
            // Load level file
            level = new UnboundLevel(graphicsDevice, content, levelFileName);
            level.DayNightCycleSpeed = 0.1f;
            level.MaxParticles = 600;

            camera = new FirstPersonCamera(level.CameraStartPosition, level.CameraStartDirection);
            camera.FreeFlyEnabled = false;
            camera.DrawDistance = level.FogStart + level.FogRange;
            camera.AABBSize = Vector2.One;
            camera.MoveSpeed = cameraMoveSpeed;
            camera.InvertY = true;
            camera.PitchMinDegrees = -75.0f;
            camera.PitchMaxDegrees = 60.0f;
            camera.AspectRatio = (float)(screenWidth) / (float)(screenHeight);
            camera.Projection = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 4.0f,
                camera.AspectRatio, 0.1f, 10000.0f);

            // Create sound players
            MediaPlayer.Play(level.BackgroundMusic);
            MediaPlayer.IsRepeating = true;
            level.AmbientSFX.Play();
            level.WeatherSFX.Play();
            level.WaterSFX.Play();

            jumpLandingSFX = content.Load<SoundEffect>(@"SoundEffects\jumpLanding");
            jumpSplashSFX = content.Load<SoundEffect>(@"SoundEffects\splash");
            jumpWindSFXInstance = content.Load<SoundEffect>(@"SoundEffects\wind_rush_SFX").CreateInstance();
            jumpWindSFXInstance.IsLooped = true;
            jumpWindSFXInstance.Volume = 0.0f;
            jumpWindSFXInstance.Play();

            footstepPlayer = new FootstepSFXPlayer(
                new SoundEffect[] { level.FootstepSFX1, level.FootstepSFX2 },
                1.25f, 0.5f);
            swimmingFootstepPlayer = new FootstepSFXPlayer(
                new SoundEffect[] { content.Load<SoundEffect>(@"SoundEffects\swim1"), 
                                    content.Load<SoundEffect>(@"SoundEffects\swim2") },
                2.0f, 0.25f);

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

            // Gradually fade in or out depending on whether we are covered by the pause screen.
            if (coveredByOtherScreen)
                pauseAlpha = Math.Min(pauseAlpha + 1f / 32, 1);
            else
                pauseAlpha = Math.Max(pauseAlpha - 1f / 32, 0);

            SoundEffect.MasterVolume = 1.0f - (pauseAlpha * 0.5f);

            if (IsActive)
            {
                UpdateGame(gameTime);

                // Update medals
                ActivePlayer.Profile.UpdateStatsAndMedals();

                // Reset music volume
                MediaPlayer.Volume = ActivePlayer.Profile.MusicEnabled ? ActivePlayer.Profile.MusicVolume : 0.0f;
            }
            else
            {
                // Ensure Xbox controller is not rumbling
                GamePad.SetVibration(ActivePlayer.PlayerIndex, 0.0f, 0.0f);

                // Decrease music volume
                MediaPlayer.Volume = ActivePlayer.Profile.MusicEnabled ? ActivePlayer.Profile.MusicVolume / 2 : 0.0f;
            }
        }

        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile
            int playerIndex = (int)ActivePlayer.PlayerIndex;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
        }

        private void UpdateGame(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            maxJumpStrength = ActivePlayer.Profile.CurrentJumpStrength();
            cameraMoveSpeed = camera.FreeFlyEnabled ? 350.0f : ActivePlayer.Profile.CurrentMoveSpeed();
            cameraSwimSpeed = cameraMoveSpeed / 2.0f;

            prevKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();

            prevGamePadState = gamePadState;
            gamePadState = GamePad.GetState(PlayerIndex.One);

            // Enable/Disable day night cycle
            if (keyboardState.IsKeyDown(Keys.Enter) && prevKeyboardState.IsKeyUp(Keys.Enter) ||
                gamePadState.IsButtonDown(Buttons.Back) && prevGamePadState.IsButtonUp(Buttons.Back))
            {
                level.RunDayNightCycle = !level.RunDayNightCycle;
            }

            // Enabled/Disable rain
            if (keyboardState.IsKeyDown(Keys.LeftShift) && prevKeyboardState.IsKeyUp(Keys.LeftShift) ||
                gamePadState.IsButtonDown(Buttons.X) && prevGamePadState.IsButtonUp(Buttons.X))
            {
                level.RainEnabled = !level.RainEnabled;
            }

            // Toggle free fly camera
            if ((keyboardState.IsKeyDown(Keys.Tab) && prevKeyboardState.IsKeyUp(Keys.Tab)) ||
                (gamePadState.IsButtonDown(Buttons.Y) && prevGamePadState.IsButtonUp(Buttons.Y)))
            {
                camera.FreeFlyEnabled = !camera.FreeFlyEnabled;
            }

            // Update camera
            if (isJumping)
            {
                camera.MoveSpeed = cameraMoveSpeed * 0.35f;
            }
            else if (isSwimming)
            {
                if (!ActivePlayer.Profile.IsSwimSkillUnlocked())
                {
                    swimResetCameraTimer += dt;
                    if (swimResetCameraTimer >= 1.5f)
                    {
                        drawSwimResetCameraText = true;

                        // Reset player camera
                        camera.Position = level.CameraStartPosition;
                        camera.Look = level.CameraStartDirection;
                        camera.MoveSpeed = cameraMoveSpeed;

                        isSwimming = false;
                        swimResetCameraTimer = 0.0f;
                    }
                    camera.MoveSpeed = 0.0f; // Player cannot move. Camera is being reset.
                }
                else
                {
                    camera.MoveSpeed = cameraSwimSpeed;
                }
            }
            else if (isSliding)
            {
                camera.MoveSpeed = 0.0f; // Player cannot move while sliding
                drawSlidingText = true;
            }
            else
            {
                camera.MoveSpeed = cameraMoveSpeed;
            }

            Vector3 oldCamPos = camera.Position;
            camera.Update(dt);

            // Start jump
            if (!isJumping && !jumpButtonPressed && !isSliding && !(isSwimming && !ActivePlayer.Profile.IsSwimSkillUnlocked()) &&
                ((keyboardState.IsKeyDown(Keys.Space) && prevKeyboardState.IsKeyUp(Keys.Space)) ||
                (gamePadState.IsButtonDown(Buttons.A) && prevGamePadState.IsButtonUp(Buttons.A))))
            {
                jumpButtonPressed = true;
            }

            // Increase jump strength on button hold time
            if (jumpButtonPressed &&
                (keyboardState.IsKeyDown(Keys.Space) ||
                gamePadState.IsButtonDown(Buttons.A)))
            {
                jumpTimer += dt;
            }   

            // Jump button is released
            float maxJumpTime = 0.15f;
            if (jumpTimer > maxJumpTime ||
                (jumpButtonPressed &&
                ((prevKeyboardState.IsKeyDown(Keys.Space) && keyboardState.IsKeyUp(Keys.Space)) ||
                (prevGamePadState.IsButtonDown(Buttons.A) && gamePadState.IsButtonUp(Buttons.A)))))
            {
                // Calculate jump strength
                jumpStrength = maxJumpStrength * (jumpTimer / maxJumpTime);
                jumpTimer = 0.0f;
                jumpButtonPressed = false;
            }

            if (!isJumping && jumpStrength > 0)
            {
                if (float.IsNaN(camera.MoveDirection.X) || float.IsNaN(camera.MoveDirection.Y) ||
                    float.IsNaN(camera.MoveDirection.Z))
                {
                    jumpVelocity = Vector3.Zero;
                }
                else
                {
                    jumpVelocity = camera.MoveDirection;
                }

                jumpVelocity.Y = jumpStrength;              // Jump strength
                jumpAmount = cameraMoveSpeed;

                isJumping = true;
                isSwimming = false;
                jumpCanceled = false;
            }

            // Cancel jump
            if (!jumpCanceled && isJumping &&
                (keyboardState.IsKeyDown(Keys.LeftControl) || 
                 gamePadState.IsButtonDown(Buttons.B)))
            {
                jumpVelocity.Y = -maxJumpStrength;
                jumpCanceled = true;
            }
           
            if (!camera.FreeFlyEnabled)
            {
                float waterHeight = level.WaterHeight + level.CameraHeightOffset / 2;
                float terrainHeight = level.Terrain.GetHeight(camera.Position.X, camera.Position.Z)
                    + level.CameraHeightOffset;

                if (isJumping)
                {
                    Vector3 velocity = jumpVelocity;
                    velocity.X *= jumpAmount;
                    velocity.Z *= jumpAmount;
                    camera.Position += velocity * dt;

                    jumpVelocity.Y -= 100 * dt;
                    if (jumpWindSFXInstance.Volume + dt <= 1.0f) jumpWindSFXInstance.Volume += dt * 0.025f;

                    if (camera.Position.Y <= waterHeight)
                    {
                        isJumping = false;
                        isSwimming = true;
                        jumpStrength = 0;

                        // Play landing SFX
                        jumpSplashSFX.Play();
                    }
                    else if (camera.Position.Y <= terrainHeight)
                    {
                        isJumping = false;
                        isSwimming = false;
                        jumpStrength = 0;

                        // Play landing SFX
                        jumpLandingSFX.Play();
                    }
                }
                else
                {
                    // Slide check
                    if (!ActivePlayer.Profile.IsRockClimbingUnlocked() &&
                        level.Terrain.IsSteepIncline(camera.Position.X, camera.Position.Z, 0.6f))
                    {
                        if (!level.Terrain.IsSteepIncline(oldCamPos.X, oldCamPos.Z, 0.6f) &&
                           camera.Position.Y > oldCamPos.Y)
                        {
                            // Player cannot walk up steep terrain (Rock Climbing)
                            camera.Position = oldCamPos;
                            drawSlidingText = true;
                        }
                        else
                        {
                            slideSpeed += dt;
                            if (slideSpeed > 2.0f) slideSpeed = 2.0f;

                            // Slide camera position along cliff
                            camera.Position = camera.Position + 
                                level.Terrain.GetSlopeDirection(camera.Position.X, camera.Position.Z) * slideSpeed;

                            isSliding = true;
                        }

                    }
                    else
                    {
                        jumpVelocity = Vector3.Zero;
                        jumpWindSFXInstance.Volume = 0.0f;
                        slideSpeed = 0.0f;
                        isSliding = false;

                        Vector3 pos = camera.Position;
                        pos.Y = terrainHeight > waterHeight ? terrainHeight : waterHeight;

                        if (pos.Y <= waterHeight)
                        {
                            isSwimming = true;
                            pos.Y += (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * level.Water.WaveHeight) * 1.5f;
                        }
                        else
                        {
                            isSwimming = false;
                        }

                        camera.Position = pos;
                    }
                }
            }

            // Update the level
            level.Update(camera, dt);

            if (!isJumping)
            {
                if (isSwimming)
                {
                    swimmingFootstepPlayer.Play(oldCamPos, camera.Position);
                }
                else if(!isSliding)
                {
                    footstepPlayer.Play(oldCamPos, camera.Position);
                }
            }
            

            // Update effect variables
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

        int numGrassBillboardsDrawn;
        public override void Draw(GameTime gameTime)
        {
            BoundingFrustum viewFrustum = camera.ViewFrustum;
            BoundingFrustum occlusionViewFrustum = new BoundingFrustum(camera.View * camera.Projection);
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

            if (IsActive && hudEnabled)
            {
                if (displayFPS)
                {
                    spriteBatch.DrawString(font, fps.ToString(),
                        new Vector2(screenWidth * 0.05f, screenHeight * 0.05f), Color.Yellow);
                }

                spriteBatch.DrawString(font, "Rain Particles: " + level.RainParticleEmitter.NumActiveParticles,
                        new Vector2(screenWidth * 0.05f, screenHeight * 0.1f), Color.Yellow);

                spriteBatch.DrawString(font, "Grass Billboards: " + numGrassBillboardsDrawn + "/" + level.GrassBillboards.Count,
                        new Vector2(screenWidth * 0.05f, screenHeight * 0.15f), Color.Yellow);

                // Print orb information
                spriteBatch.DrawString(font, "Orbs Collected: " + level.NumOrbsCollected() + "/" + level.TotalOrbs(),
                    new Vector2(screenWidth * 0.75f, screenHeight * 0.05f), Color.Yellow);
                spriteBatch.DrawString(font, "Secret Orbs Collected: " + level.NumSecretOrbsCollected() + "/" + level.TotalSecretOrbs(),
                    new Vector2(screenWidth * 0.75f, screenHeight * 0.1f), Color.Yellow);
                spriteBatch.DrawString(font, "Agility Orbs Collected: " + level.NumAgilityOrbsCollected() + "/" + level.TotalAgilityOrbs(),
                    new Vector2(screenWidth * 0.75f, screenHeight * 0.15f), Color.Yellow);
                spriteBatch.DrawString(font, "Health Orbs Collected: " + level.NumHealthOrbsCollected() + "/" + level.TotalHealthOrbs(),
                    new Vector2(screenWidth * 0.75f, screenHeight * 0.2f), Color.Yellow);

                if (drawSwimResetCameraText && drawSwimResetCameraTextTimer < 2.0f)
                {
                    drawSwimResetCameraTextTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    Vector2 swimTextSize = font.MeasureString(swimResetCameraText);
                    Vector2 swimTextPos = new Vector2(screenWidth * 0.5f - swimTextSize.X * 0.5f,
                                                      screenHeight * 0.25f - swimTextSize.Y * 0.5f);
                    spriteBatch.DrawString(font, swimResetCameraText, swimTextPos, Color.White);
                }
                else
                {
                    drawSwimResetCameraTextTimer = 0.0f;
                    drawSwimResetCameraText = false;
                }

                if (drawSlidingText && drawSlidingTextTimer < 2.0f)
                {
                    drawSlidingTextTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    Vector2 slidingTextSize = font.MeasureString(slidingText);
                    Vector2 slidingTextPos = new Vector2(screenWidth * 0.5f - slidingTextSize.X * 0.5f,
                                                      screenHeight * 0.25f - slidingTextSize.Y * 0.5f);
                    spriteBatch.DrawString(font, slidingText, slidingTextPos, Color.White);
                }
                else
                {
                    drawSlidingTextTimer = 0.0f;
                    drawSlidingText = false;
                }
            }

            spriteBatch.End();

            CalcFPS(gameTime);

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0 || pauseAlpha > 0)
            {
                float alpha = MathHelper.Lerp(1f - TransitionAlpha, 1f, pauseAlpha / 2);

                ScreenManager.FadeBackBufferToBlack(alpha);
            }

            base.Draw(gameTime);
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

                foreach (Orb orb in level.OrbList)
                {
                    if (orb.IsAlive && lightViewFrustum.Intersects(orb.BoundingSphere))
                    {
                        orbRenderer.DrawShadow(orb);
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

                BoundingFrustum reflViewFrustum = new BoundingFrustum(SharedEffectParameters.xReflectionViewMatrix *
                    SharedEffectParameters.xReflectionProjectionMatrix);

                // Draw terrain reflections
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

            // Draw Terrain/Surface
            if (level.Type == LevelType.Outdoor)
            {
                terrainRenderer.Draw(level.Terrain);
                terrainRenderer.Draw(level.TerrainPlane);
            }
            else
            {
                surfaceRenderer.DrawSurface(level.Surface);
            }

            // Draw Meshes
            RasterizerState rs = graphicsDevice.RasterizerState;
            foreach (Mesh mesh in level.MeshList)
            {
                if (viewFrustum.Intersects(mesh.AABB))
                {
                    meshRenderer.DrawMesh(mesh, viewFrustum);
                }
            }
            graphicsDevice.RasterizerState = rs;

            // Draw billboards
            foreach (Billboard billboard in level.BillboardList)
            {
                if (camera.ViewFrustum.Intersects(billboard.AABB))
                {
                    billboardRenderer.DrawLighting(billboard, Vector3.Up, camera.Look);
                }
            }

            // Draw grass
            numGrassBillboardsDrawn = 0;
            foreach (Billboard billboard in level.GrassBillboards)
            {
                if (camera.ViewFrustum.Intersects(billboard.AABB))
                {
                    billboardRenderer.DrawLightingWithWind(billboard, Vector3.Up, camera.Look);
                    numGrassBillboardsDrawn++;
                }
            }

            // Draw sky box
            level.Sky.DrawClouds();

            // Draw water
            if (level.WaterHeight != Level.WATER_DISABLED_HEIGHT)
            {
                waterRenderer.Draw(level.Water, reflectionRT, mainRT);
            }

            // Draw orbs
            orbRenderer.CreateRefractionMap(mainRT);
            foreach (Orb orb in level.OrbList)
            {
                if (orb.IsAlive && viewFrustum.Intersects(orb.BoundingSphere))
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
