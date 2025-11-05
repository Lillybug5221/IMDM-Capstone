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
    public unsafe class DodgeConfigAsset : ActionConfigAsset
    {

        public FP dodgeDistance;
        public SimCurve DashSimCurve;

        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            var currAction = filter.CurrAction;
            //setup animation
            AnimatorComponent.SetFixedPoint(frame, filter.Animator, "MoveX", currAction -> Direction.X);
            AnimatorComponent.SetFixedPoint(frame, filter.Animator, "MoveY", currAction -> Direction.Y);
            AnimatorComponent.SetTrigger(frame, filter.Animator, "Dodge");
            return;
        }

        public override void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter){
            return;
        }

        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            var currAction = filter.CurrAction;
            var transform = filter.Transform;
            var collider = filter.Collider;
            //directly transform the position along the inputed direction. check for collisions before applying each frame.
            //for each frame, grab the action completeness precent and pass that through an animation curve to get the completed distance precentage and set the player position between a 
            //roll start positon and end position at that lerp value. always draw a line between the start position and the target position, if there is a collision with something, stop there.
            //Setup endpos on startup
            if (frameNumber == 0)
            {

                FPVector2 dashDirection2D = (currAction->Direction).Normalized;
                if(dashDirection2D.Magnitude < FP.FromFloat_UNSAFE(0.001f)){dashDirection2D = new FPVector2(0,-1);}
                FP dashMagnitude = dodgeDistance;
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

            FP t = (FP)frameNumber / (FP)(currAction->ActiveFrames + frameNumber);
            SimCurve dashCurve = DashSimCurve;
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
        }
        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
      


        #region helper functons
        public static FPVector3 GetMovementDirection(FPVector2 inputDirection, FPVector3 forward) {
            // Assume up is Y-up (0,1,0)
            FPVector3 up = FPVector3.Up;

            // Calculate right vector = cross(up, forward)
            FPVector3 right = FPVector3.Cross(up, forward).Normalized;

            // Flatten forward to horizontal plane (project on plane)
            FPVector3 flatForward = FPVector3.ProjectOnPlane(forward, up).Normalized;

            // Combine input: forward * input.y + right * input.x
            FPVector3 moveDir = flatForward * inputDirection.Y + right * inputDirection.X;
            //Log.Debug(inputDirection +","+forward + "," + right);
            // Normalize if needed
            /*
            if (moveDir.MagnitudeSquared > FP._1) {
                moveDir = moveDir.Normalized;
            }
            */

            return moveDir;
        }
        #endregion
    }
}
