using UnityEngine.Serialization;

namespace Quantum.Addons.Animator
{
  using Quantum;
  using System;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.Animations;
  using UnityEngine.Playables;
  using UE = UnityEngine;
  using Photon.Deterministic;

  public unsafe class AnimatorMecanim : QuantumEntityViewComponent, IAnimatorEntityViewComponent
  {
    /// <summary>
    /// If true, a "frame rate" will be utilized.  Can be used to give animations a "stop motion" type feel.
    /// </summary>
    public bool UtilizeFrameRate;

    /// <summary>
    /// The frame rate at which animations will be played back
    /// </summary>
    public float FrameRate = 60;

    /// <summary>
    /// The Unity Animator Component reference.
    /// </summary>
    private UE.Animator _animator;

    /// <summary>
    /// The previous Animation state
    /// </summary>
    private int[] _previousAnimationState;

    /// <summary>
    /// THe previous animation time
    /// </summary>
    private float[] _previousAnimationTime;

    void Awake()
    {
      _animator = GetComponentInChildren<UE.Animator>();

      // The Animator is disabled so it can be updated it manually.
      _animator.enabled = false;
    }

    public void Init(Frame frame, AnimatorComponent* animator)
    {
      var asset = QuantumUnityDB.GetGlobalAsset<AnimatorGraph>(animator->AnimatorGraph.Id);
      if (asset)
      {
        _previousAnimationState = new int[asset.Layers.Length];
        _previousAnimationTime = new float[asset.Layers.Length];
      }
    }

    public void Animate(Frame frame, AnimatorComponent* animator)
    {
      var asset = QuantumUnityDB.GetGlobalAsset<AnimatorGraph>(animator->AnimatorGraph.Id);
      if (asset)
      {
        var layers = frame.ResolveList(animator->Layers);
        for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
          var layerData = layers.GetPointer(layerIndex);
          // If ToStateId is not 0, this means we are in the middle of a transition.
          if (layerData->ToStateId != 0)
          {
            // If the animator is not playing the to state id, this means this animation hasn't started yet.
            if (_previousAnimationState[layerIndex] != layerData->ToStateId)
            {
              // The new animation is cross faded to
              _animator.CrossFadeInFixedTime(layerData->ToStateId,
                (layerData->TransitionDuration - layerData->TransitionTime).AsFloat, layerIndex,
                layerData->ToStateTime.AsFloat);

              // The Animator is updated using a delta of 0 to make sure the correct animation transition is rendered
              _animator.Update(0);

              // The previous animation state and transition time are updated
              _previousAnimationState[layerIndex] = layerData->ToStateId;

              // We update the previous animation time by the transition time
              _previousAnimationTime[layerIndex] = layerData->ToStateTime.AsFloat;
            }
            else
            {
              // Make sure the animator gets updated once per layer
              if (layerIndex == asset.Layers.Length - 1)
              {
                UpdateAnimator(layerIndex, layerData->ToStateTime.AsFloat, layerData->ToLength.AsFloat,
                  (layerData->TransitionDuration - layerData->TransitionTime).AsFloat);
              }
            }
          }
          else if (layerData->CurrentStateId != _previousAnimationState[layerIndex])
          {
            _animator.PlayInFixedTime(layerData->CurrentStateId, layerIndex, layerData->Time.AsFloat);
            _animator.Update(0);

            _previousAnimationState[layerIndex] = layerData->CurrentStateId;
            _previousAnimationTime[layerIndex] = layerData->Time.AsFloat;
          }
          else
          {
            // Make sure the animator gets updated once per layer
            if (layerIndex == asset.Layers.Length - 1)
            {
              UpdateAnimator(layerIndex, layerData->Time.AsFloat, layerData->Length.AsFloat);
            }
          }
        }

        UpdateParameters(frame, asset, animator);
      }
    }

    /// <summary>
    /// Updates the parameters / variables for the Animators
    /// </summary>
    /// <param name="frame">The quantum frame</param>
    /// <param name="animatorGraph">The Animator graph asset</param>
    /// <param name="animatorComponent"></param>
    private void UpdateParameters(Frame frame, AnimatorGraph animatorGraph, AnimatorComponent* animatorComponent)
    {
      var variableList = frame.ResolveList(animatorComponent->AnimatorVariables);

      for (int i = 0; i < animatorGraph.Variables.Length; i++)
      {
        switch (animatorGraph.Variables[i].Type)
        {
          case AnimatorVariable.VariableType.Bool:
            _animator.SetBool(animatorGraph.Variables[i].Name, *variableList[i].BooleanValue);
            break;
          case AnimatorVariable.VariableType.Trigger:
            // Do nothing, this prevents the trigger from being called twice.
            break;
          case AnimatorVariable.VariableType.Int:
            _animator.SetInteger(animatorGraph.Variables[i].Name, *variableList[i].IntegerValue);
            break;
          case AnimatorVariable.VariableType.FP:
            _animator.SetFloat(animatorGraph.Variables[i].Name, (*variableList[i].FPValue).AsFloat);
            break;
        }
      }
    }

    /// <summary>
    /// Updates the animator to the given time.
    /// </summary>
    void UpdateAnimator(int layerIndex, float time, float length, float? transitionDifference = null)
    {
      float delta = 0;
      if (!UtilizeFrameRate)
      {
        delta = time - _previousAnimationTime[layerIndex];
        _previousAnimationTime[layerIndex] = time;
      }
      else
      {
        float inFrame = Mathf.Floor(time * FrameRate) / FrameRate;
        delta = inFrame - _previousAnimationTime[layerIndex];
        _previousAnimationTime[layerIndex] = inFrame;
      }

      // Preventing negative value due to flicker on BlendTree states
      if (delta < 0 && transitionDifference.HasValue)
      {
        _animator.CrossFadeInFixedTime(_previousAnimationState[layerIndex], transitionDifference.Value, layerIndex,
          time % length);
        delta = 0f;
      }

      _animator.Update(delta);
    }
  }
}