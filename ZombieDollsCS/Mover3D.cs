using System;
using System.Collections.Generic;
using System.Text;
using Urho3DNet;

namespace ZombieDollsCS
{
    [ObjectFactory]
    public partial class Mover3D : LogicComponent
    {
        // Ptr to GameState
        private GameState _gameState = null;
        private Vector3 _speed;

        public Mover3D(Context context) : base(context)
        {
            // Only the scene update event is needed: unsubscribe from the rest for optimization
            UpdateEventMask = UpdateEvent.UseUpdate;
        }

        public Mover3D SetGameState(GameState gameState)
        {
            _gameState = gameState;
            return this;
        }

        public Mover3D SetSpeed(Vector3 speed)
        {
            _speed = speed;
            return this;
        }

        override public void Update(float timeStep)
        {
            // If in risk of going outside the plane, rotate the model right
            Vector3 pos = Node.Position;
            if (pos.Z > _gameState._mdScene._bounds.Min.Z && pos.Z < _gameState._mdScene._bounds.Max.Z)
                // node_->Yaw(rotationSpeed_ * timeStep);
                Node.Translate(_speed * timeStep);
            else
            {
                _gameState.PlaySoundEffect("BigExplosion.wav");
                _gameState._mdScene.CreateKicking(Node);

                Node.CreateComponent<MDRemoveCom>().SetGameState(_gameState).SetCountParams(50);
                _gameState._shooterState = ShooterState.Kicking;

                Node.RemoveComponent<Mover3D>();
            }
        }
    }
}
/*
        public void DollKilled()
        {
            uint num = _mdScene._zombiesNode.Ptr.GetNumChildren(false);
            if (num == 0) 
            { 
                // level up
                _dollsNum++;
                _mdScene.CreateModels(_dollsNum, _dollsSpeed);
            }
        }

        public void ShooterKilled()
        {
            if (_dollsNum > 1)
                // level down
                _dollsNum--; 
            _mdScene.CreateModels(_dollsNum, _dollsSpeed);
        }

 */