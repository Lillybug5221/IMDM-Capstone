using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class TransformData {
    public Attack attackData;
    public List<Vector3> positions = new List<Vector3>();
    public List<Vector3> rotations = new List<Vector3>();
    public List<int> frames = new List<int>();
}


public class HitboxJSONBuilder : MonoBehaviour
{
    public static HitboxJSONBuilder Instance;

    public string FileName;
    [SerializeField]
    public string TestingAnimationName;
    public bool CreateJSON;
    
    
    

    TransformData data;

    public void Start(){
        data = new TransformData();
        Instance = this;
    }

    public void AddToLists(Vector3 pos, Vector3 rotEuler, int frame){
        data.positions.Add(pos);
        data.rotations.Add(rotEuler);
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
