using UnityEngine;
using Quantum;
using Photon.Deterministic;
using System.Collections.Generic;
using System;

// Quantum deterministic format
namespace Quantum {
    [System.Serializable]
    public struct QHitboxData {
        public ushort FrameNum;
        public FPVector3 BasePosition;
        public FPVector3 EndPosition;
        public FP Length;
        public FP Radius;
    }
    [System.Serializable]
    public struct QAttackData{
        public List<QHitboxData> Hitboxes; //this being a list instead of a dictionary with frame numebr lookup is because I'm just trying to get it to work for now, optimize later
        public Attack AttackVals;
    }

    [Serializable]
    public struct Attack{
        public AttackName attackName;
        public FPVector2 direction;
        public AttackType attackType;
        public int startupFrames;
        public int activeFrames;
        public int endlagFrames;
        public int cancelableFrames;
        public int damage;
        public List<CancelabilityData> specialCancels;
    }

    [Serializable]
    public struct CancelabilityData{

    }


    [Serializable]
    public enum AttackName{
        Light_U,
        Light_UR,
        Light_R,
        Light_DR,
        Light_D,
        Light_DL,
        Light_L,
        Light_UL,
        Heavy_U,
        Heavy_UR,
        Heavy_R,
        Heavy_DR,
        Heavy_D,
        Heavy_DL,
        Heavy_L,
        Heavy_UL
    }

    [Serializable]
    public enum AttackType{
        Light,
        Heavy,
        Special
    }
    
}