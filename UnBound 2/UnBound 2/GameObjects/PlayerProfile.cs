using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Storage;
using System.IO.IsolatedStorage;

namespace GameObjects
{
    class PlayerProfile
    {
        const string GAME_DATA_FILENAME = "UnBound2Data.txt";

        // Graphics data
        GraphicsDevice graphicsDevice;
        SpriteBatch spriteBatch;
        ContentManager content;
        SpriteFont font;

        Rectangle gamerTagRect;
        Rectangle statsRect;
        Texture2D background;
        Texture2D medalBackground;

        SpriteFont medalFont;
        Texture2D medalIcon;
        SoundEffect medalSFX;

        // Log in data
        bool isLoggedIn;
        IsolatedStorageFile storageFile;
        string saveGameFileName;
        SignedInGamer playerGamerTag;
        Texture2D gamerPicture;

        int gamerTagWidth;
        int gamerTagHeight;
        Vector2 textPosition;
        Rectangle gamerPicRect;

        // Character skills
        SpeedSkillLevel speedSkill;
        JumpSkillLevel jumpSkill;
        SwimSkillLevel swimSkill;
        RockClimbingSkillLevel rockClimbingSkill;

        // Gameplay Statistics
        int totalOrbsCollected;
        int numAgilityOrbsCollected;
        int numSecretOrbsCollected;

        int medalsUnlocked;
        Dictionary<string, Medal> medalList;
        Dictionary<string, int> medalRequirements;

        // Profile Settings
        bool invertCameraY;
        float cameraSensitivityX;
        float cameraSensitivityY;
        bool musicEnabled;
        float musicVolume;

        public PlayerProfile(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
        {
            this.graphicsDevice = graphicsDevice;
            this.spriteBatch = spriteBatch;
            this.content = content;

            saveGameFileName = "UnBound2.dat";

            medalList = new Dictionary<string, Medal>();
            medalRequirements = new Dictionary<string, int>();

            font = content.Load<SpriteFont>(@"bigFont");
            medalFont = content.Load<SpriteFont>("smallFont");
            medalIcon = content.Load<Texture2D>("medal_icon");
            medalSFX = content.Load<SoundEffect>(@"SoundEffects\medal_successSFX");

            if (ActivePlayer.FullHDEnabled)
            {
                gamerTagWidth = (int)(graphicsDevice.Viewport.Width * 0.25f);
                gamerTagHeight = (int)(graphicsDevice.Viewport.Height * 0.1f);
            }
            else
            {
                gamerTagWidth = (int)(graphicsDevice.Viewport.Width * 0.33f);
                gamerTagHeight = (int)(graphicsDevice.Viewport.Height * 0.15f);
            }

            int xPos = ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Width * 0.65f)
                : (int)(graphicsDevice.Viewport.Width * 0.6f);

            int yPos = ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Height * 0.15f)
                : (int)(graphicsDevice.Viewport.Height * 0.1f);

            gamerTagRect = new Rectangle(xPos, yPos,
                gamerTagWidth,
                gamerTagHeight);

            statsRect = new Rectangle(xPos,
                yPos + (int)(gamerTagRect.Height * 1.5f),
                gamerTagWidth,
                ActivePlayer.FullHDEnabled ? (int)(graphicsDevice.Viewport.Height * 0.5f)
                    : (int)(graphicsDevice.Viewport.Height * 0.6f));

            // Load background texture
            background = content.Load<Texture2D>(@"menu_background");
            medalBackground = content.Load<Texture2D>(@"menu_background");

            // Default profile settings
            invertCameraY = false;
            cameraSensitivityX = GameStateManagement.OptionsMenuScreen.cameraSensitivitySettings[1];
            cameraSensitivityY = GameStateManagement.OptionsMenuScreen.cameraSensitivitySettings[1];
            musicEnabled = true;
            musicVolume = 0.33f;
            MediaPlayer.Volume = musicVolume;
        }

