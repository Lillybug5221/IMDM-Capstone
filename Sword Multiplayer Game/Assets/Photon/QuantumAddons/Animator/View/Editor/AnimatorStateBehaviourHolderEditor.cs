namespace Quantum.Addons.Animator
{
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEditor;

  [CustomEditor(typeof(AnimatorStateBehaviourHolder))]
  public class AnimatorStateBehaviourHolderEditor : Editor
  {
    private Dictionary<AssetRef<AnimatorStateBehaviour>, Editor> _behaviorEditorDictionary;

    private void OnEnable()
    {
      // Clears the dictionary
      _behaviorEditorDictionary = new Dictionary<AssetRef<AnimatorStateBehaviour>, Editor>();
    }

    public override void OnInspectorGUI()
    {
      base.OnInspectorGUI();

      var holder = target as AnimatorStateBehaviourHolder;
      if (holder.AnimatorStateBehaviourAssets == null)
        return;

      // Clamps the index of the state we are going to be editting.
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Previous"))
        holder.Index--;
      holder.Index = EditorGUILayout.DelayedIntField("Behaviour Index", holder.Index);
      if (GUILayout.Button("Next"))
        holder.Index++;
      holder.Index = Mathf.Clamp(holder.Index, 0, holder.AnimatorStateBehaviourAssets.Length - 1);
      EditorGUILayout.EndHorizontal();

      var behaviourRef = holder.AnimatorStateBehaviourAssets[holder.Index];
      if (behaviourRef == null)
      {
        EditorGUILayout.HelpBox($"No behavior defined at index {holder.Index}", MessageType.Warning);
        return;
      }

      if (!_behaviorEditorDictionary.TryGetValue(behaviourRef, out var behaviourEditor))
      {
        var behaviour = QuantumUnityDB.GetGlobalAsset(behaviourRef);
        _behaviorEditorDictionary.Add(behaviourRef, Editor.CreateEditor(behaviour));
        behaviourEditor = _behaviorEditorDictionary[behaviourRef];
      }

      behaviourEditor.DrawHeader();
      behaviourEditor.OnInspectorGUI();
    }

    private void OnDisable()
    {
      if (_behaviorEditorDictionary == null)
        return;

      foreach (var editor in _behaviorEditorDictionary.Values)
      {
        if (editor == null)
          continue;
        DestroyImmediate(editor);
      }

      _behaviorEditorDictionary = null;
    }
  }
}
