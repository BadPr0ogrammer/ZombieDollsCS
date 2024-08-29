using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using Urho3DNet;

namespace ZombieDollsCS
{
    [ObjectFactory]
    public partial class MDRemoveCom : LogicComponent
    {
        // Ptr to GameState
        private GameState _gameState = null;
        // cur counter, max num
        private int _countNum = 100;
        private int _count = 0;

        public MDRemoveCom(Context context) : base(context)
        {
            UpdateEventMask = UpdateEvent.UseUpdate;
        }

        public MDRemoveCom SetGameState(GameState gameState)
        {
            _gameState = gameState;
            return this;
        }

        public MDRemoveCom SetCountParams(int countNum, int count = 0)
        {
            _countNum = countNum;
            _count = count;
            return this;
        }
        
        public override void Update(float timeStep)
        {
            if (_gameState._shooterState == ShooterState.None)
                return;

            if (_count++ > _countNum)
            {
                Node.Remove();

                if (_gameState._mdScene._zombiesNode.Ptr.GetChildren().Count == 0)
                    _gameState.Victory();

                if (_gameState._shooterState == ShooterState.Kicking)
                {
                    foreach (var item in _gameState._mdScene._zombiesNode.Ptr.GetChildren())
                        item.GetComponent<MDRemoveCom>()?.SetGameState(_gameState).SetCountParams(200);

                    _gameState._shooterState = ShooterState.Waiting;
                } 
                else if (_gameState._shooterState == ShooterState.Waiting)
                {
                    _gameState._shooterState = ShooterState.None;
                    if (_gameState._levelIndex > 1)
                        _gameState._levelIndex--;

                    _gameState._shooterState = ShooterState.None;
                    _gameState._mdScene.CreateModels(_gameState._levelIndex, _gameState._dollsSpeed);
                    _gameState._shooterState = ShooterState.Start;
                }                
            }
        }
    }
}
