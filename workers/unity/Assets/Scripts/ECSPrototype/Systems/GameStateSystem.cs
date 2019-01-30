using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LeyLineHybridECS
{
    public static class GameStateSystem
    {
        public enum State
        {
            CalculateEnergy = 0,
            //planning phase
            WaitingForInput = 1,
            UnitClicked = 2,
            //execute phase
            Spawning = 3,
            Attacking = 4,
            Moving = 5,
            GameOver = 6
        }

        public static State CurrentState;

        //int to determine active player in single Player Prototype
        public static int activePlayer;
    }
}
