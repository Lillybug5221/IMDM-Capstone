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
        Jump,
        Dodge,
        Attack,
        Parry,
        Stun
    }

    [System.Flags]
    public enum GameStateFlags {
        None = 0,
        IsJumping = 1 << 0,
        IsAttacking = 1 << 1,
        IsGrounded = 1 << 2,
        IsInvulnerable = 1 << 3,
        IsDirectionalInput = 1 << 4,
        IsDodging = 1 << 5,
        IsParrying = 1 << 6,
        IsHeavyParrying = 1 << 7,
        // etc...
    }


    public class ActionConfigAsset : AssetObject
    {
        public string ActionName;

        public int StartUpFrames;
        public int ActiveFrames;
        public int RecoveryFrames;
        public int AnimationEndFrames;

        public GameStateFlags RequiredFlags;
        public CancelRule[] CustomCancelRules;
        /*
        public ActionType Type;
        public AttackData AttackData;
        */

    }

    [System.Serializable]
    public struct CancelRule {
        public string TargetAction; // reference to other actions
        public GameStateFlags RequiredFlags;
        public bool useSpecializedCancelFrame;
        public int SpecializedCancelabilityStartFrame;
        public int SpecializedCancelabilityEndFrame;
        public int CancelablePhase;
    }

    
}
