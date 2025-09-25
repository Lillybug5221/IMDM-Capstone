using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;
using System.Runtime.Versioning;
using Quantum;
using Quantum.Addons.Animator;
namespace Quantum
{
    public unsafe class MoveConfigAsset : ActionConfigAsset
    {
        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            return;
        }
        public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            return;
        }
        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            return;
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            Log.Debug("walking");
            
            KCC* kcc = filter.KCC;
            var collider = filter.Collider;
            Transform3D* transform = filter.Transform;
            var currAction = filter.CurrAction;

            //set rotation to action saved enemy position
            FPVector3 playerPosition = filter.Transform->Position;
            FPVector3 opponentPosition = currAction -> EnemyPosition;

            FPVector3 forwardDir = opponentPosition - playerPosition;
            forwardDir.Y = FP._0;
            forwardDir = FPVector3.Normalize(forwardDir);

            //face player towards opponent
            FPQuaternion targetRotation = FPQuaternion.LookRotation(forwardDir, FPVector3.Up);
            FP rotationSpeed = FP._1;
            FPQuaternion currentRotation = filter.Transform->Rotation;  
            FPQuaternion slerpedRotation = FPQuaternion.Slerp(currentRotation, targetRotation, rotationSpeed);
            filter.Transform->Rotation = slerpedRotation;

            //hitstop probably doesn't need to be handled here, it could be handled just in the actionhandler
            #region hitstop
            //return if hitstop
            if(HitstopTickSystem.GlobalHitstopActive(frame)){
                kcc->SetInputDirection(new FPVector3(0,0,0));
                return;
            }
            #endregion


            #region movement
            //check current action to see if movement is possible
            //read directional input
            FPVector2 moveDirection = new FPVector2(0,0);
            moveDirection = currAction->Direction;
            
            //apply movement
            FPVector3 moveDir = GetMovementDirection(moveDirection, forwardDir);
            kcc->SetInputDirection(moveDir);
            
        }
        #endregion


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
