using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;

namespace Quantum {

    
    
    public interface InputStruct { 
        FPVector2 Direction {get; set;}
    }

    public struct MovementStruct : InputStruct {
        public FPVector2 Direction {get; set; }
        public bool sprinting {get; set; }
    }

    public interface ActionStruct : InputStruct {
        bool ActionInitiated {get; set; }
    }

    public struct AttackStruct : ActionStruct {
        public FPVector2 Direction {get; set; }
        public AttackName AttackName {get; set; }
        public bool ActionInitiated {get; set; }
    }

    public struct ParryStruct : ActionStruct {
        public FPVector2 Direction {get; set; }
        public bool ActionInitiated {get; set; }
    }
    
}
