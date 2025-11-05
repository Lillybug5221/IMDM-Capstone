namespace Quantum.Addons.Animator
{
  using Photon.Deterministic;
  using System;
  using System.Collections.Generic;
  using UnityEngine;

  [Serializable]
  public unsafe class AnimatorLayer
  {
    public int Id;
    public string Name;
    public AnimatorState[] States;

    public void Update(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData, FP deltaTime)
    {
      var graph = f.FindAsset<AnimatorGraph>(animatorComponent->AnimatorGraph);
      for (int i = 0; i < States.Length; i++)
      {
        var state = States[i];
        if (IsStateActive(layerData, state))
        {
          state.Update(f,animatorComponent, layerData, graph, deltaTime);
        }
        else if (States[i].IsDefault && layerData->CurrentStateId == 0)
        {
          layerData->CurrentStateId = States[i].Id;
          state.Update(f, animatorComponent, layerData, graph, deltaTime);
        }
      }

      if (layerData->ToStateId != 0) //transition occuring
      {
        layerData->TransitionTime += deltaTime; //advance transition time
        if (layerData->TransitionTime >= layerData->TransitionDuration) //on completion 
        {
          layerData->CurrentStateId = layerData->ToStateId;
          layerData->Time = layerData->ToStateTime;
          layerData->LastTime = layerData->ToStateLastTime;
          layerData->NormalizedTime = graph.ClampTime ? FPMath.Clamp(layerData->ToStateTime / layerData->ToLength, FP._0, FP._1) : layerData->ToStateTime / layerData->ToLength;
          //reset transition state
          layerData->FromStateId = 0;
          layerData->FromStateTime = FP._0;
          layerData->FromStateLastTime = FP._0;
          layerData->FromStateNormalizedTime = FP._0;
          layerData->FromLength = FP._0;

          layerData->ToStateId = 0;
          layerData->ToStateTime = FP._0;
          layerData->ToStateLastTime = FP._0;
          layerData->ToStateNormalizedTime = FP._0;
          layerData->ToLength = FP._0;

          layerData->TransitionIndex = 0;
          layerData->TransitionTime = FP._0;
          layerData->TransitionDuration = FP._0;
        }
      }
    }

    /// <summary>
    /// Generates a list of weighted animations for a specific layers, used for blending poses in an animation.
    /// </summary>
    /// <param name="f">The Quantum Game Frame.</param>
    /// <param name="animatorComponent">The AnimatorComponent being evaluated.</param>
    /// <param name="layerData">The LayerData with values from current state of the AnimatorComponent.</param>
    /// <param name="blendDatalist">A list to store the generated <see cref="AnimatorRuntimeBlendData"/>.</param>
    public void GenerateBlendList(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData,
      List<AnimatorRuntimeBlendData> blendDatalist)
    {
      for (int i = 0; i < States.Length; i++)
      {
        var state = States[i];

        if (IsStateActive(layerData, state))
        {
          state.GenerateBlendList(f, animatorComponent, layerData, this, blendDatalist);
        }
        else if (state.IsDefault && layerData->CurrentStateId == 0)
        {
          layerData->CurrentStateId = state.Id;
          state.GenerateBlendList(f, animatorComponent, layerData, this, blendDatalist);
        }
      }
      
      // normalise
      var blendCount = 0;
      var totalWeight = FP._0;

      for (int blendIndex = 0; blendIndex < blendCount; blendIndex++)
      {
        if(blendDatalist[blendIndex].LayerId != Id) continue;
        blendCount++;
        totalWeight += blendDatalist[blendIndex].Weight;
      }

      if (totalWeight == FP._0) totalWeight = FP._1;

      for (int blendIndex = 0; blendIndex < blendCount; blendIndex++)
      {
        AnimatorRuntimeBlendData blend = blendDatalist[blendIndex];
        blend.Weight /= totalWeight; //normalise
        blendDatalist[blendIndex] = blend;
      }
    }

    public bool IsStateActive(LayerData* layerData, AnimatorState state)
    {
      var isCurrentState = layerData->CurrentStateId == state.Id;
      var isTransitionState = layerData->ToStateId == state.Id || layerData->FromStateId == state.Id;
      var isTransitioning = layerData->ToStateId != 0;

      if (isCurrentState && !isTransitioning || isTransitionState && isTransitioning || state.IsAny)
      {
        return true;
      }

      return false;
    }
  }
}