        private void SelectStorageDevice()
        {
            // Select storage device if needed
            storageFile = IsolatedStorageFile.GetUserStoreForApplication();
        }

        public bool IsXboxLiveEnabled()
        {
            return playerGamerTag.IsSignedInToLive;
        }

        /// <summary>
        /// Logs in player. Loads data from file, or creates one if needed.
        /// </summary>
        /// <param name="playerIndex">Index of gamer to login</param>
        /// <returns></returns>
        public bool Login(PlayerIndex playerIndex)
        {
            // Get the gamer
            playerGamerTag = Gamer.SignedInGamers[playerIndex];

            if (playerGamerTag == null)
            {
                // This player has no valid gamertag
                return false;
            }

            SelectStorageDevice();

            // Load gamertag data
            GamerProfile profile = playerGamerTag.GetProfile();
            gamerPicture = Texture2D.FromStream(graphicsDevice,
                profile.GetGamerPicture());

            int picWidth = (int)(gamerPicture.Width * 1.5f);
            int picHeight = (int)(gamerPicture.Height * 1.5f);
            gamerPicRect = new Rectangle((int)(gamerTagRect.X + gamerTagRect.Width * 0.025f),
                gamerTagRect.Y + (gamerTagRect.Height - picHeight) / 2,
                picWidth,
                picHeight);

            Vector2 textSize = font.MeasureString(playerGamerTag.Gamertag);
            textPosition = new Vector2(gamerPicRect.X + (picWidth * 1.2f),
                gamerPicRect.Y);

            saveGameFileName = playerGamerTag.Gamertag + "_UnBound2.dat";

            // Set default settings from gamertag
            invertCameraY = playerGamerTag.GameDefaults.InvertYAxis;

            // Load data from save file
            LoadDataFromFile();

            isLoggedIn = true;
            return true;
        }

        public void Logout()
        {
            // Save data to file
            SaveDataToFile();

            // Log out player
            isLoggedIn = false;
        }

        public void LoadDataFromFile()
        {
            InitializeProfile();

#if XBOX
            if(!Guide.IsTrialMode)
            {
                if (storageFile != null)
                {
                    if (storageFile.FileExists(saveGameFileName))
                    {
                        IsolatedStorageFileStream file = storageFile.OpenFile(saveGameFileName, FileMode.Open, FileAccess.Read);

                        // Read data from file
                        byte[] buffer = new byte[file.Length];
                        file.Read(buffer, 0, buffer.Length);
                        string data = GetString(buffer);
                        DeserializeData(data);

                        file.Close();
                    }
                }
            }
#else
            StreamReader inFile = new StreamReader(saveGameFileName);

            string dataStr = inFile.ReadToEnd();
            DeserializeData(dataStr);
            
            inFile.Close();
#endif
        }

        public void SaveDataToFile()
        {
            if (!Guide.IsTrialMode)
            {
#if XBOX
                if (storageFile != null)
                {
                    IsolatedStorageFileStream file =
                        storageFile.OpenFile(saveGameFileName, FileMode.Create, FileAccess.Write);

                    // Write data to file
                    string data = SerializeData();
                    byte[] buffer = GetBytes(data);
                    file.Write(buffer, 0, buffer.Length);

                    // Close file
                    file.Close();
                }
#else
                StreamWriter outFile = new StreamWriter(saveGameFileName);

                // Write data to file
                string dataStr = SerializeData();
                outFile.Write(dataStr);

                outFile.Close();
#endif
            }
        }

        public void DeleteSaveFile()
        {
            if (storageFile != null)
            {
                if (storageFile.FileExists(saveGameFileName))
                {
                    storageFile.DeleteFile(saveGameFileName);
                }
            }

            // Reset all stats
            totalOrbsCollected = 0;
            numAgilityOrbsCollected = 0;
            numSecretOrbsCollected = 0;

            speedSkill = SpeedSkillLevel.LevelOne;
            jumpSkill = JumpSkillLevel.LevelOne;
            swimSkill = SwimSkillLevel.Locked;
            rockClimbingSkill = RockClimbingSkillLevel.Locked;

            medalsUnlocked = 0;

            medalList = new Dictionary<string, Medal>();
            medalRequirements = new Dictionary<string, int>();

            InitializeProfile();
        }

