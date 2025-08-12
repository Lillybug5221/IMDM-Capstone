namespace Quantum.Addons.Animator
{
  using System;
  using Photon.Deterministic;

  /// <summary>
  /// AnimatorTimeWindowEvent will call its Execute method every time the current evaluated time is greater than the Time and less than EndTime.
  /// OnEnter will be called on the first valid evaluation. OnExit will be called on the last valid evaluation.
  /// </summary>
  [Serializable]
  public unsafe class AnimatorTimeWindowEvent : AnimatorEvent
  {
    public FP EndTime;

    /// <inheritdoc cref="AnimatorEvent.Evaluate"/>
    public override bool Evaluate(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
      AnimatorTimeWindowEventAsset eventAsset = f.FindAsset(AssetRef) as AnimatorTimeWindowEventAsset;
      if (layerData->Time >= Time && layerData->Time <= EndTime)
      {
        eventAsset.Execute(f, animatorComponent, layerData);

        if (base.Evaluate(f, animatorComponent, layerData))
        {
          eventAsset.OnEnter(f, animatorComponent, layerData);
          return true;
        }
      }
      else if (layerData->Time >= EndTime && layerData->LastTime < EndTime)
      {
        eventAsset.OnExit(f, animatorComponent, layerData);
      }

      return false;
    }

    /// <inheritdoc cref="AnimatorEvent.GetInspectorStringFormat"/>
    public override string GetInspectorStringFormat()
    {
      return $"Event: {GetType().Name}; Start-Time: {Time}, End-Time: {EndTime}";
    }
  }
}