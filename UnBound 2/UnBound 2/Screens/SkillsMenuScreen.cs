using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameStateManagement
{
    class SkillsMenuScreen : MenuScreen
    {

        public SkillsMenuScreen()
            : base("Skills")
        {
            drawMenuEntryBackgrounds = false;

            // Menu Entries
            MenuEntry speedSkillMenuEntry = new MenuEntry("Speed");
            MenuEntry jumpSkillMenuEntry = new MenuEntry("Jump");
            MenuEntry swimSkillMenuEntry = new MenuEntry("Swim");
            MenuEntry rockClimbingSkillMenuEntry = new MenuEntry("Rock Climbing");
            MenuEntry resetSkillsMenuEntry = new MenuEntry("Reset Skills");
            MenuEntry backMenuEntry = new MenuEntry("Back");

            speedSkillMenuEntry.Selected += SpeedSkillMenuEntrySelected;
            jumpSkillMenuEntry.Selected += JumpSkillMenuEntrySelected;
            swimSkillMenuEntry.Selected += SwimSkillMenuEntrySelected;
            rockClimbingSkillMenuEntry.Selected += RockClimbingSkillMenuEntrySelected;
            resetSkillsMenuEntry.Selected += ResetSkillsMenuEntrySelected;
            backMenuEntry.Selected += OnCancel;

            MenuEntries.Add(speedSkillMenuEntry);
            MenuEntries.Add(jumpSkillMenuEntry);
            MenuEntries.Add(swimSkillMenuEntry);
            MenuEntries.Add(rockClimbingSkillMenuEntry);
            MenuEntries.Add(resetSkillsMenuEntry);
            MenuEntries.Add(backMenuEntry);
        }

        private void SpeedSkillMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ActivePlayer.Profile.PurchaseSkill(GameObjects.SkillList.SpeedSkill);
        }

        private void JumpSkillMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ActivePlayer.Profile.PurchaseSkill(GameObjects.SkillList.JumpSkill);
        }

        private void SwimSkillMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ActivePlayer.Profile.PurchaseSkill(GameObjects.SkillList.SwimSkill);
        }

        private void RockClimbingSkillMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ActivePlayer.Profile.PurchaseSkill(GameObjects.SkillList.RockClimbingSkill);
        }

        private void ResetSkillsMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            ActivePlayer.Profile.DeleteSaveFile();
        }

        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw player profile
            ActivePlayer.Profile.Draw(TransitionAlpha);
        }
    }
}
