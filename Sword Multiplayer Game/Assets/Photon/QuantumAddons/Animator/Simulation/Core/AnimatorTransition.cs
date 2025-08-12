namespace Quantum.Addons.Animator
{
  using System;
  using Photon.Deterministic;

  [Serializable]
  public unsafe class AnimatorTransition
  {
    public int Index;
    public string Name;

    /// <summary>
    /// The duration of the transition.
    /// </summary>
    public FP Duration;

    /// <summary>
    /// The time at which the destination state will start.
    /// </summary>
    public FP Offset;

    /// <summary>
    /// If AnimatorStateTransition.has_exit_time is true, exit_time represents the exact time at which the transition can take effect.
    /// This is represented in normalized time, so for example an exit time of 0.75 means that on the first frame where 75% of the animation has played, the Exit Time condition will be true. On the next frame, the condition will be false.
    /// For LoopTime animations, transitions with exit times smaller than 1 will be evaluated every loop, so you can use this to time your transition with the proper timing in the animation, every loop.
    /// Transitions with exit times greater than one will be evaluated only once, so they can be used to exit at a specific time, after a fixed number of loops. For example, a transition with an exit time of 3.5 will be evaluated once, after three and a half loops.
    /// Matt:  I think this documentation is incorrect and this is NOT normalized time anymore.</summary>
    public FP ExitTime;

    /// <summary>
    /// When active the transition will have an exit time condition.
    /// </summary>
    public bool HasExitTime;

    /// <summary>
    /// Allow the transition to occur if the current state is the same as the next state
    /// </summary>
    public bool CanTransitionToSelf;

    /// <summary>
    /// AnimatorCondition conditions that need to be met for a transition to happen.
    /// </summary>
    public AnimatorCondition[] Conditions;

    public int DestinationStateId;

    public string DestinationStateName;

        /// <summary>
        /// Updates and Checks the transition
        /// </summary>
        /// <returns>If true is returned, this means a transition as occurred.</returns>
    public bool Update(Frame f, AnimatorComponent* animatorComponent, LayerData* layerData, AnimatorState state,
      FP deltaTime)
    {
      if (Duration == FP._0 && !state.IsAny)
      {
        //Log.Warn(string.Format("Transition {0} has a duration of 0", state.Name));
      }


      // We want the current state type to know what values to get later when a transition has occured.
      // FromState transitions shouldn't even be possible, so might update.
      AnimatorStateType stateType = AnimatorStateType.CurrentState;
      FP time = layerData->Time;

      // Makes sure to get the current time and state type is we are in an any state transition check.
      if (state.IsAny)
      {
        if (layerData->ToStateId != 0)
        {
          stateType = AnimatorStateType.ToState;
          time = layerData->ToStateTime;
        }
      }
      else
      {
        if (layerData->ToStateId == state.Id)
        {
          stateType = AnimatorStateType.ToState;
          time = layerData->ToStateTime;
        }
        else if (layerData->FromStateId == state.Id)
        {
          stateType = AnimatorStateType.FromState;
          time = layerData->FromStateTime;
        }
      }
      
      var graph = f.FindAsset(animatorComponent->AnimatorGraph);

      bool noCurrentTransition = graph.AllowTransitionInterruption || layerData->ToStateId == 0;
      
      bool selfConditional = layerData->CurrentStateId != DestinationStateId || CanTransitionToSelf;
      if (stateType == AnimatorStateType.ToState)
        selfConditional = layerData->ToStateId != DestinationStateId || CanTransitionToSelf;

      if (HasExitTime == false && Conditions.Length == 0)
        return false;

      if (noCurrentTransition && selfConditional)
      {
        if (!HasExitTime || time >= ExitTime)
        {
          bool conditionsMet = true;
          for (int c = 0; c < Conditions.Length; c++)
          {
            if (!Conditions[c].Check(f, animatorComponent, graph))
            {
              conditionsMet = false;
              break;
            }
          }

          if (conditionsMet)
          {
            //fill in a transition state
            // Call the signals
            if (stateType == AnimatorStateType.ToState)
            {
                f.Signals.OnAnimatorStateExit(animatorComponent->Self, animatorComponent, graph, layerData, graph.GetState(layerData->ToStateId), layerData->ToStateTime);
            }
            else
            {
                f.Signals.OnAnimatorStateExit(animatorComponent->Self, animatorComponent, graph, layerData, graph.GetState(layerData->CurrentStateId), layerData->Time);
            }

            layerData->TransitionTime = FP._0;
            layerData->TransitionDuration = Duration;
            layerData->TransitionIndex = Index;

            // If the transition occurs during a transition, we need to set the "from state" information to the "to state" information.
            // Otherwise, we just set it from the Current State information.
            if (graph.AllowTransitionInterruption && stateType == AnimatorStateType.ToState)
            {
              layerData->FromStateId = layerData->ToStateId;
              layerData->FromStateTime = layerData->ToStateTime;
              layerData->FromStateLastTime = layerData->ToStateLastTime;
              layerData->FromStateNormalizedTime = layerData->ToStateNormalizedTime;
              layerData->FromLength = layerData->ToLength;
            }
            else
            {
              layerData->FromStateId = layerData->CurrentStateId;
              layerData->FromStateTime = layerData->Time;
              layerData->FromStateLastTime = layerData->LastTime;
              layerData->FromStateNormalizedTime = layerData->NormalizedTime;
              layerData->FromLength = layerData->Length;
            }

            layerData->ToStateId = DestinationStateId;
            layerData->ToStateTime = Offset;
            layerData->ToStateLastTime = FPMath.Max(Offset - deltaTime, FP._0);

            // If AnimatorState.Update run the code for s, the weights are not initialized and we get a divide by zero exception.
            var nextState = graph.GetState(layerData->ToStateId);
            if (nextState.Motion != null && nextState.GetLength(f, layerData) == 0)
            {
              nextState.Motion.CalculateWeights(f, animatorComponent, layerData, layerData->ToStateId);
            }

            layerData->ToLength = nextState.GetLength(f, layerData);
            if (layerData->ToLength != FP._0)
            {
              layerData->ToStateNormalizedTime = graph.ClampTime
                ? FPMath.Clamp(layerData->ToStateTime / layerData->ToLength, FP._0, FP._1)
                : layerData->ToStateTime / layerData->ToLength;
            }
            else
            {
              layerData->ToStateNormalizedTime = FP._0;
            }

            //fill in a transition state
            // Call the signals
            f.Signals.OnAnimatorStateEnter(animatorComponent->Self, animatorComponent, graph, layerData, graph.GetState(layerData->ToStateId), layerData->ToStateTime);

            return true;
           }
        }
      }

      return false;
    }
  }
}