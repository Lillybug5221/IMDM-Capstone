using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class TransformData {
    public Attack attackData;
    public List<Vector3> positions = new List<Vector3>();
    public List<Quaternion> rotations = new List<Quaternion>();
}


public class HitboxJSONBuilder : MonoBehaviour
{
    public static HitboxJSONBuilder Instance;

    public string FileName;
    public Attack attackData = new Attack();
    [SerializeField]
    public string TestingAnimationName;
    public bool CreateJSON;
    
    
    

    TransformData data;

    public void Start(){
        data = new TransformData();
        data.attackData = attackData;
        Instance = this;
    }

    public void AddToLists(Vector3 pos, Quaternion rot){
        data.positions.Add(pos);
        data.rotations.Add(rot);
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
