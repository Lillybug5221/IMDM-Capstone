using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;
using System.Runtime.Versioning;
using Quantum;
using Quantum.Addons.Animator;
using Quantum.Physics3D;


namespace Quantum
{
    public unsafe class StunConfigAsset : ActionConfigAsset
    {
        public SimCurve PushbackSimCurve;
        public int HitStopStartupFrames;
        public int HitStopActiveFrames;
        
        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            
           
            string AnimationName = "Hit_Stagger";
            var currStun = filter.StunVals;
            var currAction = filter.CurrAction;

            if(frame.Unsafe.TryGetPointer<Damageable>(filter.Entity, out var damageable) && (KnockBackType) currStun -> KnockbackType != KnockBackType.StanceBreak){
                Log.Debug("Invincible Set True");
                damageable -> Invincible = true;
            }
            
            //play the right animation
            if((KnockBackType)currStun -> KnockbackType == KnockBackType.InPlaceStagger){
                AnimationName = "Hit_Stagger";
            }else if((KnockBackType)currStun -> KnockbackType == KnockBackType.GroundSplat){
                AnimationName = "Ground_Splat";
            }else if((KnockBackType)currStun -> KnockbackType == KnockBackType.FlyBack){
                AnimationName = "Fly_Back";
            }else if((KnockBackType)currStun -> KnockbackType == KnockBackType.Parry){
                AnimationName = "Parry_Deflect";
            }else if((KnockBackType)currStun -> KnockbackType == KnockBackType.Block){
                AnimationName = "Block_Stagger";
            }else if((KnockBackType)currStun -> KnockbackType == KnockBackType.GuardBreak){
                AnimationName = "Heavy_Parry_Stagger";
            }else if((KnockBackType)currStun -> KnockbackType == KnockBackType.StanceBreak){
                AnimationName = "Stance_Break";
            }
            AnimatorComponent.SetTrigger(frame, filter.Animator, AnimationName);
            //overwrite the recovery framees of currAction
            currAction -> RecoveryFrames = currStun -> StunTime;
            //addHitstop
            AddGlobalHitstop(frame, HitStopActiveFrames, HitStopStartupFrames);
            return;
        }

        public override void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter){
            if(frame.Unsafe.TryGetPointer<Damageable>(filter.Entity, out var damageable)){
                damageable -> Invincible = false;
                Log.Debug("Invincible Set False");
            }
            return;
        }
        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            
            var currAction = filter.CurrAction;
            var currStun = filter.StunVals;
            var transform = filter.Transform;
            var collider = filter.Collider;

            if(currStun -> StunTime == 0){
                return;
            }
            //directly transform the position along the inputed direction. check for collisions before applying each frame.
            //for each frame, grab the action completeness precent and pass that through an animation curve to get the completed distance precentage and set the player position between a 
            //roll start positon and end position at that lerp value. always draw a line between the start position and the target position, if there is a collision with something, stop there.
            //Setup endpos on startup
            if (frameNumber == 0)
            {
                FPVector2 dashDirection2D = new FPVector2(0,-1);
                FP dashMagnitude = currStun -> KnockbackDistance;
                FPVector3 dashDirectionWorld = new FPVector3(dashDirection2D.X, 0, dashDirection2D.Y);

                //maybe pass a player start position into the action instead of using the current position
                FPVector3 dirToTarget = (new FPVector3((FP)currAction->EnemyPosition.X, (FP)currAction->PlayerPosition.Y, (FP)currAction->EnemyPosition.Z) - currAction->PlayerPosition).Normalized;
                FPQuaternion lookRot = FPQuaternion.LookRotation(dirToTarget, FPVector3.Up);
                FPVector3 rotatedVector = lookRot * dashDirectionWorld;
                //Log.Debug(dashDirectionWorld + "rotated is" + rotatedVector);
                FPVector3 endPosition = currAction->PlayerPosition + (rotatedVector * dashMagnitude);
                var hits = (frame.Physics3D.LinecastAll(currAction->PlayerPosition, endPosition)).ToArray();
                Hit3D? foundHit = null;
                foreach(var h in hits){
                    if(h.Entity != filter.Entity){
                        foundHit = h;
                        break;
                    }
                }
                currAction->DashEndPos = endPosition;
                if (!foundHit.HasValue)
                {
                    currAction->PrecentageOfDodgeCompletable = (FP)1;
                }
                else
                {
                    FP TotalDistance = (endPosition - currAction->PlayerPosition).Magnitude;
                    FP CompletableDistance = (foundHit.Value.Point - currAction->PlayerPosition).Magnitude;
                    FP Ratio = (CompletableDistance / TotalDistance);
                    //this is a hack to stop clipping when a dash is initated into a wall when they are already toucing.
                    if (Ratio < FP.FromFloat_UNSAFE(0.2f)) { Ratio = 0; }
                    currAction->PrecentageOfDodgeCompletable = Ratio;
                }

            }

            FP t = (FP)frameNumber / (FP)(currAction->RecoveryFrames + frameNumber);
            SimCurve dashCurve = PushbackSimCurve;
            t = dashCurve.Evaluate(t);
            if (t <= currAction->PrecentageOfDodgeCompletable)
            {
                FPVector3 nextPosition = FPVector3.Lerp(currAction->PlayerPosition, currAction->DashEndPos, t);
                FPVector3 collisionTestDir = nextPosition - transform->Position;

                var hits = frame.Physics3D.ShapeCastAll(transform->Position, transform->Rotation, collider->Shape, collisionTestDir);
                if (hits.Count <= 0)
                {
                    transform->Position = nextPosition; // safe
                }
                else
                {
                    // hit a wall before nextPos, place at hit.Point instead
                    //playerTransform.Position = hit.Point;
                }
            }

            //no stance damage when stance broken hit
            if(frame.Unsafe.TryGetPointer<Damageable>(filter.Entity, out var damageable) && (KnockBackType) currStun -> KnockbackType == KnockBackType.StanceBreak){
                Log.Debug("disable stance damage");
                filter.GameStateFlags->Flags |= (int) GameStateFlags.IsStanceBroken;
            }
        }

        public override void CancelableLogicFirstFrame(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            if(frame.Unsafe.TryGetPointer<Damageable>(filter.Entity, out var damageable)){
                damageable -> Invincible = false;
                Log.Debug("Invincible Set false");
            }
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }

        public void AddGlobalHitstop(Frame f, int frames, int delayFrames){
            var globalEntity = Globals.Get(f);
            var ghs = f.Get<GlobalHitstop>(globalEntity);

            if (frames > ghs.FramesLeft) {
                ghs.FramesLeft = frames;
                ghs.DelayLeft = delayFrames;
            }

            f.Set(globalEntity, ghs);
        }
      
    }
}