        private string SerializeData()
        {
            // Turn data to be saved into a string object
            string data = string.Empty;

            // CHARACTER SKILLS
            data += "TOTAL_ORBS_COLLECTED\n" + totalOrbsCollected.ToString() + "\n";
            data += "NUM_SECRET_ORBS_COLLECTED\n" + numSecretOrbsCollected.ToString() + "\n";
            data += "NUM_AGILITY_ORBS_COLLECTED\n" + numAgilityOrbsCollected.ToString() + "\n";
            data += "SPEED_SKILL\n" + speedSkill.ToString() + "\n";
            data += "JUMP_SKILL\n" + jumpSkill.ToString() + "\n";
            data += "SWIM_SKILL\n" + swimSkill.ToString() + "\n";
            data += "ROCK_CLIMBING_SKILL\n" + swimSkill.ToString() + "\n";

            // GAME DATA
            data += "MEDALS_UNLOCKED\n" + medalsUnlocked.ToString() + "\n";

            // MEDALS
            foreach (string medalTitle in medalList.Keys)
            {
                data += medalTitle + "\n" + (medalList[medalTitle].IsUnlocked ?
                    "UNLOCKED" : "LOCKED");
                data += "\n"; 
            }

            // SETTINGS
            data += "INVERT_CAMERA_Y" + "\n" + (invertCameraY ? "ENABLED" : "DISABLED") + "\n";
            data += "CAMERA_X_SENSITIVITY" + "\n" + cameraSensitivityX.ToString(CultureInfo.InvariantCulture) + "\n";
            data += "CAMERA_Y_SENSITIVITY" + "\n" + cameraSensitivityY.ToString(CultureInfo.InvariantCulture) + "\n";
            data += "MUSIC_VOLUME" + "\n" + (musicEnabled ? "ENABLED" : "DISABLED") + "\n";

            return data;
        }

        private void DeserializeData(string data)
        {
            char[] separator = { '\n' };
            string[] dataArray = data.Split(separator);
            Dictionary<string, string> dataDictionary = new Dictionary<string, string>();

            for (int i = 0; i < dataArray.Length && dataArray[i] != ""; i+=2)
            {
                // Store key/value pairs into the dictionary
                dataDictionary.Add(dataArray[i], dataArray[i + 1]);
            }

            foreach (string key in dataDictionary.Keys)
            {
                if (key == "TOTAL_ORBS_COLLECTED")
                {
                    totalOrbsCollected = int.Parse(dataDictionary[key]);
                } 
                if (key == "NUM_SECRET_ORBS_COLLECTED")
                {
                    numSecretOrbsCollected = int.Parse(dataDictionary[key]);
                }
                else if (key == "NUM_AGILITY_ORBS_COLLECTED")
                {
                    numAgilityOrbsCollected = int.Parse(dataDictionary[key]);
                }
                else if (key == "SPEED_SKILL")
                {
                    speedSkill = (SpeedSkillLevel)Enum.Parse(typeof(SpeedSkillLevel), dataDictionary[key]);
                }
                else if (key == "JUMP_SKILL")
                {
                    jumpSkill = (JumpSkillLevel)Enum.Parse(typeof(JumpSkillLevel), dataDictionary[key]);
                }
                else if (key == "SWIM_SKILL")
                {
                    swimSkill = (SwimSkillLevel)Enum.Parse(typeof(SwimSkillLevel), dataDictionary[key]);
                }
                else if (key == "ROCK_CLIMBING_SKILL")
                {
                    rockClimbingSkill = (RockClimbingSkillLevel)Enum.Parse(typeof(RockClimbingSkillLevel), dataDictionary[key]);
                }
                else if (key == "MEDALS_UNLOCKED")
                {
                    medalsUnlocked = int.Parse(dataDictionary[key]);
                }
                else if (medalList.ContainsKey(key))
                {
                    if (dataDictionary[key] == "UNLOCKED")
                    {
                        // Set this medal to be unlocked
                        medalList[key].IsUnlocked = true;
                    }
                }
                else if (key == "INVERT_CAMERA_Y")
                {
                    invertCameraY = (dataDictionary[key] == "ENABLED");
                }
                else if (key == "CAMERA_X_SENSITIVITY")
                {
                    cameraSensitivityX = float.Parse(dataDictionary[key], CultureInfo.InvariantCulture);
                }
                else if (key == "CAMERA_Y_SENSITIVITY")
                {
                    cameraSensitivityY = float.Parse(dataDictionary[key], CultureInfo.InvariantCulture);
                }
                else if (key == "MUSIC_VOLUME")
                {
                    MusicEnabled = (dataDictionary[key] == "ENABLED");
                }
            }
        }

