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
    public unsafe class HeavyParryConfigAsset : ActionConfigAsset
    {

        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            //setup animation
            AnimatorComponent.SetTrigger(frame, filter.Animator, "Heavy_Parry");
            Log.Debug("Heavy Parrying");
            var currAction = filter.CurrAction;

            var clampedDirection = ClampDirection(currAction -> Direction);
            Log.Debug("Heavy parry dir is " + clampedDirection);

            var parry = new ParryComponent()
            {
                HeavyParry = true,
                HeldBlock = false,
                Direction = clampedDirection
            };
            frame.Add(filter.Entity, parry);
            return;

        }

        public override void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter){
            if (frame.TryGet<ParryComponent>(filter.Entity, out var parryComponent))
            {
                frame.Remove<ParryComponent>(filter.Entity);
            }
            else
            {
                Log.Error("Parry Component Unexpectedly Disappeared");
            }
            return;
        }

        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }

        public override void RecoveryLogicFirstFrame(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            if (frame.TryGet<ParryComponent>(filter.Entity, out var parryComponent))
            {
                frame.Remove<ParryComponent>(filter.Entity);
            }
            else
            {
                Log.Error("Parry Component Unexpectedly Disappeared");
            }
            return;
        }

        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }

        private FPVector2 ClampDirection(FPVector2 v){
            if(v == new FPVector2(FP._0,FP._0)){
                return new FPVector2(FP._0, FP._1);
            }

            if (FPMath.Abs(v.X) > FPMath.Abs(v.Y))
            {
                if (v.X > FP._0)
                    return new FPVector2(FP._1, FP._0);   // Right
                else
                    return new FPVector2(-FP._1, FP._0);  // Left
            }
            else
            {
                if (v.Y > FP._0)
                    return new FPVector2(FP._0, FP._1);   // Up
                else
                    return new FPVector2(FP._0, -FP._1);  // Down
            }
        }
      
    }
}
