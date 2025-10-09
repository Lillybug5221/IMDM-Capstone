using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;
using System.Runtime.Versioning;
using Quantum;
using Quantum.Addons.Animator;
namespace Quantum
{
    public unsafe class BlockStateConfigAsset : ActionConfigAsset
    {
        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            Log.Debug("blocking");
            //setup animation
            AnimatorComponent.SetTrigger(frame, filter.Animator, "Block_Hold");

            //remove parry component
            if (frame.TryGet<ParryComponent>(filter.Entity, out var parryComponent))
            {
                frame.Remove<ParryComponent>(filter.Entity);
            }
            else
            {
                Log.Error("Parry Component Unexpectedly Disappeared");
            }
            //add block component
            var parry = new ParryComponent()
            {
                HeavyParry = false,
                HeldBlock = true,
                Direction = new FPVector2(0, 0)
            };
            frame.Add(filter.Entity, parry);
            
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
    }
}
