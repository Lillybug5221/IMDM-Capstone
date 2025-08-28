using UnityEngine;
using Photon.Deterministic;
using System;

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
}



