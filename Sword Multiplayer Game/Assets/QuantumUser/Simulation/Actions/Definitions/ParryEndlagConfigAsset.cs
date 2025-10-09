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
    public unsafe class ParryEndlagConfigAsset : ActionConfigAsset
    {

        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            //setup animation
            Log.Debug("parry endlag");
            AnimatorComponent.SetTrigger(frame, filter.Animator, "Parry_Endlag");
            if (frame.TryGet<ParryComponent>(filter.Entity, out var parryComponent))
            {
                frame.Remove<ParryComponent>(filter.Entity);
            }
            else
            {
                Log.Error("Parry Component Unexpectedly Disappeared");
            }
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
      
    }
}
