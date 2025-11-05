namespace Quantum
{
  using System;
  using Addons.Animator;
  using UnityEngine;
  
  /// <summary>
  /// This is a sample of how to use Instant events. Use it as a base to create a new class inheriting from AnimatorInstantEventAsset and
  /// implement a custom logic on Execute method 
  /// </summary>
  [Serializable]
  public unsafe class ExampleInstantEventAsset : AnimatorInstantEventAsset
  {
    public override void Execute(Frame f, AnimatorComponent* animator, LayerData* layerData)
    {
      Debug.Log($"[Quantum Animator ({f.Number})] Execute animator instant event.");
    }
  }
}