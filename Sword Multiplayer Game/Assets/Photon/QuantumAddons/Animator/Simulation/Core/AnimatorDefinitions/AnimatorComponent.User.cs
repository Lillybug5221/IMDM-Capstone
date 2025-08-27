namespace Quantum
{
  using Photon.Deterministic;
  using Collections;
  using Addons.Animator;
  using UnityEngine;

  /// <summary>
  /// Extension struct for the AnimatorComponent component
  /// Used mainly to get/set variable values and to initialize an entity's AnimatorComponent
  /// </summary>
  public unsafe partial struct AnimatorComponent
  {
    internal static AnimatorRuntimeVariable* Variable(Frame f, AnimatorComponent* animator, int index)
    {
      var variablesList = f.ResolveList(animator->AnimatorVariables);
      Assert.Check(index >= 0 && index < variablesList.Count);
      return variablesList.GetPointer(index);
    }

    internal static AnimatorRuntimeVariable* VariableByName(Frame f, AnimatorComponent* animatorComponent, string name,
      out int variableId)
    {
      variableId = -1;
      if (animatorComponent->AnimatorGraph.Equals(default) == false)
      {
        AnimatorGraph graph = f.FindAsset<AnimatorGraph>(animatorComponent->AnimatorGraph.Id);
        variableId = graph.VariableIndex(name);
        if (variableId >= 0)
        {
          return Variable(f, animatorComponent, variableId);
        }
      }

      return null;
    }

    public void ResetVariables(Frame f, AnimatorComponent* animatorComponent, AnimatorGraph graph)
    {
      var variables = graph.Variables;

      // set variable defaults
      for (int v = 0; v < variables.Length; v++)
      {
        switch (variables[v].Type)
        {
          case AnimatorVariable.VariableType.FP:
            SetFixedPoint(f, animatorComponent, variables[v].Index, variables[v].DefaultFp);
            break;

          case AnimatorVariable.VariableType.Int:
            SetInteger(f, animatorComponent, variables[v].Index, variables[v].DefaultInt);
            break;

          case AnimatorVariable.VariableType.Bool:
            SetBoolean(f, animatorComponent, variables[v].Index, variables[v].DefaultBool);
            break;

          case AnimatorVariable.VariableType.Trigger:
            SetTrigger(f, animatorComponent, graph, variables[v].Name);
            break;
        }
      }
    }

    /// <summary>
    /// Initializes the passed AnimatorComponent component based on the AnimatorGraph passed
    /// This is how timers are initialized and variables are set to default
    /// </summary>
    public static void SetAnimatorGraph(Frame f, AnimatorComponent* animatorComponent, AnimatorGraph graph)
    {
      Assert.Check(graph != null, $"[Custom Animator] Tried to initialize Custom Animator component with null graph.");
      graph.Initialise(f, animatorComponent);
    }

    /// <summary>
    /// Fades the Animator to the state with the provided stateName.
    /// </summary>
    /// <param name="f">Reference to the Quantum Frame</param>
    /// <param name="stateName">The name of the state being faded to</param>
    /// <param name="fadeDuration">The duration of the transition</param>
    /// <param name="offset">The offset, in seconds that the to state will start at</param>
    /// <param name="deltaTime">The delta time, used to offset the previous state time if needed</param>
    /// <param name="resetVariables">If true, the variables in the graph will be reset to their default values.</param>
    /// <param name="setIgnoreTransitions">If true, the layer with the requested state will prevent this transition from being interrupted</param>
    public void FadeTo(Frame f, string stateName, FP? fadeDuration = null,
      FP? offset = null, FP? deltaTime = null, bool resetVariables = false, bool setIgnoreTransitions = false)
    {
      var graph = f.FindAsset<AnimatorGraph>(AnimatorGraph.Id);
      AnimatorState toState = graph.GetStateByName(ref stateName, out int layerIndex);
      if (toState == null)
      {
        return;
      }

      var layers = f.ResolveList(Layers);
      LayerData* layerData = layers.GetPointer(layerIndex);
      layerData->IgnoreTransitions = setIgnoreTransitions;
      FadeTo(f,
        layerData,
        toState,
        fadeDuration ?? FP._0_10,
        offset ?? FP._0,
        deltaTime ?? f.DeltaTime,
        resetVariables,
        setIgnoreTransitions);
    }

    public void FadeTo(Frame f, LayerData* layerData, AnimatorState toState,
      FP? fadeDuration = null, FP? offset = null, FP? deltaTime = null, bool resetVariables = false,
      bool setIgnoreTransitions = false)
    {
      FadeTo(f,
        layerData,
        toState,
        fadeDuration ?? FP._0_10,
        offset ?? FP._0,
        deltaTime ?? f.DeltaTime,
        resetVariables,
        setIgnoreTransitions);
    }

    public void FadeTo(Frame f, LayerData* layerData, AnimatorState toState,
      FP fadeDuration, FP offset, FP deltaTime, bool resetVariables, bool setIgnoreTransitions)
    {
      AnimatorComponent* animatorComponent = f.Unsafe.GetPointer<AnimatorComponent>(Self);
      var graph = f.FindAsset<AnimatorGraph>(AnimatorGraph.Id);
      if (graph.AllowFadeToTransitions == false)
      {
        Debug.LogWarning(
          $"[Quantum Animator] It is not possible to transition to state {toState.Name}. Enable AllowFadeToTransitions on {graph.name}.");
        return;
      }

      layerData->IgnoreTransitions = setIgnoreTransitions;

      layerData->TransitionTime = FP._0;
      layerData->TransitionDuration = fadeDuration;
      layerData->TransitionIndex = 0;

      // Allows fading to work, even if in the middle of a transition.
      if (layerData->ToStateId == 0)
      {
        // Calls the on state exit signal
        f.Signals.OnAnimatorStateExit(Self, animatorComponent, graph, layerData,
          graph.GetState(layerData->CurrentStateId), layerData->Time);

        layerData->FromStateId = layerData->CurrentStateId;
        layerData->FromStateTime = layerData->Time;
        layerData->FromStateLastTime = layerData->LastTime;
        layerData->FromStateNormalizedTime = layerData->NormalizedTime;
        layerData->FromLength = layerData->Length;
      }
      else
      {
        // Calls the on state exit signal
        f.Signals.OnAnimatorStateExit(Self, animatorComponent, graph, layerData,
          graph.GetState(layerData->ToStateId), layerData->ToStateTime);

        layerData->FromStateId = layerData->ToStateId;
        layerData->FromStateTime = layerData->ToStateTime;
        layerData->FromStateLastTime = layerData->ToStateLastTime;
        layerData->FromStateNormalizedTime = layerData->ToStateNormalizedTime;
        layerData->FromLength = layerData->ToLength;
      }

      layerData->ToStateId = toState.Id;
      layerData->ToStateTime = offset;
      layerData->ToStateLastTime = FPMath.Max(offset - deltaTime, FP._0);

      // If AnimatorState.Update run the code for s, the weights are not initialized and we get a divide by zero exception.
      //var s = graph.GetState(a->to_state_id);
      if (toState.GetLength(f, layerData) == 0)
      {
        toState.Motion.CalculateWeights(f, animatorComponent, layerData, layerData->ToStateId);
      }

      layerData->ToLength = toState.GetLength(f, layerData);
      layerData->ToStateNormalizedTime = graph.ClampTime
        ? FPMath.Clamp01(layerData->ToStateTime / layerData->ToLength)
        : layerData->ToStateTime / layerData->ToLength;

      if (resetVariables)
      {
        ResetVariables(f, animatorComponent, graph);
      }

      // Calls the on state enter signal
      f.Signals.OnAnimatorStateEnter(Self, animatorComponent, graph, layerData,
        graph.GetState(layerData->ToStateId), layerData->ToStateTime);
    }

    /// <summary>
    /// Try to fade the Animator to the state with the provided stateName. Fail if the target state is equals to the
    /// current state or if the Animator is already transitioning to the target state. 
    /// </summary>
    /// <param name="f">Reference to the Quantum Frame</param>
    /// <param name="stateName">The name of the state being faded to</param>
    /// <param name="fadeDuration">The duration of the transition</param>
    /// <param name="offset">The offset, in seconds that the to state will start at</param>
    /// <param name="deltaTime">The delta time, used to offset the previous state time if needed</param>
    /// <param name="resetVariables">If true, the variables in the graph will be reset to their default values.</param>
    /// <param name="setIgnoreTransitions">If true, the layer with the requested state will prevent this transition from being interrupted</param>
    public bool TryFadeToState(Frame f, string stateName, FP? fadeDuration = null, FP? offset = null,
      FP? deltaTime = null, bool resetVariables = false, bool setIgnoreTransitions = false)
    {
      var graph = f.FindAsset<AnimatorGraph>(AnimatorGraph);
      AnimatorState toState = graph.GetStateByName(ref stateName, out int layerIndex);
      if (toState == null)
      {
        return false;
      }

      var layers = f.ResolveList(Layers);
      LayerData* layerData = layers.GetPointer(layerIndex);


      var fromStateId = layerData->FromStateId;
      var toStateId = layerData->ToStateId;

      if (toStateId != 0 && fromStateId != 0 && fromStateId == toStateId)
      {
        return false;
      }

      var currentState = graph.GetState(layerData->CurrentStateId);
      if (currentState.Name == stateName)
      {
        return false;
      }

      FadeTo(f, layerData, toState, fadeDuration, offset, deltaTime, resetVariables, setIgnoreTransitions);
      return true;
    }

    /// <summary>
    /// Gets the current playing AnimatorState Id for a specific layer.
    /// </summary>
    /// <param name="f">Reference to the Quantum Frame</param>
    /// <param name="layerIndex">The index of the layer to search.</param>
    public int GetCurrentStateId(Frame f, int layerIndex)
    {
      var layers = f.ResolveList(Layers);
      LayerData* layerData = layers.GetPointer(layerIndex);
      return layerData->CurrentStateId;
    }

    /// <summary>
    /// Gets the current playing AnimatorState for a specific layer.
    /// </summary>
    /// <param name="f">Reference to the Quantum Frame</param>
    /// <param name="layerIndex">The index of the layer to search.</param>
    public AnimatorState GetCurrentState(Frame f, int layerIndex)
    {
      var graph = f.FindAsset(AnimatorGraph);
      return graph.GetState(GetCurrentStateId(f, layerIndex));
    }

    private static void SetRuntimeVariable(Frame f, AnimatorComponent* animatorComponent,
      AnimatorRuntimeVariable* variable,
      int variableId)
    {
      Assert.Check(variable != null);
      Assert.Check(variableId >= 0);

      var paramsList = f.ResolveList(animatorComponent->AnimatorVariables);
      *paramsList.GetPointer(variableId) = *variable;
    }

    public static QList<FP> GetStateWeights(Frame f, LayerData* layerData, int stateId)
    {
      var weightsDictionary = f.ResolveDictionary(layerData->BlendTreeWeights);
      var weights = f.ResolveList(weightsDictionary[stateId].Values);
      return weights;
    }

    #region FixedPoint

    private static void SetFixedPointValue(Frame f, AnimatorComponent* animatorComponent,
      AnimatorRuntimeVariable* variable,
      int variableId, FP value)
    {
      if (variable == null)
      {
        return;
      }

      *variable->FPValue = value;
      SetRuntimeVariable(f, animatorComponent, variable, variableId);
    }

    public static void SetFixedPoint(Frame f, AnimatorComponent* animatorComponent, string name,
      FP value)
    {
      var variable = VariableByName(f, animatorComponent, name, out var variableId);
      SetFixedPointValue(f, animatorComponent, variable, variableId, value);
    }

    public static void SetFixedPoint(Frame f, AnimatorComponent* animatorComponent,
      AnimatorGraph graph, string name,
      FP value)
    {
      Assert.Check(animatorComponent->AnimatorGraph == graph);

      var variableId = graph.VariableIndex(name);
      SetFixedPoint(f, animatorComponent, variableId, value);
    }

    public static void SetFixedPoint(Frame f, AnimatorComponent* animatorComponent, int variableId, FP value)
    {
      if (variableId < 0)
      {
        return;
      }

      var variable = Variable(f, animatorComponent, variableId);
      SetFixedPointValue(f, animatorComponent, variable, variableId, value);
    }

    public static FP GetFixedPoint(Frame f, AnimatorComponent* animatorComponent, string name)
    {
      var variable = VariableByName(f, animatorComponent, name, out _);
      if (variable != null)
      {
        return *variable->FPValue;
      }

      return FP.PiOver4;
    }

    public static FP GetFixedPoint(Frame f, AnimatorComponent* animatorComponent, AnimatorGraph g, string name)
    {
      Assert.Check(animatorComponent->AnimatorGraph == g);

      var variableId = g.VariableIndex(name);
      return GetFixedPoint(f, animatorComponent, variableId);
    }

    public static FP GetFixedPoint(Frame f, AnimatorComponent* animatorComponent, int variableId)
    {
      if (variableId < 0)
      {
        return FP.PiOver4;
      }

      var variable = Variable(f, animatorComponent, variableId);
      if (variable != null)
      {
        return *variable->FPValue;
      }

      return FP.PiOver4;
    }

    #endregion

    #region Integer

    static void SetIntegerValue(Frame f, AnimatorComponent* animatorComponent, AnimatorRuntimeVariable* variable,
      int variableId,
      int value)
    {
      if (variable == null)
      {
        return;
      }

      *variable->IntegerValue = value;
      SetRuntimeVariable(f, animatorComponent, variable, variableId);
    }

    public static void SetInteger(Frame f, AnimatorComponent* animatorComponent, string name, int value)
    {
      var variable = VariableByName(f, animatorComponent, name, out var variableId);
      SetIntegerValue(f, animatorComponent, variable, variableId, value);
    }

    public static void SetInteger(Frame f, AnimatorComponent* animatorComponent, AnimatorGraph graph, string name,
      int value)
    {
      Assert.Check(animatorComponent->AnimatorGraph == graph);

      var variableId = graph.VariableIndex(name);
      SetInteger(f, animatorComponent, variableId, value);
    }

    public static void SetInteger(Frame f, AnimatorComponent* animatorComponent, int variableId, int value)
    {
      if (variableId < 0)
      {
        return;
      }

      var variable = Variable(f, animatorComponent, variableId);
      SetIntegerValue(f, animatorComponent, variable, variableId, value);
    }

    public static int GetInteger(Frame f, AnimatorComponent* animatorComponent, string name)
    {
      var variable = VariableByName(f, animatorComponent, name, out _);
      if (variable != null)
      {
        return *variable->IntegerValue;
      }

      return 0;
    }

    public static int GetInteger(Frame f, AnimatorComponent* animatorComponent, AnimatorGraph graph, string name)
    {
      Assert.Check(animatorComponent->AnimatorGraph == graph);

      var variableId = graph.VariableIndex(name);
      return GetInteger(f, animatorComponent, variableId);
    }

    public static int GetInteger(Frame f, AnimatorComponent* animatorComponent, int variableId)
    {
      if (variableId < 0)
      {
        return 0;
      }

      var variable = Variable(f, animatorComponent, variableId);
      if (variable != null)
      {
        return *variable->IntegerValue;
      }

      return 0;
    }

    #endregion

    #region Boolean

    static void SetBooleanValue(Frame f, AnimatorComponent* animatorComponent, AnimatorRuntimeVariable* variable,
      int variableId,
      bool value)
    {
      if (variable == null)
      {
        return;
      }

      *variable->BooleanValue = value;
      SetRuntimeVariable(f, animatorComponent, variable, variableId);
    }

    public static void SetBoolean(Frame f, AnimatorComponent* animator, string name, bool value)
    {
      var variable = VariableByName(f, animator, name, out var variableId);
      SetBooleanValue(f, animator, variable, variableId, value);
    }

    public static void SetBoolean(Frame f, AnimatorComponent* animator, AnimatorGraph graph,
      string name,
      bool value)
    {
      Assert.Check(animator->AnimatorGraph == graph);

      var variableId = graph.VariableIndex(name);
      SetBoolean(f, animator, variableId, value);
    }

    public static void SetBoolean(Frame f, AnimatorComponent* animatorComponent, int variableId,
      bool value)
    {
      if (variableId < 0)
      {
        return;
      }

      var variable = Variable(f, animatorComponent, variableId);
      SetBooleanValue(f, animatorComponent, variable, variableId, value);
    }

    public static bool GetBoolean(Frame f, AnimatorComponent* amAnimatorComponent, string name)
    {
      var variable = VariableByName(f, amAnimatorComponent, name, out _);
      if (variable != null)
      {
        return *variable->BooleanValue;
      }

      return false;
    }

    public static bool GetBoolean(Frame f, AnimatorComponent* amAnimatorComponent, AnimatorGraph graph,
      string name)
    {
      Assert.Check(amAnimatorComponent->AnimatorGraph == graph);

      var variableId = graph.VariableIndex(name);
      return GetBoolean(f, amAnimatorComponent, variableId);
    }

    public static bool GetBoolean(Frame f, AnimatorComponent* amAnimatorComponent, int variableId)
    {
      if (variableId < 0)
      {
        return false;
      }

      var variable = Variable(f, amAnimatorComponent, variableId);
      if (variable != null)
      {
        return *variable->BooleanValue;
      }

      return false;
    }

    #endregion

    #region Trigger

    public static void SetTrigger(Frame f, AnimatorComponent* animator, string name)
    {
      SetBoolean(f, animator, name, true);
    }

    public static void SetTrigger(Frame f, AnimatorComponent* animator, AnimatorGraph graph,
      string name)
    {
      SetBoolean(f, animator, graph, name, true);
    }

    public static void SetTrigger(Frame f, AnimatorComponent* animator, int variableId)
    {
      SetBoolean(f, animator, variableId, true);
    }

    public static void ResetTrigger(Frame f, AnimatorComponent* animator, string name)
    {
      SetBoolean(f, animator, name, false);
    }

    public static void ResetTrigger(Frame f, AnimatorComponent* animator, AnimatorGraph graph,
      string name)
    {
      SetBoolean(f, animator, graph, name, false);
    }

    public static void ResetTrigger(Frame f, AnimatorComponent* animator, int variableId)
    {
      SetBoolean(f, animator, variableId, false);
    }

    public static bool IsTriggerActive(Frame f, AnimatorComponent* animator, string name)
    {
      return GetBoolean(f, animator, name);
    }

    public static bool IsTriggerActive(Frame f, AnimatorComponent* animator, AnimatorGraph graph, string name)
    {
      return GetBoolean(f, animator, graph, name);
    }

    public static bool IsTriggerActive(Frame f, AnimatorComponent* animator, int variableId)
    {
      return GetBoolean(f, animator, variableId);
    }

    #endregion

    public bool IsInTransition(Frame f, int layerIndex = 0) {
        var layers = f.ResolveList(Layers);
        LayerData* layerData = layers.GetPointer(layerIndex);

        return layerData->ToStateId != 0 && 
              layerData->TransitionTime < layerData->TransitionDuration;
    }
  }

  
}