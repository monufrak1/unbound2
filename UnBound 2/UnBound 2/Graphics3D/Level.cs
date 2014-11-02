using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Collections;

namespace Graphics3D
{
    enum LevelType
    {
        Outdoor,
        Indoor
    }

    class Level
    {
        protected GraphicsDevice device;
        protected ContentManager content;

        // Level data
        protected string title;
        protected LevelType type;

        protected Song backgroundMusic;
        protected SoundEffect ambientSFX;
        protected SoundEffectInstance ambientSFXInstance;
        protected SoundEffect footstepSFX1;
        protected SoundEffect footstepSFX2;
        protected SoundEffectInstance weatherSFXInstance;
        protected SoundEffectInstance waterSFXInstance;

        protected Vector3 cameraStartPosition;
        protected Vector3 cameraStartDirection;
        protected float cameraHeightOffset;

        protected Vector4 originalSkyColor;
        protected Vector4 skyColor;
        protected Vector4 weatherSkyColor;
        protected float weatherLerpAmount;
        protected float darkSkyOffset;

        protected bool runDayNightCycle;
        protected bool daytimeTransition;
        protected Vector3 originalLightDirection;
        protected Vector3 lightRotationAxis;
        protected float lightRotationAngle;
        protected float dayNightCycleSpeed;
        protected float maxLightRotationDegrees;

        protected float originalFogStart;
        protected float originalFogRange;
        protected float fogStart;
        protected float fogRange;

        public static float WATER_DISABLED_HEIGHT = -10000.0f;
        protected float waterHeight;
        protected Vector4 waterColor;
        protected float deepWaterFogDistance;

        protected Light[] lights;
        protected Light activeLight;
        protected TexturedSkyDome sky;
        protected TerrainGrid terrain;
        protected TerrainGrid terrainPlane;
        protected SurfacePlane surface;
        protected WaterGrid water;
        protected List<Billboard> billboardList;
        protected List<Mesh> meshList;

        // Grass
        protected int numGrassBillboards;
        protected float grassRadius;
        protected Billboard grassBillboard;
        protected List<Billboard> grassBillboards;
        private Dictionary<Billboard, float> grassToCameraDistanceMap;

        // Weather particles effects
        protected int numRainParticlesToEmit;
        protected int maxParticles;
        protected bool rainEnabled;
        protected float rainInterval;
        protected float rainIntervalTimer;
        protected float rainTimer;
        protected float rainDelta;
        protected ParticleEmitter rainParticleEmitter;

        // Ambient particle effects
        protected bool emitAmbientParticles;
        protected float ambientParticleTimer;
        protected float ambientParticleTimerDelta;
        protected ParticleEmitter ambientParticleEmitter1;
        protected ParticleEmitter ambientParticleEmitter2;
        protected ParticleEmitter ambientParticleEmitter3;

        Random rand;

