﻿using Urho3DNet;

namespace ZombieDollsCS.GameStates
{
    public class VictoryState : StateBase
    {
        public VictoryState(GameState game) : base(game)
        {
        }

        /// <inheritdoc/>
        public override void EndInteraction(InteractionKey interactionKey, IntVector2 interactionPosition)
        {
            //Game.NextLevel();
        }
    }
}