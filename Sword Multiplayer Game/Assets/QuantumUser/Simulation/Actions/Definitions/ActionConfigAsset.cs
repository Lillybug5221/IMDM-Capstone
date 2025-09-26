using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Quantum
{
    [Serializable]
    public enum ActionType{
        None,
        Movement,
        Dodge,
        LightAttack,
        HeavyAttack,
        Parry,
        HeavyParry,
        Stun
    }

    [System.Flags]
    public enum GameStateFlags {
        None = 0,
        IsDirectionalInput = 1 << 0,
        IsLightAttacking = 1 << 1,
        IsHeavyAttacking = 1 << 2,
        IsDodging = 1 << 3,
        IsParrying = 1 << 4,
        IsHeavyParrying = 1 << 5,
        //CurrentActionPhase1 = 1 << 6,
        //CurrentActionPhase2 = 1 << 7,
        //CurrentActionPhase3 = 1 << 8,
        //CurrentActionPhase4 = 1 << 9,
        //CurrentActionPhase5 = 1 << 10,
    }


    public unsafe abstract class ActionConfigAsset : AssetObject
    {
        //public ActionType ActionType;

        public int StartUpFrames;
        public int ActiveFrames;
        public int RecoveryFrames;
        public int CancelableFrames;

        public GameStateFlags RequiredFlags;
        public GameStateFlags ForbiddenFlags;

        public CancelRule[] CustomCancelRules;

        public abstract void Initialize(Frame frame, ref ActionStateMachine.Filter filter);
        public abstract void StartupLogic(Frame frame,ref ActionHandlerSystem.Filter filter);
        public abstract void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter);
        public abstract void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter);
        public abstract void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter);
        public abstract void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter);
        

    }

    [System.Serializable]
    public struct CancelRule {
        public ActionConfigAsset TargetAction; // reference to other actions
        public bool useSpecializedCancelFrame;
        public int SpecializedCancelabilityStartFrame;
        public int SpecializedCancelabilityEndFrame;
        public int CancelablePhase;
    }

    
}
