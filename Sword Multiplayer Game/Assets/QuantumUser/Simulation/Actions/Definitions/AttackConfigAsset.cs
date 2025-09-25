using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Quantum
{
    public unsafe class AttackConfigAsset : ActionConfigAsset
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
            return;
        }
    }
}
