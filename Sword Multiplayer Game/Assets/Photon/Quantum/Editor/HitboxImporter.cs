using UnityEditor;
using UnityEngine;
using Quantum;
using System.IO;
using Photon.Deterministic;
using System.Collections.Generic;
#if UNITY_EDITOR

namespace Quantum.Editor {
    public static class HitboxImporter {
        [MenuItem("Quantum/Import Hitboxes From JSON")]
        
        public static void ImportFromJson() {
            string path = EditorUtility.OpenFilePanel("Select Attack Hitboxes JSON", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            string json = File.ReadAllText(path);
            TransformData wrapper = JsonUtility.FromJson<TransformData>(json);

            // Load the existing SimulationConfig asset
            var config = FindSimulationConfig();

            if (config == null) {
                Debug.LogError("SimulationConfig.asset not found. Generate DB first.");
                return;
            }

            // Convert to deterministic format
            var newAttackData = new QAttackData();
            newAttackData.AttackVals = wrapper.attackData;
            newAttackData.Hitboxes = new List<QHitboxData>();
            for (int i = 0; i < wrapper.positions.Count; i++) {
                var temp = new QHitboxData();
                temp.frameNum = (ushort)wrapper.frames[i];
                temp.Position = new FPVector3(FP.FromFloat_UNSAFE(wrapper.positions[i].x), FP.FromFloat_UNSAFE(wrapper.positions[i].y), FP.FromFloat_UNSAFE(wrapper.positions[i].z));
                temp.RotationEuler = new FPVector3(FP.FromFloat_UNSAFE(wrapper.rotations[i].x), FP.FromFloat_UNSAFE(wrapper.rotations[i].y), FP.FromFloat_UNSAFE(wrapper.rotations[i].z));
                newAttackData.Hitboxes.Add(temp);
            }
            config.AttackHitboxData.Add(newAttackData);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"Imported Attack Data into Sim Config");
        }

        public static SimulationConfig FindSimulationConfig() {
            // Search all assets of type SimulationConfig
            string[] guids = AssetDatabase.FindAssets("t:SimulationConfig");
            if (guids.Length == 0) {
                Debug.LogError("No SimulationConfig asset found in project.");
                return null;
            }

            // Take the first one found
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SimulationConfig>(path);
        }
        
    }

    
}
#endif
