using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameObjects
{
    enum SkillList
    {
        SpeedSkill,
        JumpSkill,
        SwimSkill,
        RockClimbingSkill
    }

    enum SpeedSkillLevel
    {
        LevelOne   = 0,
        LevelTwo   = 1,//10,
        LevelThree = 2,//25,
        LevelFour  = 3,//50,
        LevelFive  = 4,//75
    }

    enum SpeedSkillValues
    {
        LevelOne = 35,
        LevelTwo = 45,
        LevelThree = 60,
        LevelFour = 75,
        LevelFive = 100
    }

    enum JumpSkillLevel
    {
        LevelOne   = 0,
        LevelTwo   = 1,//10,
        LevelThree = 2,//25,
        LevelFour  = 3,//50,
        LevelFive  = 4,//75
    }

    enum JumpSkillValues
    {
        LevelOne = 50,
        LevelTwo = 100,
        LevelThree = 150,
        LevelFour = 200,
        LevelFive = 250
    }

    enum SwimSkillLevel
    {
        Locked,
        Unlocked = 5 
    }

    enum RockClimbingSkillLevel
    {
        Locked,
        Unlocked = 5
    }
}
