#region File Description
//-----------------------------------------------------------------------------
// PauseMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using GameObjects;
#endregion

namespace GameStateManagement
{
    /// <summary>
    /// The pause menu comes up over the top of the game,
    /// giving the player options to resume or quit.
    /// </summary>
    class PauseMenuScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public PauseMenuScreen()
            : base("Paused")
        {
            drawMenuEntryBackgrounds = false;

            // Create our menu entries.
            MenuEntry resumeGameMenuEntry = new MenuEntry("Resume");
            MenuEntry skillsMenuEntry = new MenuEntry("Skills");
            MenuEntry medalsMenuEntry = new MenuEntry("Medals");
            MenuEntry settingsGameMenuEntry = new MenuEntry("Settings");
            MenuEntry quitGameMenuEntry = new MenuEntry("Quit");
            
            // Hook up menu event handlers.
            resumeGameMenuEntry.Selected += OnCancel;
            skillsMenuEntry.Selected += SkillsMenuEntrySelected;
            medalsMenuEntry.Selected += MedalsMenuEntrySelected;
            settingsGameMenuEntry.Selected += SettingsGameMenuEntrySelected;
            quitGameMenuEntry.Selected += QuitGameMenuEntrySelected;

            // Add entries to the menu.
            MenuEntries.Add(resumeGameMenuEntry);
            MenuEntries.Add(skillsMenuEntry);
            MenuEntries.Add(medalsMenuEntry);
            MenuEntries.Add(settingsGameMenuEntry);
            MenuEntries.Add(quitGameMenuEntry);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
        }

        #endregion

        #region Handle Input


        /// <summary>
        /// Event handler for when the Quit Game menu entry is selected.
        /// </summary>
        void QuitGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            const string message = "Are you sure you want to quit?";

            MessageBoxScreen confirmQuitMessageBox = new MessageBoxScreen(message);

            confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmQuitMessageBox, ControllingPlayer);
        }


        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to quit" message box. This uses the loading screen to
        /// transition from the game back to the main menu screen.
        /// </summary>
        void ConfirmQuitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, false, ActivePlayer.PlayerIndex,
                new BackgroundScreen(),
                new MainMenuScreen());
        }

        private void SkillsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new SkillsMenuScreen(), ActivePlayer.PlayerIndex);
        }

        private void MedalsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.AddScreen(new MedalsMenuScreen(0, MedalsMenuScreen.TRANSITION_ON_TIME),
                ActivePlayer.PlayerIndex);
        }

        private void SettingsGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            // Add options screen
            ScreenManager.AddScreen(new OptionsMenuScreen(), ActivePlayer.PlayerIndex);
        }

        #endregion

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw player profile
            ActivePlayer.Profile.Draw(TransitionAlpha);
        }
    }
}
