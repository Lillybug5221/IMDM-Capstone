namespace Quantum.Addons.Animator
{
  using System;

  /// <summary>
  /// AnimatorInstantEvent will call it's Execute method once Evaluate is valid.
  /// </summary>
  [Serializable]
  public unsafe class AnimatorInstantEvent : AnimatorEvent
  {
    /// <inheritdoc cref="AnimatorEvent.Evaluate"/>
    public override bool Evaluate(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
      if (base.Evaluate(f, animatorComponent, layerData))
      {
        AnimatorEventAsset eventAsset = f.FindAsset(AssetRef);
        eventAsset.Execute(f, animatorComponent, layerData);
        return true;
      }

      return false;
    }
  }
}