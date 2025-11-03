namespace Quantum
{
    using Photon.Deterministic;
    using System.Runtime.Versioning;
    using System.Collections.Generic;
    using Quantum.Physics3D;
    using Quantum.Addons.Animator;

    public unsafe class ActionHandlerSystem : SystemMainThreadFilter<ActionHandlerSystem.Filter>
    {
        public struct Filter{
            public EntityRef Entity;
            public Transform3D* Transform;
            public PlayerLink* Link;
            public AnimatorComponent* Animator;
            public CurrentAction* CurrAction;
            public KCC* KCC;
            public PhysicsCollider3D* Collider;
            public CurrentGameStateFlags* GameStateFlags;
            public CurrentStunVals* StunVals;
            //public Damageable* Damageable;
        }
        public override void Update(Frame frame,ref Filter filter)
        {
            var currAction = filter.CurrAction;
            var transform = filter.Transform;
            var attacksData = frame.SimulationConfig.AttackHitboxData;
            var collider = filter.Collider;
            var actionConfigs = frame.SimulationConfig.ActionConfigs;

            if(HitstopTickSystem.GlobalHitstopActive(frame)){
                currAction -> StartTick += 1;
				return; //skip this frame
			}
            
            //Log.Debug("Start Tick is" + CurrentAction->StartTick);
            /*
            ActionType currentActionType = (ActionType)(currAction->ActionType);
            if(currentActionType == ActionType.None || currentActionType == ActionType.Movement){
                return;
            }
            */
            int frameNumber = frame.Number - currAction -> StartTick;


            //handle root motion
            #region root motion
            //handle root motion
            var currentMotion = GetCurrentMotionHelper(frame, filter.Animator);
            var currentAnimFrame = GetCurrentFrameHelper(currentMotion, 0);
            int currentFrame = frame.Number;
            FP deltaTime = frame.DeltaTime;
            int elapsedFrames = currentFrame - currAction->StartTick;
            FP animationTime = elapsedFrames * deltaTime;
            FPVector3 prevFramePos = GetCurrentFrameHelper(currentMotion, animationTime - deltaTime);
            FPVector3 currFramePos = GetCurrentFrameHelper(currentMotion, animationTime);
            FPVector3 deltaFrame = currFramePos - prevFramePos;

            bool transitioning = filter.Animator->IsInTransition(frame, 0);
            if(deltaFrame != new FPVector3(0,0,0) && !transitioning){
                FPVector3 enemyPos = currAction->EnemyPosition;
                FPVector3 dirToTarget = (new FPVector3((FP)enemyPos.X, (FP)currAction->PlayerPosition.Y, (FP)enemyPos.Z) - currAction -> PlayerPosition).Normalized;
                FPQuaternion lookRot = FPQuaternion.LookRotation(dirToTarget, FPVector3.Up);
                FPVector3 rotatedVector = lookRot * deltaFrame;

                FPVector3 nextPosition = transform ->Position + rotatedVector;
                FPVector3 collisionTestDir = nextPosition - transform -> Position;
                var shapeCastHits = frame.Physics3D.ShapeCastAll(transform->Position, transform->Rotation, collider->Shape, collisionTestDir);
                var rayCastHits = frame.Physics3D.RaycastAll(transform->Position, collisionTestDir.Normalized, FP.FromFloat_UNSAFE(0.2f)); //using a magic number for the inner raycast currently
                if (shapeCastHits.Count <= 0 && rayCastHits.Count <= 0){
                    transform -> Position = nextPosition; // safe
                } else {
                    // hit a wall before nextPos, place at hit.Point instead
                    //playerTransform.Position = hit.Point;
                }
            }
            #endregion
            #region Stance Regen
            //regen stance
            
            if(frame.Unsafe.TryGetPointer<Damageable>(filter.Entity, out var damageable)){
                if(damageable -> CurrStance < damageable -> MaxStance){
                    FP HealthPrecentage = damageable -> CurrHealth / damageable -> MaxHealth;
                    //x is min, y is max
                    var RegenMinAndMax = actionConfigs[currAction->ActionIndex].StanceRegenAtEmptyAndFull;
                    damageable -> CurrStance += FPMath.Lerp(RegenMinAndMax.X, RegenMinAndMax.Y, HealthPrecentage);
                    if(damageable -> CurrStance > damageable -> MaxStance){
                        damageable -> CurrStance = damageable -> MaxStance;
                    }
                    frame.Events.BarChange(filter.Link -> Player, damageable -> MaxStance, damageable -> CurrStance, 1);
                }
            }
            
            #endregion
            #region Start Up
            if (currAction->ActionPhase == 1)
            {
                if (frameNumber == 0) {
                    actionConfigs[currAction->ActionIndex].StartupLogicFirstFrame(frame, ref filter, frameNumber);
                }

                if (currAction->StartUpFrames <= 0)
                {
                    currAction->ActionPhase++;
                    actionConfigs[currAction->ActionIndex].ActiveLogicFirstFrame(frame, ref filter, frameNumber);
                }
                else
                {
                    actionConfigs[currAction->ActionIndex].StartupLogic(frame, ref filter, frameNumber);
                    currAction->StartUpFrames--;
                }
                #endregion
                #region Active
            }
            if (currAction->ActionPhase == 2)
            {   
                if (currAction->ActiveFrames <= 0)
                {
                    currAction->ActionPhase++;
                    actionConfigs[currAction->ActionIndex].RecoveryLogicFirstFrame(frame, ref filter, frameNumber);
                }
                else
                {
                    actionConfigs[currAction->ActionIndex].ActiveLogic(frame, ref filter, frameNumber);
                    currAction->ActiveFrames--;
                }
                #endregion
                #region EndLag
            }
            if (currAction->ActionPhase == 3)
            {   

                if (currAction->RecoveryFrames <= 0)
                {
                    actionConfigs[currAction->ActionIndex].CancelableLogicFirstFrame(frame, ref filter, frameNumber);
                    currAction->ActionPhase++;
                    //Log.Debug("Action Cancelable");
                }else{
                    actionConfigs[currAction->ActionIndex].RecoveryLogic(frame, ref filter, frameNumber);
                    currAction->RecoveryFrames--;
                }
                #endregion
                #region Cancelable
            }
            if (currAction->ActionPhase == 4)
            {
                if (currAction->CancelableFrames <= 0)
                {
                    //Log.Debug("Action Over");
                    currAction->ActionPhase++;
                }else{
                    actionConfigs[currAction->ActionIndex].CancelableLogic(frame, ref filter, frameNumber);
                    currAction->CancelableFrames--;
                }
            }
            #endregion
                
        }

        public int GetAttackDataFromEnum(AttackName attackName, List<QAttackData> attackData){
            for(int i = 0; i < attackData.Count; i++){
                if(attackData[i].AttackVals.attackName == attackName){
                    return i;
                }
            }
            Log.Error("No attack by that name found");
            return 0;
        }
        private AnimatorMotion GetCurrentMotionHelper(Frame f, AnimatorComponent* animator)
        {
            // Get the current state using the current state ID
            AnimatorState currentState = animator->GetCurrentState(f, 0);
            if (currentState == null)
                Log.Error("Current state not found.");
            else{
                //Log.Debug(currentState);
            }
            
            var currentMotion = currentState.Motion;

            return currentMotion;
        }
        private FPVector3 GetCurrentFrameHelper(AnimatorMotion motion, FP animatorTime)
        {
            //root mototion not yet handled for blend trees
            if (motion is AnimatorClip clip)
            {
                // Retrieve the animator data for the clip
                var data = clip.Data;
                if(data.DisableRootMotion){
                    return new FPVector3(0,0,0);
                }else{
                    // Get the frame at the current time
                    return data.GetFrameAtTime(animatorTime).Position;
                }
            }
            return new FPVector3(0,0,0);
        }

    }

    
}
