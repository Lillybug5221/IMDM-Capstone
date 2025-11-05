namespace Quantum
{
  using Addons.Animator;
  using System;
  using UnityEngine;

  /// <summary>
  /// This is a sample of how to use SampleTimeWindowEvent events. Use it as a base to create a new class inheriting from AnimatorInstantEventAsset and
  /// implement a custom logic on Execute method 
  /// </summary>
  [Serializable]
  public class ExampleTimeWindowEventAsset : AnimatorTimeWindowEventAsset
  {
    public override unsafe void OnEnter(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
      Debug.Log($"[Quantum Animator ({f.Number})] OnEnter animator time window event.");
    }
    
    public override unsafe void Execute(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
      Debug.Log($"[Quantum Animator ({f.Number})] Execute animator time window event.");
    }

    public override unsafe void OnExit(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData)
    {
      Debug.Log($"[Quantum Animator ({f.Number})] OnExit animator time window event.");
    }
  }
}