        public Level(GraphicsDevice device, ContentManager content, string levelFileName)
        {
            this.device = device;
            this.content = content;

            rand = new Random();

            meshList = new List<Mesh>();
            billboardList = new List<Billboard>();

            LoadLevel(levelFileName);

            // Set up light
            activeLight = new Light(lights[0]);
            if (lights.Count() >= 4)
            {
                runDayNightCycle = true;
                originalLightDirection = lights[1].Direction;  // Noon light direction
                lightRotationAxis = Vector3.Normalize(new Vector3(originalLightDirection.X, 0.0f,
                    originalLightDirection.Z));

                daytimeTransition = true;
                maxLightRotationDegrees = 120.0f;
                float startAngle = maxLightRotationDegrees;
                dayNightCycleSpeed = 0.25f;
                lightRotationAngle = MathHelper.ToRadians(startAngle);
            }
            else
            {
                runDayNightCycle = false;
            }

            skyColor = originalSkyColor;
            weatherSkyColor = Vector4.One;
            weatherLerpAmount = 0.0f;

            fogStart = originalFogStart;
            fogRange = originalFogRange;

            // Create weather particles
            maxParticles = 400;
            rainInterval = 30.0f;
            rainDelta = 5.0f;
            rainTimer = rainDelta;
            rainParticleEmitter = new ParticleEmitter(device, content, Vector3.Zero,
                content.Load<Texture2D>(@"Textures\rain_drop"), new RainParticleUpdater(),
                maxParticles, 1.25f);

            // Create ambient particles
            ambientParticleEmitter1 = new ParticleEmitter(device, content, Vector3.Zero,
                content.Load<Texture2D>(@"Textures\ambient_particle_blue"), new AmbientParticleUpdater(),
                100, 3.0f);
            ambientParticleEmitter2 = new ParticleEmitter(device, content, Vector3.Zero,
                content.Load<Texture2D>(@"Textures\ambient_particle_pink"), new AmbientParticleUpdater(),
                100, 3.0f);
            ambientParticleEmitter3 = new ParticleEmitter(device, content, Vector3.Zero,
                content.Load<Texture2D>(@"Textures\ambient_particle_green"), new AmbientParticleUpdater(),
                100, 3.0f);

            ambientParticleTimerDelta = 0.1f;

            // Generate initial grass
            numGrassBillboards = grassBillboard != null ? 200 : 0;
            grassRadius = 300.0f;
            grassBillboards = new List<Billboard>(numGrassBillboards);
            grassToCameraDistanceMap = new Dictionary<Billboard, float>();
            if (type == LevelType.Outdoor)
            {
                for (int i = 0; i < numGrassBillboards; i++)
                {
                    // Add the billboard clone from original billboard
                    Vector2 size = grassBillboard.Size * Math.Max(0.5f, (float)rand.NextDouble());
                    Billboard newBillboard = new Billboard(device, Vector3.Zero, size, grassBillboard.BillboardTexture);
                    grassBillboards.Add(newBillboard);
                    grassToCameraDistanceMap.Add(newBillboard, 0.0f);
                }
            }
        }

