using UnityEngine;
using Quantum;
using Photon.Deterministic;
using System.Collections.Generic;

// Quantum deterministic format
namespace Quantum {
    [System.Serializable]
    public struct QHitboxData {
        public FPVector3 Position;
        public FPQuaternion Rotation;
    }
    [System.Serializable]
    public struct QAttackData{
        public List<QHitboxData> Hitboxes;
        public Attack AttackVals;
    }
    
}