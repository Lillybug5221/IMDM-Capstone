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
            public CurrentGameStateFlags * GameStateFlags;
            
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            var currAction = filter.CurrAction;
            var actionConfigs = frame.SimulationConfig.ActionConfigs;
            var defaultCancelRules = frame.SimulationConfig.DefaultCancelRules;
            var transform = filter.Transform;

            var kcc = filter.KCC;

            //return for hitstop
            #region hitstop
            //return if hitstop
            if(HitstopTickSystem.GlobalHitstopActive(frame)){
                kcc->SetInputDirection(new FPVector3(0,0,0));
                return;
            }
            #endregion


            //Read current gamestate component, and update action accordingly.
            //update enemy position
            FPVector3 opponentPosition = new FPVector3(0,0,0);
            foreach (var pair in frame.GetComponentIterator<PlayerLink>())
            {
                EntityRef entity = pair.Entity;
                PlayerLink playerLink = pair.Component;
                if (playerLink.Player != filter.Link->Player)
                {
                    if (frame.Unsafe.TryGetPointer<Transform3D>(entity, out var enemyTransform))
                    {
                        opponentPosition = enemyTransform->Position;
                        //Log.Debug("opponent pos is" + opponentPosition);
                    }
                }
            }

            // update currAction's Updated Enemy Position used for tracking

            if(currAction->TrackingActive){
                currAction -> EnemyPosition = opponentPosition;
            }


            //keep rotation consistent no matter the current action;

            //set rotation to action saved enemy position
            FPVector3 playerPosition = filter.Transform->Position;
            FPVector3 savedOpponentPosition = currAction -> EnemyPosition;

            FPVector3 forwardDir = savedOpponentPosition - playerPosition;
            forwardDir.Y = FP._0;
            forwardDir = FPVector3.Normalize(forwardDir);

            //face player towards opponent
            FPQuaternion targetRotation = FPQuaternion.LookRotation(forwardDir, FPVector3.Up);
            FP rotationSpeed = FP._1;
            FPQuaternion currentRotation = filter.Transform->Rotation;  
            FPQuaternion slerpedRotation = FPQuaternion.Slerp(currentRotation, targetRotation, rotationSpeed);
            filter.Transform->Rotation = slerpedRotation;

            //get current gamestate as a byte flag

            //for the current action, get its list of possibily cancels

            //for each of those cancels, if its criteria is met in frame value and gamestate, 
            //set that to the current action

            CancelRule[] currActionCancelRules = defaultCancelRules.Rules;
            GameStateFlags currFlags = (GameStateFlags)(filter.GameStateFlags -> Flags);

            ActionConfigAsset currActionConfig = actionConfigs[currAction -> ActionIndex];
            if(currActionConfig.HasCustomCancelRules()){
                currActionCancelRules = currActionConfig.CustomCancelRules.Rules;
            }
            FPVector2 inputDirection = filter.GameStateFlags -> InputDirection;
            int foundIndex = -1;
            for(int i = 0; i < currActionCancelRules.Length; i++){
                ActionConfigAsset nextAction = currActionCancelRules[i].TargetAction;
                //check if in right phase
                if(currAction -> ActionPhase >= currActionCancelRules[i].CancelablePhase){

                }else{
                    continue;
                }
                //check if flags are met
                bool meetsFlags = (currFlags & nextAction.RequiredFlags) == nextAction.RequiredFlags && (currFlags & nextAction.ForbiddenFlags) == 0;
                if(meetsFlags){
                    if(foundIndex == -1){
                        foundIndex = i;
                    }else if(FPVector2.DistanceSquared(inputDirection, nextAction.RequiredDirection) < FPVector2.DistanceSquared(inputDirection, currActionCancelRules[foundIndex].TargetAction.RequiredDirection)){
                        foundIndex = i;
                    }
                }
            }

            if(foundIndex != -1){
                ActionConfigAsset nextAction = currActionCancelRules[foundIndex].TargetAction;
                currActionConfig.Deinitialize(frame, ref filter);
                SetCurrAction(actionConfigs.IndexOf(nextAction), frame, currAction, transform->Position, opponentPosition, inputDirection);
                nextAction.Initialize(frame, ref filter);
            }

            //reset flags
            filter.GameStateFlags -> Flags = 0;
            
        }

        private void SetCurrAction(int actionIndex, Frame frame, CurrentAction* currAction, FPVector3 playerPosition, FPVector3 opponentPosition, FPVector2 inputDir){
            var actionConfigs = frame.SimulationConfig.ActionConfigs;
            currAction -> ActionNumber = currAction -> ActionNumber + 1;
            currAction -> ActionIndex = actionIndex;
            currAction -> Direction = inputDir;
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
