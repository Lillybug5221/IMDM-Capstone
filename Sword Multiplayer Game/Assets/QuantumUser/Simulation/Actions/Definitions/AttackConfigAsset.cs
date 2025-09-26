using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Quantum
{
    public unsafe class AttackConfigAsset : ActionConfigAsset
    {
        public string AttackName;

        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            AnimatorComponent.SetTrigger(frame, filter.Animator, AttackName);
        }

        public override void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter){
            return;
        }

        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            return;
        }
        public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            Log.Debug("attacking active");
        }
        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            return;
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter){
            return;
        }
    }
}
