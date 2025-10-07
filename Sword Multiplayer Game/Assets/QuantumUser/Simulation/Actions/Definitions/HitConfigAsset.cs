using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;
using System.Runtime.Versioning;
using Quantum;
using Quantum.Addons.Animator;
namespace Quantum
{
    public unsafe class HitConfigAsset : ActionConfigAsset
    {
        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            //setup animation
            AnimatorComponent.SetTrigger(frame, filter.Animator, "Hit_Stagger");
            AddGlobalHitstop(frame, 3, 3);
            return;
        }

        public override void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter){
            return;
        }

        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
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

        public void AddGlobalHitstop(Frame f, int frames, int delayFrames){
            var globalEntity = Globals.Get(f);
            var ghs = f.Get<GlobalHitstop>(globalEntity);

            if (frames > ghs.FramesLeft) {
                ghs.FramesLeft = frames;
                ghs.DelayLeft = delayFrames;
            }

            f.Set(globalEntity, ghs);
        }

        #endregion
    }
}
