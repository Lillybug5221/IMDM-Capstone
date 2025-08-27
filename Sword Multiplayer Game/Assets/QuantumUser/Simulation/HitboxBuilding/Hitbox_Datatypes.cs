using UnityEngine;
using Quantum;
using Photon.Deterministic;
using System.Collections.Generic;

// Quantum deterministic format
namespace Quantum {
    [System.Serializable]
    public struct QHitboxData {
        public ushort frameNum;
        public FPVector3 Position;
        public FPVector3 RotationEuler;
        public FP Height;
        public FP Radius;
    }
    [System.Serializable]
    public struct QAttackData{
        public List<QHitboxData> Hitboxes; //this being a list instead of a dictionary with frame numebr lookup is because I'm just trying to get it to work for now, optimize later
        public Attack AttackVals;
    }
    
}