        private void LoadLevel(string levelFileName)
        {
            // Load the data from the file
            StreamReader inFile = new StreamReader(levelFileName);

            while (!inFile.EndOfStream)
            {
                string line = inFile.ReadLine();
                string command = line.Split(' ')[0];
                string section = (command == "#") ? line.Split(' ')[1] : "";

                // HEADER
                if (command == "LEVEL_TITLE")
                {
                    title = line.Substring(command.Length + 1, line.Length - (command.Length + 1)); 
                }
                else if (command == "LEVEL_TYPE")
                {
                    string levelType = line.Split(' ')[1];
                    if (levelType == "INDOOR")
                    {
                        type = LevelType.Indoor;
                    }
                    else if (levelType == "OUTDOOR")
                    {
                        type = LevelType.Outdoor;
                    }
                }

                // SOUND EFFECTS
                else if (command == "BACKGROUND_MUSIC")
                {
                    string musicFileName = line.Remove(0, ("BACKGROUND_MUSIC").Length + 1);
                    musicFileName = musicFileName.Remove(musicFileName.IndexOf('.')); // Remove file extension XNA

                    backgroundMusic = content.Load<Song>(@"Music\" + musicFileName);
                }
                else if (command == "AMBIENT_SFX")
                {
                    string ambientFileName = line.Split(' ')[1];
                    ambientFileName = ambientFileName.Remove(ambientFileName.IndexOf('.')); // Remove file extension XNA

                    float volume = float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture);
                    float pitch = float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture);

                    ambientSFX = content.Load<SoundEffect>(@"SoundEffects\" + ambientFileName);
                    ambientSFXInstance = ambientSFX.CreateInstance();
                    ambientSFXInstance.IsLooped = true;
                    ambientSFXInstance.Volume = volume;
                    ambientSFXInstance.Pitch = pitch;

                    // Check for newer file version
                    if (line.Split(' ').Length == 6)
                    {
                        weatherSFXInstance = content.Load<SoundEffect>(@"SoundEffects\" + line.Split(' ')[4]).CreateInstance();
                        weatherSFXInstance.IsLooped = true;
                        weatherSFXInstance.Volume = 0.0f;

                        waterSFXInstance = content.Load<SoundEffect>(@"SoundEffects\" + line.Split(' ')[5]).CreateInstance();
                        waterSFXInstance.IsLooped = true;
                        waterSFXInstance.Volume = 0.0f;
                    }
                }
                else if (command == "FOOTSTEP_SFXs")
                {
                    string fileName1 = line.Split(' ')[1];
                    string fileName2 = line.Split(' ')[2];

                    fileName1 = fileName1.Remove(fileName1.IndexOf('.')); // Remove file extensions XNA
                    fileName2 = fileName2.Remove(fileName2.IndexOf('.'));

                    footstepSFX1 = content.Load<SoundEffect>(@"SoundEffects\" + fileName1);
                    footstepSFX2 = content.Load<SoundEffect>(@"SoundEffects\" + fileName2);
                }

                // CAMERA
                else if (command == "CAMERA_START_POSITION")
                {
                    cameraStartPosition = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                    float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                    float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));
                }
                else if (command == "CAMERA_START_DIRECTION")
                {
                    cameraStartDirection = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));
                }
                else if (command == "CAMERA_HEIGHT_OFFSET")
                {
                    cameraHeightOffset = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);
                }

                // LIGHTING
                else if (command == "NUM_LIGHTS")
                {
                    int numLights = int.Parse(line.Split(' ')[1]);
                    lights = new Light[numLights];

                    // Create lights
                    for (int i = 0; i < numLights; i++)
                    {
                        // Light type
                        line = inFile.ReadLine();
                        LightType type = LightType.Directional;     // Directional light by default
                        string lightTypeStr = line.Split(' ')[1];
                        if (lightTypeStr == "DIRECTIONAL")
                        {
                            type = LightType.Directional;
                        }
                        else if (lightTypeStr == "POINT")
                        {
                            type = LightType.Point;
                        }
                        else if (lightTypeStr == "SPOTLIGHT")
                        {
                            type = LightType.Spotlight;
                        }

                        // Light position
                        line = inFile.ReadLine();
                        Vector3 position = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                        // Light direction
                        line = inFile.ReadLine();
                        Vector3 direction = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                             float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                        // Ambient
                        line = inFile.ReadLine();
                        Vector4 ambient = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                        // Diffuse
                        line = inFile.ReadLine();
                        Vector4 diffuse = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                        // Specular
                        line = inFile.ReadLine();
                        Vector4 specular = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                        // Size
                        line = inFile.ReadLine();
                        float lightSize = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                        // Create the light
                        lights[i] = new Light(device,
                            content.Load<Texture2D>(@"Textures\sunFlare"),
                            position,
                            direction,
                            ambient,
                            diffuse,
                            specular,
                            lightSize,
                            type);

                        inFile.ReadLine();
                    }
                }
                    
                // SKY and FOG
                if (section == "SKY_FOG")
                {
                    string skyTexture;
                    float skyTextureScale;

                    line = inFile.ReadLine();
                    skyTexture = (line.Split(' ')[1]).Split('.')[0];   // Remove file extension (XNA)

                    line = inFile.ReadLine();
                    skyTextureScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    originalSkyColor = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                    line = inFile.ReadLine();
                    darkSkyOffset = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    originalFogStart = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    originalFogRange = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create sky
                    sky = new TexturedSkyDome(device,
                        content.Load<Effect>(@"Effects\SkyDome"),
                        content.Load<Texture2D>(@"Textures\" + skyTexture),
                        skyTextureScale,
                        8000.0f);
                }

                // WATER
                if (section == "WATER")
                {
                    line = inFile.ReadLine();
                    waterHeight = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    waterColor = new Vector4(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture),
                                                      float.Parse(line.Split(' ')[4], CultureInfo.InvariantCulture));

                    line = inFile.ReadLine();
                    string waterNormalTexture = line.Split(' ')[1].Split('.')[0];  // Remove file extension (XNA)

                    line = inFile.ReadLine();
                    float texScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float ratio = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    deepWaterFogDistance = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float reflectionAmount = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float refractionAmount = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float waveHeight = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float waveSpeed = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create water
                    water = new WaterGrid(device,
                        content.Load<Texture2D>(@"Textures\" + waterNormalTexture),
                        texScale, ratio, reflectionAmount, refractionAmount,
                        waveHeight, waveSpeed);
                }

                // TERRAIN
                if (section == "TERRAIN")
                {
                    line = inFile.ReadLine();
                    string heightMapFileName = line.Split(' ')[1];

                    line = inFile.ReadLine();
                    string lowTex = line.Split(' ')[1].Split('.')[0];

                    line = inFile.ReadLine();
                    string highTex = line.Split(' ')[1].Split('.')[0];   // Remove file extension (XNA)

                    line = inFile.ReadLine();
                    float texScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float spacing = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    line = inFile.ReadLine();
                    float heightScale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create terrain
                    terrain = new TerrainFromHeightMap(device,
                        content.Load<Texture2D>(@"Textures\" + lowTex),
                        content.Load<Texture2D>(@"Textures\" + highTex),
                        @"Terrain\" + heightMapFileName,
                        texScale,
                        spacing,
                        heightScale);

                    // Create terrain plane
                    terrainPlane = new TerrainPlane(device,
                        content.Load<Texture2D>(@"Textures\" + lowTex),
                        content.Load<Texture2D>(@"Textures\" + highTex),
                        texScale);
                }

                // SURFACE
                if (section == "SURFACE")
                {
                    // Surface height
                    line = inFile.ReadLine();
                    float surfaceHeight = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Diffuse
                    line = inFile.ReadLine();
                    string diffTexFileName = line.Split(' ')[1];
                    diffTexFileName = diffTexFileName.Remove(diffTexFileName.IndexOf('.')); // Remove file extension XNA

                    // Specular
                    line = inFile.ReadLine();
                    string specTexFileName = line.Split(' ')[1];
                    specTexFileName = specTexFileName.Remove(specTexFileName.IndexOf('.')); // Remove file extension XNA

                    // Normal
                    line = inFile.ReadLine();
                    string normTexFileName = line.Split(' ')[1];
                    normTexFileName = normTexFileName.Remove(normTexFileName.IndexOf('.')); // Remove file extension XNA

                    // Texture Scale
                    line = inFile.ReadLine();
                    float scale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    // Create surface
                    surface = new SurfacePlane(device,
                        surfaceHeight,
                        content.Load<Texture2D>(@"Textures\" + diffTexFileName),
                        content.Load<Texture2D>(@"Textures\" + specTexFileName),
                        content.Load<Texture2D>(@"Textures\" + normTexFileName),
                        scale);
                }

                // Billboard
                if (command == "NEW_BILLBOARD")
                {
                    bool isGrassBillboard = line.Split(' ').Length > 1 && line.Split(' ')[1] == "GRASS_BILLBOARD" ?
                        true : false;

                    // Get the texture name
                    line = inFile.ReadLine();
                    string billboardTex = line.Split(' ')[1];
 
                    // Get the position
                    line = inFile.ReadLine();
                    Vector3 position = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                               float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                               float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                    // Get the size
                    line = inFile.ReadLine();
                    Vector2 size = new Vector2(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture));

                    // Load the billboard
                    Billboard newBillboard = new Billboard(device, position, size,
                        content.Load<Texture2D>(@"Textures\" + billboardTex));

                    if (isGrassBillboard)
                    {
                        // Set the grass billboard
                        grassBillboard = newBillboard;
                    }
                    else
                    {
                        // Add the billboard to the list
                        billboardList.Add(newBillboard);
                    }
                }

                // MESHES
                if (command == "MESH_NAME")
                {
                    // Preload the Mesh
                    MeshManager.LoadMesh(line.Split(' ')[1]);
                }
                else if (command == "NEW_MESH")
                {
                    // Get new mesh copy from the manager
                    Mesh newMesh = MeshManager.LoadMesh(line.Split(' ')[1]);

                    // POSITION
                    line = inFile.ReadLine();
                    Vector3 position = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                    // ROTATION
                    line = inFile.ReadLine();
                    Vector3 rotation = new Vector3(float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[2], CultureInfo.InvariantCulture),
                                                   float.Parse(line.Split(' ')[3], CultureInfo.InvariantCulture));

                    // SCALE
                    line = inFile.ReadLine();
                    float scale = float.Parse(line.Split(' ')[1], CultureInfo.InvariantCulture);

                    newMesh.Position = position;
                    newMesh.RotationAngles = rotation;
                    newMesh.Scale = scale;

                    // Add the mesh to the list
                    meshList.Add(newMesh);
                }
            }

            inFile.Close();
        }

        public void SortMeshList(Vector3 cameraPosition, BoundingFrustum viewFrustum)
        {
            // Sort meshes in a front to back order from the camera
            IComparer<Mesh> comp = new MeshDistanceComparer(cameraPosition, viewFrustum);
            MeshList.Sort(comp);
        }

        public void Update(FirstPersonCamera camera, float dt)
        {
            UpdateGrassBillboards(camera);
            UpdateLightingAndSkyColor(camera, dt);
            UpdateWeatherEffects(camera, dt);
            UpdateAmbientParticles(camera, dt);

            // Update ambient SFX
            weatherSFXInstance.Volume = (float)rainParticleEmitter.NumActiveParticles/(float)maxParticles;
            waterSFXInstance.Volume = Math.Min(Math.Abs(waterHeight / camera.Position.Y), 1.0f);
        }

        private void UpdateGrassBillboards(FirstPersonCamera camera)
        {
            foreach (Billboard b in grassBillboards)
            {
                float dist = Vector3.Distance(b.Position, camera.Position);

                Vector3 pos = b.Position;
                Vector2 size = grassBillboard.Size;

                // Reorganize grass around camera
                if (!camera.ViewFrustum.Intersects(b.AABB) || dist > grassRadius)
                {
                    Vector3 randVec = new Vector3((float)rand.NextDouble() - 0.5f,
                        (float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f);
                    randVec *= grassRadius;

                    pos = camera.Position + randVec;
                    dist = Vector3.Distance(pos, camera.Position);
                    grassToCameraDistanceMap[b] = dist;
                    b.Size = Vector2.Zero;
                }

                // Set grass size
                //float lerpAmount = dist / grassRadius;
                //b.Size = Vector2.Lerp(size, Vector2.Zero, lerpAmount);
                Vector2 newSize = Vector2.Zero;
                newSize.X = b.Size.X + (grassToCameraDistanceMap[b] - dist) * 0.05f;
                if (newSize.X > grassBillboard.Size.X) newSize.X = grassBillboard.Size.X;
                if (newSize.X < 0.0f) newSize.X = 0.0f;
                newSize.Y = newSize.X;

                b.Size = newSize;   

                // Update the distance map
                grassToCameraDistanceMap[b] = dist;

                // Set position
                pos.Y = terrain.GetHeight(pos.X, pos.Z);
                if (pos.Y < waterHeight)
                {
                    b.Position = Vector3.Zero;
                }
                else
                {
                    pos.Y += b.Size.Y;
                    b.Position = pos;
                }
            }
        }

        /// <summary>
        /// Updates the active light and sky color based on the current
        /// rotation of the light. Level must have 4 Directional lights
        /// defined at locations [0-3] in Lights array, with each light
        /// also providing the sky color in the Ambient color channel.
        /// Level.SkyColor represents the 'Noon' sky color, with lights
        /// 0, 2, and 3 providing the sky colors for morning, evening, and
        /// night respectively.
        /// </summary>
        /// <param name="dt"></param>
        private void UpdateLightingAndSkyColor(FirstPersonCamera camera, float dt)
        {
            // Update the rotation angle
            if (runDayNightCycle)
            {
                if (daytimeTransition)
                    lightRotationAngle -= dt * dayNightCycleSpeed;
                else
                    lightRotationAngle += dt * dayNightCycleSpeed;
            }

            float lightRotationAngleDegrees = MathHelper.ToDegrees(lightRotationAngle);
            if (lightRotationAngleDegrees < -maxLightRotationDegrees)
            {
                daytimeTransition = false;
                lightRotationAngleDegrees = -maxLightRotationDegrees;
            }
            else if (lightRotationAngleDegrees > maxLightRotationDegrees)
            {
                daytimeTransition = true;
                lightRotationAngleDegrees = maxLightRotationDegrees;
            }

            // Orbit light about its rotation axis
            Matrix rotationMatrix = Matrix.CreateFromAxisAngle(lightRotationAxis, lightRotationAngle);
            activeLight.Direction = Vector3.TransformNormal(originalLightDirection, rotationMatrix);

            // Update light attributes
            Light light0 = lights[0];
            Light light1 = lights[0];
            Vector4 skyColor0 = originalSkyColor;
            Vector4 skyColor1 = originalSkyColor;
            float lerpAmount = 0.0f;
            if (lightRotationAngleDegrees <= maxLightRotationDegrees &&
                lightRotationAngleDegrees > 0)
            {
                if (daytimeTransition)
                {
                    // Morning to Noon
                    light0 = lights[0];
                    light1 = lights.Length > 1 ? lights[1] : lights[0];

                    skyColor0 = light0.Ambient;
                    skyColor1 = originalSkyColor;

                    lerpAmount = 1 - (lightRotationAngleDegrees / maxLightRotationDegrees);
                }
                else
                {
                    // Night to morning
                    light0 = lights.Length > 1 ? lights[3] : lights[0];
                    light1 = lights[0];

                    skyColor0 = light0.Ambient;
                    skyColor1 = light1.Ambient;

                    lerpAmount = 1 - Math.Min(((maxLightRotationDegrees - Math.Abs(lightRotationAngleDegrees)) /
                                           (maxLightRotationDegrees - 90.0f)), 1.0f);
                }
            }
            else if (lightRotationAngleDegrees <= 0 &&
                    lightRotationAngleDegrees >= -maxLightRotationDegrees)
            {
                if (daytimeTransition)
                {
                    // Noon to Evening
                    light0 = lights.Length > 1 ? lights[1] : lights[0];
                    light1 = lights.Length > 1 ? lights[2] : lights[0];

                    skyColor0 = originalSkyColor;
                    skyColor1 = light1.Ambient;

                    lerpAmount = lightRotationAngleDegrees / (-maxLightRotationDegrees);
                }
                else
                {
                    // Evening to Night
                    light0 = lights.Length > 1 ? lights[2] : lights[0];
                    light1 = lights.Length > 1 ? lights[3] : lights[0];

                    skyColor0 = light0.Ambient;
                    skyColor1 = light1.Ambient;

                    lerpAmount = Math.Min(((maxLightRotationDegrees - Math.Abs(lightRotationAngleDegrees)) /
                                           (maxLightRotationDegrees - 90.0f)), 1.0f);
                }
            }

            // Interpolate light values
            activeLight.Ambient = Vector4.Lerp(light0.Ambient, light1.Ambient, lerpAmount) * (1 - weatherLerpAmount);
            activeLight.Diffuse = Vector4.Lerp(light0.Diffuse, light1.Diffuse, lerpAmount);
            activeLight.Specular = Vector4.Lerp(light0.Specular, light1.Specular, lerpAmount);
            activeLight.Size = (light0.Size * (1.0f - lerpAmount)) - (light1.Size * -lerpAmount);

            // Interpolate sky values
            skyColor = Vector4.Lerp(Vector4.Lerp(skyColor0, skyColor1, lerpAmount),
                weatherSkyColor, weatherLerpAmount);
            skyColor.W = 1.0f;

            // Set final light position
            activeLight.Position = camera.Position + (-activeLight.Direction * 250.0f);

            // Set ambient particle effects
            emitAmbientParticles = !daytimeTransition && ((int)lerpAmount == 1 || (int)lerpAmount == 0);
        }

        private void UpdateWeatherEffects(FirstPersonCamera camera, float dt)
        {
            Vector3 rainDirection = Vector3.Normalize(new Vector3(0.025f, -1.0f, 0.035f));
            Vector2 rainSize = new Vector2(0.5f, 5.0f + (float)rand.NextDouble());
            float rainSpeed = 5.0f;

            float skySpeed = 0.25f;
            float fogSpeed = 200.0f;

            // Possibly toggle rain on each interval
            if (rainIntervalTimer <= 0.0f)
            {
                // Enable/Disable rain
                rainEnabled = rand.NextDouble() <= 0.25;
                rainIntervalTimer = rainInterval;
            }
            else
            {
                rainIntervalTimer -= dt;
            }

            // Update weather sky color and amount
            if (rainEnabled)
            {
                rainTimer -= dt;

                // Increase particles
                if (rainTimer < 0.0f)
                {
                    rainTimer = rainDelta;
                    numRainParticlesToEmit++;
                    if (rainParticleEmitter.NumActiveParticles + numRainParticlesToEmit >
                        rainParticleEmitter.MaxParticles)
                    {
                        // Cannot emit more particles
                        numRainParticlesToEmit--;
                    }
                }
                    
                weatherSkyColor = Vector4.Zero;
                weatherLerpAmount += (dayNightCycleSpeed * dt * skySpeed * 2.0f);
                fogStart -= (dayNightCycleSpeed * dt * fogSpeed * 2.0f);
                fogRange -= (dayNightCycleSpeed * dt * fogSpeed * 2.0f);

                if (weatherLerpAmount > 0.5f) weatherLerpAmount = 0.5f;
                if (fogStart < originalFogStart * 0.15f) fogStart = originalFogStart * 0.15f;
                if (fogRange < originalFogRange * 0.4f) fogRange = originalFogRange * 0.4f;
            }
            else
            {
                rainTimer += dt;

                // Decrease particles
                if (rainTimer - rainDelta > 0.0f)
                {
                    rainTimer = 0.0f;
                    numRainParticlesToEmit--;
                    if (numRainParticlesToEmit < 0)
                    {
                        numRainParticlesToEmit = 0;
                        rainTimer = rainDelta;
                    }
                }

                // Update the fog and sky after rain particles have stopped
                if (numRainParticlesToEmit <= 0)
                {
                    weatherLerpAmount -= (dayNightCycleSpeed * dt * (skySpeed));
                    fogStart += (dayNightCycleSpeed * dt * (fogSpeed));
                    fogRange += (dayNightCycleSpeed * dt * (fogSpeed));

                    if (weatherLerpAmount < 0.0f) weatherLerpAmount = 0.0f;
                    if (fogStart > originalFogStart) fogStart = originalFogStart;
                    if (fogRange > originalFogRange) fogRange = originalFogRange;
                }
            }

            // Update emitter positions
            rainParticleEmitter.Position = camera.Position;

            // Emit particles
            rainParticleEmitter.EmitParticles(numRainParticlesToEmit, rainDirection, rainSize, rainSpeed);

            // Update emitters
            rainParticleEmitter.Update(dt);
        }

        private void UpdateAmbientParticles(FirstPersonCamera camera, float dt)
        {
            ambientParticleTimer += dt;

            // Update emitter position
            ambientParticleEmitter1.Position = camera.Position;
            ambientParticleEmitter2.Position = camera.Position;
            ambientParticleEmitter3.Position = camera.Position;

            // Emit particles
            if (emitAmbientParticles && numRainParticlesToEmit <= 0 &&
                ambientParticleTimer > ambientParticleTimerDelta)
            {
                ambientParticleEmitter1.EmitParticles(1, Vector3.Down, Vector2.One * 0.25f, 5.0f);
                ambientParticleEmitter2.EmitParticles(1, Vector3.Down, Vector2.One * 0.25f, 5.0f);
                ambientParticleEmitter3.EmitParticles(1, Vector3.Down, Vector2.One * 0.25f, 5.0f);

                ambientParticleTimer = 0.0f;
            }

            // Update emitter
            ambientParticleEmitter1.Update(dt);
            ambientParticleEmitter2.Update(dt);
            ambientParticleEmitter3.Update(dt);
        }

        public void DrawParticleEffects(FirstPersonCamera camera)
        {
            // Draw particles currently active
            rainParticleEmitter.DrawParticles(camera);
            ambientParticleEmitter1.DrawParticles(camera);
            ambientParticleEmitter2.DrawParticles(camera);
            ambientParticleEmitter3.DrawParticles(camera);
        }

        // PROPERTIES
        public string Title
        {
            get { return title; }
        }

        public LevelType Type
        {
            get { return type; }
        }

        public Song BackgroundMusic
        {
            get { return backgroundMusic; }
        }

        public SoundEffectInstance AmbientSFX
        {
            get { return ambientSFXInstance; }
        }

        public SoundEffectInstance WeatherSFX
        {
            get { return weatherSFXInstance; }
        }

        public SoundEffectInstance WaterSFX
        {
            get { return waterSFXInstance; }
        }

        public SoundEffect FootstepSFX1
        {
            get { return footstepSFX1; }
        }

        public SoundEffect FootstepSFX2
        {
            get { return footstepSFX2; }
        }

        public Vector3 CameraStartPosition
        {
            get { return cameraStartPosition; }
        }

        public Vector3 CameraStartDirection
        {
            get { return cameraStartDirection; }
        }

        public float CameraHeightOffset
        {
            get { return cameraHeightOffset; }
        }

        public Vector4 SkyColor
        {
            get { return skyColor; }
        }

        public float DarkSkyOffset
        {
            get { return darkSkyOffset; }
        }

        public float FogStart
        {
            get { return fogStart; }
        }

        public float FogRange
        {
            get { return fogRange; }
        }

        public float WaterHeight
        {
            get { return waterHeight; }
        }

        public Vector4 WaterColor
        {
            get { return waterColor; }
        }

        public float DeepWaterFogDistance
        {
            get { return deepWaterFogDistance; }
        }

        public Light[] Lights
        {
            get { return lights; }
        }

        public Light ActiveLight
        {
            get { return activeLight; }
        }

        public TexturedSkyDome Sky
        {
            get { return sky; }
        }

        public TerrainGrid Terrain
        {
            get { return terrain; }
        }

        public TerrainGrid TerrainPlane
        {
            get { return terrainPlane; }
        }

        public SurfacePlane Surface
        {
            get { return surface; }
        }

        public WaterGrid Water
        {
            get { return water; }
        }

        public List<Billboard> BillboardList
        {
            get { return billboardList; }
        }

        public List<Mesh> MeshList
        {
            get { return meshList; }
        }

        public List<Billboard> GrassBillboards
        {
            get { return grassBillboards; }
        }

        public int MaxParticles
        {
            get { return maxParticles; }
            set
            {
                maxParticles = value;
                rainParticleEmitter.MaxParticles = maxParticles;
            }
        }
        public ParticleEmitter RainParticleEmitter
        {
            get { return rainParticleEmitter; }
        }

        public bool RunDayNightCycle
        {
            get { return runDayNightCycle; }
            set { runDayNightCycle = value; }
        }

        public float DayNightCycleSpeed
        {
            get { return dayNightCycleSpeed; }
            set { dayNightCycleSpeed = value; }
        }

        public bool RainEnabled
        {
            get { return rainEnabled; }
            set { rainEnabled = value; }
        }

        public int NumRainParticlesToEmit
        {
            get { return numRainParticlesToEmit; }
            set { numRainParticlesToEmit = value; }
        }
    }

    class RainParticleUpdater : ParticleEmitterUpdater
    {
        Random rand;
        public RainParticleUpdater()
        {
            rand = new Random();
        }

        public void UpdateParticle(Particle p, float dt)
        {
            if (p.Age == 0.0f)
            {
                // Set particle's position to random offset around camera
                Vector3 randVec = new Vector3((float)(rand.NextDouble() - 0.5), 0.0f, (float)(rand.NextDouble() - 0.5));
                randVec += SharedEffectParameters.xEyeDirection * 0.5f;
                randVec *= 200.0f;
                randVec.Y = 100.0f;

                p.Position = SharedEffectParameters.xEyePosW + randVec;
            }

            // Update particle age and position
            p.Age += dt;
            p.Position = p.Position + p.Direction * p.Speed;
        }
    }

    class AmbientParticleUpdater : ParticleEmitterUpdater
    {
        Random rand;
        public AmbientParticleUpdater()
        {
            rand = new Random();
        }

        public void UpdateParticle(Particle p, float dt)
        {
            if (p.Age == 0.0f)
            {
                // Set particle's position to random offset around camera
                Vector3 randVec = new Vector3((float)(rand.NextDouble() - 0.5), 0.0f, (float)(rand.NextDouble() - 0.5));
                randVec += SharedEffectParameters.xEyeDirection * 0.5f;
                randVec *= 200.0f;
                randVec.Y = 200.0f * ((float)rand.NextDouble() - 0.5f);
                p.Position = SharedEffectParameters.xEyePosW + randVec;

                // Randomly set speed
                p.Speed = (float)rand.NextDouble() * p.Speed;  // p.Speed used to initially store maximum speed
            }

            // Update particle age and position
            p.Age += dt;
            Vector3 pos = p.Position;
            pos.X += (float)Math.Cos(p.Age * p.Speed) * 0.2f;
            pos.Y += (float)Math.Sin(p.Age * p.Speed) * 0.2f;
            pos.Z += (float)Math.Cos(p.Age * p.Speed) * 0.2f;
            p.Position = pos;
        }
    }
}
