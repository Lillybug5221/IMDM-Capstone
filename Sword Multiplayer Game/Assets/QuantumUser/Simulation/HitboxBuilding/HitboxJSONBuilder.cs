using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Photon.Deterministic;
using Quantum;

[System.Serializable]
public class TransformData {
    public Attack attackData;
    public List<Vector3> basePositions = new List<Vector3>();
    public List<Vector3> endPositions = new List<Vector3>();
    public List<FP> radii = new List<FP>();
    public List<FP> lengths = new List<FP>();
    public List<int> frames = new List<int>();
}


public class HitboxJSONBuilder : MonoBehaviour
{
    public static HitboxJSONBuilder Instance;

    public string FileName;
    [SerializeField]
    public ActionConfigAsset ActionToBuild;
    public bool CreateJSON;
    public FP Radius;
    public FP Length;
    
    
    

    TransformData data;

    public void Start(){
        data = new TransformData();
        Instance = this;
    }

    public void AddToLists(Vector3 basePos, Vector3 endPos, int frame){
        data.basePositions.Add(basePos);
        data.endPositions.Add(endPos);
        data.radii.Add(Radius);
        data.lengths.Add(Length);
        data.frames.Add(frame);
    }

    public void Save() {
        Debug.Log("Saving");
        string json = JsonUtility.ToJson(data, true);

        string path = Path.Combine(Application.dataPath, FileName +".json");
        File.WriteAllText(path, json);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log("Saved JSON:\n" + json);
    }

}