        private byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private void InitializeProfile()
        {
            totalOrbsCollected = 0;
            numAgilityOrbsCollected = 0;
            numSecretOrbsCollected = 0;

            speedSkill = SpeedSkillLevel.LevelOne;
            jumpSkill = JumpSkillLevel.LevelOne;
            swimSkill = SwimSkillLevel.Locked;
            rockClimbingSkill = RockClimbingSkillLevel.Locked;

            List<Medal> gunMedals = new List<Medal>();
            List<Medal> secretMedals = new List<Medal>();
            List<Medal> scoreAttackMedals = new List<Medal>();

            // Read in game data from file
            StreamReader inFile = new StreamReader(GAME_DATA_FILENAME);
            string line;

            while (!inFile.EndOfStream)
            {
                line = inFile.ReadLine();

                
            }

            string title;

            // Create medals
            title = "Orb Collector";
            medalRequirements.Add(title, 10);
            AddMedal(title, "Collect " + medalRequirements[title] + " orbs");

            title = "Radical Hangtime";
            medalRequirements.Add(title, 5);
            AddMedal(title, "Stay airborn for over " + medalRequirements[title] + " seconds");

            AddMedal("Nice to meet you", "???");

            AddMedal("UnBound", 
                "Available now on" + "\n" +
                "Xbox Live Indie Games");

            AddMedal("ShootOut",
                "Available now on" + "\n" +
                "Xbox Live Indie Games");

            AddMedal("ShootOut Reloaded",
                "Available now on" + "\n" +
                "Xbox Live Indie Games");

            AddMedal("Master Orchestrator", "D-Mav on the Mic!");

            AddMedal("Master Medal", 
                "Unlock every Medal" + "\n" +
                "in UnBound 2");
        }

        public void UnlockMedal(string medalTitle)
        {
            // Unlock the medal if it exists
            if (medalList[medalTitle] != null)
            {
                // Unlock the medal
                if (medalList[medalTitle].Unlock()) medalsUnlocked++;
            }

            // Unlock "Master Medal"
            if (medalsUnlocked == medalList.Count - 1)
            {
                UnlockMedal("Master Medal");
            }
        }

        public void UpdateStatsAndMedals()
        {
            if (totalOrbsCollected >= medalRequirements["Orb Collector"])
            {
                UnlockMedal("Orb Collector");
            }
        }

        private void AddMedal(string medalTitle, string medalDescription)
        {
            Medal newMedal = new Medal(graphicsDevice, spriteBatch, font, medalFont,
                medalBackground, medalIcon, medalSFX,
                medalTitle, medalDescription);
            medalList.Add(newMedal.Title, newMedal);
        }

        public int GetMedalRequirement(string medalTitle)
        {
            return medalRequirements[medalTitle];
        }

