namespace Quantum.Addons.Animator
{
  using System;
  using Photon.Deterministic;

  /// <summary>
  /// The AnimatorEvent abstract class declaration.
  /// </summary>
  [Serializable]
  public unsafe abstract class AnimatorEvent
  {
    /// <summary>
    /// Reference to the AnimatorEventAsset containing the event bake and Execution routine.
    /// </summary>
    public AssetRef<AnimatorEventAsset> AssetRef;

    /// <summary>
    /// Time of the clip that will Execute the event, this value is baked during AnimatorGraph import process.
    /// </summary>
    public FP Time;

    /// <summary>
    /// Called every time that an AnimationMotion with this Event is updated.
    /// </summary>
    /// <param name="f">The Quantum Game Frame.</param>
    /// <param name="animatorComponent">The AnimatorComponent being evaluated.</param>
    /// <param name="currentTime">Time of the AnimatorMotion.</param>
    public virtual bool Evaluate(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
      //Check if it's time to fire the Instant event.
      //Check if it's the first Update of the clip, for cases where the event Time is 0.
      if (layerData->Time >= Time && layerData->LastTime <= Time
          || layerData->LastTime > layerData->Time && layerData->Time >= Time)
      {
        return true;
      }

      return false;
    }

    /// <summary>
    /// Returns the custom data string for this event type.
    /// </summary>
    public virtual string GetInspectorStringFormat()
    {
      return $"Event: {GetType().Name}; Time: {Time}";
    }
  }
}