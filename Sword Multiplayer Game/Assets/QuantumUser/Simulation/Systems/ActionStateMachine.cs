namespace Quantum
{
    using Photon.Deterministic;
    using System.Runtime.Versioning;
    using System.Collections.Generic;
    using Quantum;
    using Quantum.Addons.Animator;

    public unsafe class ActionStateMachine : SystemMainThreadFilter<ActionStateMachine.Filter>
    {

        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PlayerLink* Link;
            public AnimatorComponent* Animator;
            public CurrentAction* CurrAction;
            public KCC* KCC;
            public PhysicsCollider3D* Collider;
            
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            var currAction = filter.CurrAction;
            var actionConfigs = frame.SimulationConfig.ActionConfigs;
            var transform = filter.Transform;

            //Read current gamestate component, and update action accordingly.
            //update enemy position
            FPVector3 opponentPosition = new FPVector3(0,0,0);
            foreach (var pair in frame.GetComponentIterator<PlayerLink>()) {
                EntityRef entity = pair.Entity;
                PlayerLink playerLink = pair.Component;
                if(playerLink.Player != filter.Link -> Player){
                    if(frame.Unsafe.TryGetPointer<Transform3D>(entity, out var enemyTransform))
                    {
                        opponentPosition = enemyTransform->Position;
                        //Log.Debug("opponent pos is" + opponentPosition);
                    }
                }
            }

            SetCurrAction(0, frame, currAction, transform->Position, opponentPosition);
            
        }

        private void SetCurrAction(int actionIndex, Frame frame, CurrentAction* currAction, FPVector3 playerPosition, FPVector3 opponentPosition){
            var actionConfigs = frame.SimulationConfig.ActionConfigs;
            currAction -> ActionNumber = currAction -> ActionNumber + 1;
            currAction -> ActionIndex = actionIndex;
            //currAction -> ActionType = (byte)ActionType.Attack;
            currAction -> EnemyPosition = opponentPosition;
            currAction -> PlayerPosition = playerPosition;
            currAction -> StartTick = frame.Number;
            currAction -> StartUpFrames = (ushort)actionConfigs[actionIndex].StartUpFrames;
            currAction -> ActiveFrames = (ushort)actionConfigs[actionIndex].ActiveFrames;
            currAction -> RecoveryFrames = (ushort)actionConfigs[actionIndex].RecoveryFrames;
            currAction -> CancelableFrames = (ushort)actionConfigs[actionIndex].CancelableFrames;
            currAction -> ActionPhase = (byte)1;
            currAction -> Damage = (ushort)0;
            currAction -> DamageApplied = false;
        }
    }
}