        public void Draw(float transitionAlpha)
        {
            DrawGamerTag(transitionAlpha);

            // Draw gamertag
            spriteBatch.Begin();

            // Draw stats
            string statsString = "PLAYER STATS\n\n" +
                    "Orbs Collected: " + totalOrbsCollected.ToString() + "\n" +
                    "Agility Orbs:   " + numAgilityOrbsCollected.ToString() + "\n" +
                    "Secret Orbs:    " + numSecretOrbsCollected.ToString() + "\n\n" +
                    "Speed Skill:   " + speedSkill.ToString() + "\n" +
                    "Jump Skill:    " + jumpSkill.ToString() + "\n" +
                    "Swim Skill:    " + swimSkill.ToString() + "\n" +
                    "Rock Climbing: " + rockClimbingSkill.ToString() + "\n";

            float statsScale = ActivePlayer.FullHDEnabled ? 1.0f : 0.85f;
            Vector2 statsSize = font.MeasureString(statsString) * statsScale;
            Vector2 statsPosition =
                new Vector2(statsRect.X + statsRect.Width / 2 - statsSize.X / 2,
                            statsRect.Y + statsRect.Height/2 - statsSize.Y/2);

            spriteBatch.Draw(background, statsRect, Color.White * (transitionAlpha - 0.2f));
            spriteBatch.DrawString(font, statsString, statsPosition,
                    Color.White * transitionAlpha, 0.0f, Vector2.Zero, statsScale, SpriteEffects.None, 0.0f);
  
            spriteBatch.End();
        }

        public void DrawGamerTag(float transitionAlpha)
        {
            if (isLoggedIn)
            {
                spriteBatch.Begin();

                // Draw Gamertag
                spriteBatch.Draw(background, gamerTagRect, Color.White * (transitionAlpha - 0.2f));
                spriteBatch.Draw(gamerPicture, gamerPicRect, Color.White * transitionAlpha);
                spriteBatch.DrawString(font, playerGamerTag.Gamertag,
                    textPosition, Color.White * transitionAlpha);

                string medalsInfo = "Medals: " + medalsUnlocked + "/" + medalList.Count;
                Vector2 medalsInfoSize = font.MeasureString(medalsInfo);
                Vector2 medalsInfoPosition = new Vector2(textPosition.X, textPosition.Y + gamerPicRect.Height - (medalsInfoSize.Y));
                spriteBatch.DrawString(font, medalsInfo,
                    medalsInfoPosition, Color.White * transitionAlpha);

                spriteBatch.End();
            }
        }

        Medal currentlyDrawnMedal;
        public void DrawMedals(float dt)
        {
            spriteBatch.Begin();

            // Draw each Medal
            if (currentlyDrawnMedal != null)
            {
                if (!currentlyDrawnMedal.Draw(dt))
                {
                    currentlyDrawnMedal = null;
                }
            }
            else
            {
                foreach (string medalTitle in medalList.Keys)
                {
                    if (medalList[medalTitle].Draw(dt))
                    {
                        currentlyDrawnMedal = medalList[medalTitle];
                        break;
                    }
                }
            }

            spriteBatch.End();
        }

        public void CollectOrb(Orb orb)
        {
            if (orb is SecretOrb)
            {
                numSecretOrbsCollected++;
            }
            else if (orb is AgilityOrb)
            {
                numAgilityOrbsCollected++;
            }

            totalOrbsCollected++;
        }

