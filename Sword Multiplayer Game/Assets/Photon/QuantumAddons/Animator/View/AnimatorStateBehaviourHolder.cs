namespace Quantum.Addons.Animator
{
  using UnityEngine;

  public class AnimatorStateBehaviourHolder : StateMachineBehaviour
  {
    public AssetRef<AnimatorStateBehaviour>[] AnimatorStateBehaviourAssets;

    [HideInInspector()] public int Index;
  }
}