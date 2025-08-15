using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Quantum
{
    [CreateAssetMenu(menuName = "Quantum/Spawn Point Asset")]
    public class SpawnPointAsset : ScriptableObject {
        public SpawnPointData[] hitboxPoint; // Unity-friendly structs
    }

    [System.Serializable]
    public struct SpawnPointData {
        public Vector3 position;
        public Quaternion rotation;
    }
}