        public void PurchaseSkill(SkillList skill)
        {
            if (skill == SkillList.SpeedSkill &&
                (int)speedSkill < (int)SpeedSkillLevel.LevelFive)
            {
                SpeedSkillLevel[] skillLevels = (SpeedSkillLevel[])Enum.GetValues(typeof(SpeedSkillLevel));
                int index = Array.IndexOf(skillLevels, speedSkill);

                // Check if player can afford skill
                if (numAgilityOrbsCollected >= (int)(skillLevels[index + 1]))
                {
                    // Increase skill level
                    speedSkill = skillLevels[index + 1];
                    numAgilityOrbsCollected -= (int)speedSkill;
                }
            }
            else if (skill == SkillList.JumpSkill &&
                (int)jumpSkill < (int)JumpSkillLevel.LevelFive)
            {
                JumpSkillLevel[] skillLevels = (JumpSkillLevel[])Enum.GetValues(typeof(JumpSkillLevel));
                int index = Array.IndexOf(skillLevels, jumpSkill);

                // Check if player can afford skill
                if (numAgilityOrbsCollected >= (int)(skillLevels[index + 1]))
                {
                    // Increase skill level
                    jumpSkill = skillLevels[index + 1];
                    numAgilityOrbsCollected -= (int)jumpSkill;
                }
            }
            else if (skill == SkillList.SwimSkill &&
                    swimSkill == SwimSkillLevel.Locked)
            {
                if (numSecretOrbsCollected >= (int)SwimSkillLevel.Unlocked)
                {
                    swimSkill = SwimSkillLevel.Unlocked;
                    numSecretOrbsCollected -= (int)SwimSkillLevel.Unlocked;
                }
            }
            else if (skill == SkillList.RockClimbingSkill &&
                    rockClimbingSkill == RockClimbingSkillLevel.Locked)
            {
                if (numSecretOrbsCollected >= (int)RockClimbingSkillLevel.Unlocked)
                {
                    rockClimbingSkill = RockClimbingSkillLevel.Unlocked;
                    numSecretOrbsCollected -= (int)RockClimbingSkillLevel.Unlocked;
                }
            }
        }

        // PROPERTIES
        // GAMEPLAY STATS
        public int TotalOrbsCollected
        {
            get { return totalOrbsCollected; }
        }

        public int NumSecretOrbsCollected
        {
            get { return numSecretOrbsCollected; }
        }

        public int NumAgilityOrbsCollected
        {
            get { return numAgilityOrbsCollected; }
        }

        public float CurrentMoveSpeed()
        {
            SpeedSkillLevel[] skillLevels = (SpeedSkillLevel[])Enum.GetValues(typeof(SpeedSkillLevel));
            SpeedSkillValues[] skillValues = (SpeedSkillValues[])Enum.GetValues(typeof(SpeedSkillValues));

            return (float)skillValues[Array.IndexOf(skillLevels, speedSkill)];
        }

        public float CurrentJumpStrength()
        {
            JumpSkillLevel[] skillLevels = (JumpSkillLevel[])Enum.GetValues(typeof(JumpSkillLevel));
            JumpSkillValues[] skillValues = (JumpSkillValues[])Enum.GetValues(typeof(JumpSkillValues));

            return (float)skillValues[Array.IndexOf(skillLevels, jumpSkill)];
        }

        public bool IsDoubleJumpUnlocked()
        {
            return true;
        }

        public bool IsSwimSkillUnlocked()
        {
            return swimSkill == SwimSkillLevel.Unlocked;
        }

        public bool IsRockClimbingUnlocked()
        {
            return rockClimbingSkill == RockClimbingSkillLevel.Unlocked;
        }

        public int MedalsUnlocked
        {
            get { return medalsUnlocked; }
        }

        public Dictionary<string, Medal> MedalList
        {
            get { return medalList; }
        }

        public JumpSkillLevel JumpSkill
        {
            get { return jumpSkill; }
        }

        public SpeedSkillLevel SpeedSkill
        {
            get { return speedSkill; }
        }

        // SETTINGS
        public bool IsLoggedIn
        {
            get { return isLoggedIn; }
        }

        public bool InvertCameraY
        {
            get { return invertCameraY; }
            set { invertCameraY = value; }
        }

        public float CameraSensitivityX
        {
            get { return cameraSensitivityX; }
            set { cameraSensitivityX = value; }
        }

        public float CameraSensitivityY
        {
            get { return cameraSensitivityY; }
            set { cameraSensitivityY = value; }
        }

        public bool MusicEnabled
        {
            get { return musicEnabled; }
            set
            {
                musicEnabled = value;

                // Set Media Player volume
                MediaPlayer.Volume = musicEnabled ? musicVolume : 0.0f;
            }
        }

        public float MusicVolume
        {
            get { return musicVolume; }
        }
    }
}