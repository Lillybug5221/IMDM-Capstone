using UnityEngine.EventSystems;

namespace Quantum.Addons.Animator
{
  using UnityEditor;
  using UnityEngine;
  using Quantum;

  [CustomEditor(typeof(AnimatorMecanim))]
  public unsafe class AnimatorMecanimEditor : AnimatorUpdaterEditor
  {
    void OnEnable()
    {
      _animatorUpdater = (AnimatorMecanim)target;
    }
  }
}