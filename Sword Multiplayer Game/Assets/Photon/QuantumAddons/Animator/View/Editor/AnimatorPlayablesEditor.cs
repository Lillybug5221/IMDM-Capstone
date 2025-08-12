using UnityEngine.EventSystems;

namespace Quantum.Addons.Animator
{
  using UnityEditor;
  using UnityEngine;
  using Quantum;

  [CustomEditor(typeof(AnimatorPlayables))]
  public unsafe class AnimatorPlayablesEditor : AnimatorUpdaterEditor
  {
    void OnEnable()
    {
      _animatorUpdater = (AnimatorPlayables)target;
    }
  }
}