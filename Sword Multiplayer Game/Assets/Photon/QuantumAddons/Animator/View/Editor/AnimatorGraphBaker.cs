namespace Quantum.Addons.Animator
{
  using System;
  using System.Collections.Generic;
  using Photon.Deterministic;
  using UnityEditor;
  using UnityEditor.Animations;
  using UnityEngine;
  using UA = UnityEditor.Animations;
  using Quantum;
  using System.Linq;

  public class AnimatorGraphBaker
  {
    public void OnBake(AnimatorGraph asset)
    {
      try
      {
        BakeAnimatorGraphAsset(asset, asset.Controller);
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw;
      }

      EditorUtility.SetDirty(asset);
      AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Quantum Animator/ Bake all Graph Assets")]
    internal static void BakeAll()
    {
      string[] guids = AssetDatabase.FindAssets($"t:{nameof(AnimatorGraph)}");
      var assets = guids
        .Select(guid => AssetDatabase.LoadAssetAtPath<AnimatorGraph>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

      AnimatorGraphBaker baker = new AnimatorGraphBaker();

      foreach (var asset in assets)
      {
        baker.OnBake(asset);
      }
    }

    private AnimationClip GetStateClip(RuntimeAnimatorController controller, Motion motion)
    {
      AnimationClip clip = default;
      if (controller is AnimatorOverrideController controllerOverride)
      {
        // Get the original (default) clip from the state
        var originalClip = motion as AnimationClip;

        if (originalClip != null)
        {
          // Create and populate the override list
          var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(controllerOverride.overridesCount);
          controllerOverride.GetOverrides(overrides);

          // Try to find an override for the original clip
          foreach (var pair in overrides)
          {
            if (pair.Key == originalClip)
            {
              clip = pair.Value != null ? pair.Value : originalClip;
              break;
            }
          }
        }
      }
      else
      {
        clip = motion as AnimationClip;
      }

      return clip;
    }


    public void BakeAnimatorGraphAsset(AnimatorGraph graphAsset, RuntimeAnimatorController runtimeController)
    {
      if (runtimeController == null)
      {
        graphAsset.IsValid = false;
        throw new Exception(
          string.Format(
            $"[Quantum Animator] AnimatorGraph Controller is not valid, fix {graphAsset.name} before importing animations."));
      }

      UnityEditor.Animations.AnimatorController controller;
      if (runtimeController is AnimatorOverrideController controllerOverride)
      {
        controller = (UnityEditor.Animations.AnimatorController)controllerOverride.runtimeAnimatorController;
      }
      else
      {
        controller = (UnityEditor.Animations.AnimatorController)runtimeController;
      }

      if (!graphAsset)
      {
        return;
      }

      int weightTableResolution = (int)graphAsset.WeightTableResolution;
      int variableCount = controller.parameters.Length;

      graphAsset.Variables = new AnimatorVariable[variableCount];

      #region Mecanim Parameters/Variables

      // Mecanim Parameters/Variables
      // Make a dictionary of parameters by name for use when extracting conditions for transitions
      Dictionary<string, AnimatorControllerParameter> parameterDic =
        new Dictionary<string, AnimatorControllerParameter>();
      for (int i = 0; i < variableCount; i++)
      {
        AnimatorControllerParameter parameter = controller.parameters[i];
        parameterDic.Add(parameter.name, parameter);
        AnimatorVariable newVariable = new AnimatorVariable();

        newVariable.Name = parameter.name;
        newVariable.Index = i;
        switch (parameter.type)
        {
          case AnimatorControllerParameterType.Bool:
            newVariable.Type = AnimatorVariable.VariableType.Bool;
            newVariable.DefaultBool = parameter.defaultBool;
            break;
          case AnimatorControllerParameterType.Float:
            newVariable.Type = AnimatorVariable.VariableType.FP;
            newVariable.DefaultFp = FP.FromFloat_UNSAFE(parameter.defaultFloat);
            break;
          case AnimatorControllerParameterType.Int:
            newVariable.Type = AnimatorVariable.VariableType.Int;
            newVariable.DefaultInt = parameter.defaultInt;
            break;
          case AnimatorControllerParameterType.Trigger:
            newVariable.Type = AnimatorVariable.VariableType.Trigger;
            break;
        }

        graphAsset.Variables[i] = newVariable;
      }

      #endregion

      #region Mecanim State Graph

      var clips = new List<AnimationClip>();
      int layerCount = controller.layers.Length;
      graphAsset.Layers = new AnimatorLayer[layerCount];
      for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
      {
        AnimatorLayer newLayer = new AnimatorLayer();
        newLayer.Name = controller.layers[layerIndex].name;
        newLayer.Id = layerIndex;

        // Gets all states in the layer regardless if in a sub state machine or not.
        List<UnityEditor.Animations.AnimatorState> stateList = new List<UnityEditor.Animations.AnimatorState>();

        PopulateStateList(stateList, controller.layers[layerIndex].stateMachine);
        int stateCount = stateList.Count;

        // Potential warning for if a layer has no states in it.
        if (stateCount == 0)
        {
          Debug.LogError($"Layer {newLayer.Name} has no states in it.  This asset is not valid.");
          graphAsset.IsValid = false;
          continue;
        }

        newLayer.States = new AnimatorState[stateCount + 1]; // additional element for the any state
        Dictionary<UA.AnimatorState, AnimatorState> stateDictionary =
          new Dictionary<UA.AnimatorState, AnimatorState>();

        for (int stateIndex = 0; stateIndex < stateCount; stateIndex++)
        {
          UnityEditor.Animations.AnimatorState state = stateList[stateIndex];
          AnimatorState newState = new AnimatorState();
          newState.Name = state.name;
          newState.Id = state.nameHash;
          newState.IsDefault = controller.layers[layerIndex].stateMachine.defaultState == state;
          newState.Speed = FP.FromFloat_UNSAFE(state.speed);
          newState.CycleOffset = FP.FromFloat_UNSAFE(state.cycleOffset);

          if (state.motion != null)
          {
            AnimationClip clip = GetStateClip(runtimeController, state.motion);
            if (clip != null)
            {
              clips.Add(clip);
              AnimatorClip newClip = new AnimatorClip();
              newClip.Name = state.motion.name;
              newClip.Id = clip.name.GetHashCode();
              newClip.Data = Extract(state, clip);
              newState.Motion = newClip;
            }
            else
            {
              BlendTree tree = state.motion as BlendTree;
              if (tree != null)
              {
                foreach (var child in tree.children)
                {
                  if (child.motion == null)
                  {
                    graphAsset.IsValid = false;
                    throw new Exception(string.Format(
                      "There is a missing motion on State {0}. This is no allowed, fix before importing animations.",
                      state.name));
                  }
                }

                int childCount = tree.children.Length;

                AnimatorBlendTree newBlendTree = new AnimatorBlendTree();
                newBlendTree.Name = state.motion.name;
                newBlendTree.MotionCount = childCount;
                newBlendTree.Motions = new AnimatorMotion[childCount];
                newBlendTree.Positions = new FPVector2[childCount];
                newBlendTree.TimesScale = new FP[childCount];

                string parameterXname = tree.blendParameter;
                string parameterYname = tree.blendParameterY;

                for (int v = 0; v < variableCount; v++)
                {
                  if (controller.parameters[v].name == parameterXname)
                    newBlendTree.BlendParameterIndex = v;
                  if (controller.parameters[v].name == parameterYname)
                    newBlendTree.BlendParameterIndexY = v;
                }

                if (tree.blendType == BlendTreeType.Simple1D)
                {
                  newBlendTree.BlendParameterIndexY = newBlendTree.BlendParameterIndex;
                }

                if (newBlendTree.BlendParameterIndex == -1)
                {
                  Debug.LogError(
                    $"[Quantum Animator] Blend Tree parameter named {parameterXname} was not found on the Animator Controller during the baking process");
                }

                if (tree.blendType == BlendTreeType.Simple1D && newBlendTree.BlendParameterIndexY == -1)
                {
                  Debug.LogError(
                    $"[Quantum Animator] Blend Tree parameter named {parameterYname} was not found on the Animator Controller during the baking process");
                }

                for (int c = 0; c < childCount; c++)
                {
                  ChildMotion cMotion = tree.children[c];
                  AnimationClip cClip = GetStateClip(runtimeController, cMotion.motion);
                  if (tree.blendType == BlendTreeType.Simple1D)
                  {
                    newBlendTree.Positions[c] = new FPVector2(FP.FromFloat_UNSAFE(cMotion.threshold), 0);
                    newBlendTree.TimesScale[c] = FP.FromFloat_UNSAFE(cMotion.timeScale);
                  }
                  else
                  {
                    newBlendTree.Positions[c] = new FPVector2(FP.FromFloat_UNSAFE(cMotion.position.x),
                      FP.FromFloat_UNSAFE(cMotion.position.y));
                    //TODO timesScale
                  }

                  if (cClip != null)
                  {
                    clips.Add(cClip);
                    AnimatorClip newClip = new AnimatorClip();
                    newClip.Data = Extract(state, cClip);
                    newClip.Name = newClip.ClipName;
                    newClip.Id = cClip.name.GetHashCode();
                    newBlendTree.Motions[c] = newClip;
                  }
                }

                newBlendTree.CalculateWeightTable(weightTableResolution);

                //Debug WeightTable
                System.Text.StringBuilder debugString = new System.Text.StringBuilder();
                debugString.Append("weightTable content:\n");

                for (int i = 0; i < newBlendTree.WeightTable.GetLength(0); i++)
                {
                  for (int j = 0; j < newBlendTree.WeightTable.GetLength(1); j++)
                  {
                    FP[] arrayElement = newBlendTree.WeightTable[i, j];

                    debugString.Append($"weightTable[{i},{j}] = [");
                    for (int k = 0; k < arrayElement.Length; k++)
                    {
                      debugString.Append(arrayElement[k].ToString());
                      if (k < arrayElement.Length - 1)
                      {
                        debugString.Append(", ");
                      }
                    }

                    debugString.Append("]\n");
                  }
                }
                //Debug.Log(debugString);

                newBlendTree.CalculateTimeScaleTable(weightTableResolution);

                //Debug SpeedTable
                debugString = new System.Text.StringBuilder();
                debugString.Append("speedTable content:\n");

                for (int i = 0; i < newBlendTree.TimeScaleTable.GetLength(0); i++)
                {
                  debugString.Append($"speedTable[{i}] = [");
                  debugString.Append($"{newBlendTree.TimeScaleTable[i]}");
                  debugString.Append("]\n");
                }
                //Debug.Log(debugString);

                newState.Motion = newBlendTree;
              }
            }
          }

          // Add a function to do an additional state parsing pass
          ParseState(newState, state);


          newLayer.States[stateIndex] = newState;

          stateDictionary.Add(state, newState);
        }

        #endregion

        #region State Transitions

        // State Transitions
        // once the states have all been created
        // we'll hook up the transitions
        for (int stateIndex = 0; stateIndex < stateCount; stateIndex++)
        {
          UnityEditor.Animations.AnimatorState state = stateList[stateIndex];
          AnimatorState newState = newLayer.States[stateIndex];
          int transitionCount = state.transitions.Length;
          newState.Transitions = new AnimatorTransition[transitionCount];
          for (int transitionIndex = 0; transitionIndex < transitionCount; transitionIndex++)
          {
            AnimatorStateTransition unityAnimatorStateTransition = state.transitions[transitionIndex];

            // Done to prevent transitions from occuring with the AnimatorMecanim system.  Might be better to be an optional graph parameter
            if (graphAsset.MuteGraphTransitionsOnExport)
              unityAnimatorStateTransition.mute = true;

            var destinationState = unityAnimatorStateTransition.isExit
              ? controller.layers[layerIndex].stateMachine.defaultState
              : unityAnimatorStateTransition.destinationState;
            if (!stateDictionary.ContainsKey(destinationState)) continue;

            AnimatorTransition newTransition = new AnimatorTransition();
            newTransition.Index = transitionIndex;
            newTransition.Name = string.Format("{0} to {1}", state.name, destinationState.name);

            AnimationClip clip = GetStateClip(runtimeController, state.motion);
            AnimationClip destinationClip = GetStateClip(runtimeController, destinationState.motion);

            FP transitionDuration = unityAnimatorStateTransition.duration.ToFP();
            FP transitionOffset = unityAnimatorStateTransition.offset.ToFP();
            if (unityAnimatorStateTransition.hasFixedDuration == false && clip != null &&
                destinationClip != null)
            {
              transitionDuration *= clip.averageDuration.ToFP();
              transitionOffset *= destinationClip.averageDuration.ToFP();
            }

            newTransition.Duration = transitionDuration;
            newTransition.Offset = transitionOffset;
            newTransition.HasExitTime = unityAnimatorStateTransition.hasExitTime;

            var exitTime = clip != null
              ? unityAnimatorStateTransition.exitTime * clip.averageDuration
              : unityAnimatorStateTransition.exitTime;

            newTransition.ExitTime = FP.FromFloat_UNSAFE(exitTime);
            newTransition.DestinationStateId = stateDictionary[destinationState].Id;
            newTransition.DestinationStateName = stateDictionary[destinationState].Name;
            newTransition.CanTransitionToSelf =
              false; // Only any state transitions should be able to transition to themselves (might need to adjust)


            int conditionCount = unityAnimatorStateTransition.conditions.Length;
            newTransition.Conditions = new AnimatorCondition[conditionCount];
            for (int conditionIndex = 0; conditionIndex < conditionCount; conditionIndex++)
            {
              UnityEditor.Animations.AnimatorCondition condition =
                state.transitions[transitionIndex].conditions[conditionIndex];

              if (!parameterDic.ContainsKey(condition.parameter)) continue;
              AnimatorControllerParameter parameter = parameterDic[condition.parameter];

              AnimatorCondition newCondition = new AnimatorCondition();

              newCondition.VariableName = condition.parameter;
              newCondition.Mode = (AnimatorCondition.Modes)condition.mode;

              SetupConditionParameter(newTransition, condition, ref newCondition, parameter.type);
              newTransition.Conditions[conditionIndex] = newCondition;
            }

            newState.Transitions[transitionIndex] = newTransition;
          }
        }

        #endregion


        // This populates the animator behaviours for the state...
        for (int s = 0; s < stateCount; s++)
        {
          UnityEditor.Animations.AnimatorState state = stateList[s];
          AnimatorState newState = newLayer.States[s];

          newState.StateBehaviours = new List<AnimatorStateBehaviour>();
          foreach (var behaviour in state.behaviours)
          {
            if (behaviour is AnimatorStateBehaviourHolder holder && holder.AnimatorStateBehaviourAssets != null)
            {
              newState.StateBehaviours.AddRange(
                holder.AnimatorStateBehaviourAssets
                  .Select(QuantumUnityDB.GetGlobalAsset)
                  .Where(currentBehaviour => currentBehaviour != null)
              );
            }
          }

          // Mostly done to prevent users from adding null state behaviours.
          newState.StateBehaviours.RemoveAll(x => x == null);
        }

        #region AnyState

        //Create Any State
        AnimatorState anyState = new AnimatorState();
        anyState.Name = "Any State";
        anyState.Id = anyState.Name.GetHashCode();
        anyState.IsAny = true; //important for this one
        AnimatorStateTransition[] anyStateTransitions = controller.layers[layerIndex].stateMachine.anyStateTransitions;
        int anyStateTransitionCount = anyStateTransitions.Length;
        anyState.Transitions = new AnimatorTransition[anyStateTransitionCount];
        for (int t = 0; t < anyStateTransitionCount; t++)
        {
          // Done to prevent transitions from occuring with the AnimatorMecanim system.  Might be better to be an optional graph parameter
          if (graphAsset.MuteGraphTransitionsOnExport)
            anyStateTransitions[t].mute = true;

          AnimatorStateTransition transition = anyStateTransitions[t];
          if (!stateDictionary.ContainsKey(transition.destinationState)) continue;

          AnimationClip destinationClip = GetStateClip(runtimeController, transition.destinationState.motion);

          AnimatorTransition newTransition = new AnimatorTransition();
          newTransition.Index = t;
          newTransition.Name = string.Format("Any State to {0}", transition.destinationState.name);
          newTransition.Duration = FP.FromFloat_UNSAFE(transition.duration);
          newTransition.HasExitTime = transition.hasExitTime;
          newTransition.ExitTime = FP._1;
          newTransition.Offset =
            FP.FromFloat_UNSAFE(transition.offset * transition.destinationState.motion.averageDuration);
          newTransition.DestinationStateId = stateDictionary[transition.destinationState].Id;
          newTransition.DestinationStateName = stateDictionary[transition.destinationState].Name;
          newTransition.CanTransitionToSelf = transition.canTransitionToSelf;

          int conditionCount = transition.conditions.Length;
          newTransition.Conditions = new AnimatorCondition[conditionCount];
          for (int c = 0; c < conditionCount; c++)
          {
            UnityEditor.Animations.AnimatorCondition condition = anyStateTransitions[t].conditions[c];

            if (!parameterDic.ContainsKey(condition.parameter)) continue;
            AnimatorControllerParameter parameter = parameterDic[condition.parameter];
            AnimatorCondition newCondition = new AnimatorCondition();

            newCondition.VariableName = condition.parameter;
            newCondition.Mode = (AnimatorCondition.Modes)condition.mode;

            switch (parameter.type)
            {
              case AnimatorControllerParameterType.Float:
                newCondition.ThresholdFp = FP.FromFloat_UNSAFE(condition.threshold);
                break;

              case AnimatorControllerParameterType.Int:
                newCondition.ThresholdInt = Mathf.RoundToInt(condition.threshold);
                break;
            }

            newTransition.Conditions[c] = newCondition;
          }

          anyState.Transitions[t] = newTransition;
        }

        #endregion

        newLayer.States[stateCount] = anyState;
        graphAsset.Layers[layerIndex] = newLayer;
      }

      AnimatorGraph.Serialize(graphAsset);

      // Actually write the quantum asset onto the scriptable object.
      graphAsset.Clips = clips;
      graphAsset.IsValid = true;

      EditorUtility.SetDirty(graphAsset);

      Debug.Log($"[Quantum Animator] Imported {graphAsset.name} data.");
    }

    private static void SetupConditionParameter(AnimatorTransition transition,
      UnityEditor.Animations.AnimatorCondition unityCondition,
      ref AnimatorCondition newCondition, AnimatorControllerParameterType parameterType)
    {
      bool isValidCondition = true;
      switch (parameterType)
      {
        case AnimatorControllerParameterType.Bool:
          if (newCondition.Mode != AnimatorCondition.Modes.If && newCondition.Mode != AnimatorCondition.Modes.IfNot)
            isValidCondition = false;
          break;
        case AnimatorControllerParameterType.Float:
          newCondition.ThresholdFp = FP.FromFloat_UNSAFE(unityCondition.threshold);
          if (newCondition.Mode != AnimatorCondition.Modes.Greater && newCondition.Mode != AnimatorCondition.Modes.Less)
            isValidCondition = false;
          break;
        case AnimatorControllerParameterType.Int:
          newCondition.ThresholdInt = Mathf.RoundToInt(unityCondition.threshold);
          if (newCondition.Mode != AnimatorCondition.Modes.Greater
              && newCondition.Mode != AnimatorCondition.Modes.Less
              && newCondition.Mode != AnimatorCondition.Modes.Equals
              && newCondition.Mode != AnimatorCondition.Modes.NotEqual)
            isValidCondition = false;

          break;
        case AnimatorControllerParameterType.Trigger:
          if (newCondition.Mode != AnimatorCondition.Modes.If)
            isValidCondition = false;
          break;
      }

      if (isValidCondition == false)
      {
        Debug.LogWarning($"The transition from {transition.Name} has an invalid condition. " +
                         $"Please recreate the condition that uses the parameter {newCondition.VariableName} ");
      }
    }

    /// <summary>
    /// Custom state parsing that takes the unity animator state so various aspects of it can be applied to the AnimatorState.
    /// </summary>
    /// <param name="qtnAnimatorState">The Quantum Animator State</param>
    /// <param name="unityAnimatorState">The Unity Animator State</param>
    protected virtual void ParseState(AnimatorState qtnAnimatorState, UA.AnimatorState unityAnimatorState)
    {
    }

    /// <summary>
    /// Populates the list with all of the state machine's states and then traverses into the sub state machines of the state machine
    /// </summary>
    /// <param name="stateList">The list of Unity AnimatorStates to populate.</param>
    /// <param name="stateMachine">The state machine using to populate the list.</param>    
    private static void PopulateStateList(List<UnityEditor.Animations.AnimatorState> stateList,
      AnimatorStateMachine stateMachine)
    {
      for (int i = 0; i < stateMachine.states.Length; i++)
      {
        stateList.Add(stateMachine.states[i].state);
      }

      for (int i = 0; i < stateMachine.stateMachines.Length; i++)
      {
        PopulateStateList(stateList, stateMachine.stateMachines[i].stateMachine);
      }
    }

    public static AnimatorData Extract(UnityEditor.Animations.AnimatorState state, AnimationClip clip)
    {
      AnimatorData animationData = new AnimatorData();
      animationData.ClipName = clip.name;
      animationData.MotionId = clip.name.GetHashCode();

      EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
      AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);

      float usedTime = settings.stopTime - settings.startTime;

      animationData.FrameRate = Mathf.RoundToInt(clip.frameRate);
      animationData.Length = FP.FromFloat_UNSAFE(usedTime);
      animationData.FrameCount = Mathf.RoundToInt(clip.frameRate * usedTime);
      animationData.Frames = new AnimatorFrame[animationData.FrameCount];
      animationData.LoopTime = clip.isLooping && settings.loopTime;
      animationData.Mirror = settings.mirror;
      ;
      animationData.Events = ProcessEvents(clip);

      bool hasDisableRootMotion = false;
      foreach (var behaviour in state.behaviours)
      {
        if (behaviour is AnimatorDisableRootMotionBehaviour disableRootMotion)
        {
          hasDisableRootMotion = true;
        }
      }

      animationData.DisableRootMotion = hasDisableRootMotion;

      //Read the curves of animation
      int frameCount = animationData.FrameCount;
      int curveBindingsLength = curveBindings.Length;
      if (curveBindingsLength == 0) return animationData;

      AnimationCurve curveTx = null,
        curveTy = null,
        curveTz = null,
        curveRx = null,
        curveRy = null,
        curveRz = null,
        curveRw = null;

      for (int c = 0; c < curveBindingsLength; c++)
      {
        string propertyName = curveBindings[c].propertyName;

        //Check if the current property is a source for root motion
        if (curveBindings[c].path.Split("/").Length != 1)
          continue;

        if (propertyName == "m_LocalPosition.x" || propertyName == "RootT.x")
          curveTx = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalPosition.y" || propertyName == "RootT.y")
          curveTy = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalPosition.z" || propertyName == "RootT.z")
          curveTz = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);

        if (propertyName == "m_LocalRotation.x" || propertyName == "RootQ.x")
          curveRx = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalRotation.y" || propertyName == "RootQ.y")
          curveRy = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalRotation.z" || propertyName == "RootQ.z")
          curveRz = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
        if (propertyName == "m_LocalRotation.w" || propertyName == "RootQ.w")
          curveRw = AnimationUtility.GetEditorCurve(clip, curveBindings[c]);
      }

      bool hasPosition = curveTx != null && curveTy != null && curveTz != null;
      bool hasRotation = curveRx != null && curveRy != null && curveRz != null && curveRw != null;

      if (!hasPosition) Debug.LogWarning("No movement data was found in the animation: " + clip.name);
      if (!hasRotation) Debug.LogWarning("No rotation data was found in the animation: " + clip.name);

      // The initial pose might not be the first frame and might not face foward
      // calculate the initial direction and create an offset Quaternion to apply to transforms;

      Quaternion startRotUq = Quaternion.identity;
      FPQuaternion startRot = FPQuaternion.Identity;

      if (hasRotation)
      {
        float srotxu = curveRx.Evaluate(settings.startTime);
        float srotyu = curveRy.Evaluate(settings.startTime);
        float srotzu = curveRz.Evaluate(settings.startTime);
        float srotwu = curveRw.Evaluate(settings.startTime);

        FP srotx = FP.FromFloat_UNSAFE(srotxu);
        FP sroty = FP.FromFloat_UNSAFE(srotyu);
        FP srotz = FP.FromFloat_UNSAFE(srotzu);
        FP srotw = FP.FromFloat_UNSAFE(srotwu);

        startRotUq = new Quaternion(srotxu, srotyu, srotzu, srotwu);
        startRot = new FPQuaternion(srotx, sroty, srotz, srotw);
      }

      Quaternion offsetRotUq = Quaternion.Inverse(startRotUq);
      FPQuaternion offsetRot = FPQuaternion.Inverse(startRot);

      float startPositionX = 0f;
      float startPositionY = 0f;
      float startPositionZ = 0f;
      if (hasPosition) {
          startPositionX = curveTx.Evaluate(settings.startTime);
          startPositionY = curveTy.Evaluate(settings.startTime);
          startPositionZ = curveTz.Evaluate(settings.startTime);
      }

      float startRotYFloat = 0f;
      if (hasRotation) {
          var startCurveRot = GetCurveRotation(curveRx, curveRy, curveRz, curveRw, settings.startTime);
          startRotYFloat = startCurveRot.eulerAngles.y * Mathf.Deg2Rad;
          while (startRotYFloat < -Mathf.PI) startRotYFloat += Mathf.PI * 2;
          while (startRotYFloat > Mathf.PI) startRotYFloat += -Mathf.PI * 2;
      }

      for (int i = 0; i < frameCount; i++)
      {
        var frameData = new AnimatorFrame();
        frameData.Id = i;
        float percent = i / (frameCount > 1 ? frameCount - 1f : 1);
        float frameTime = usedTime * percent;
        frameData.Time = FP.FromFloat_UNSAFE(frameTime);
        float clipTime = settings.startTime + percent * (settings.stopTime - settings.startTime);

        if (hasRotation)
        {
          var curveRotation = GetCurveRotation(curveRx, curveRy, curveRz, curveRw, clipTime);
          var newRot = CalculateFrameRotation(curveRotation, settings);
          frameData.Rotation = FPQuaternion.Product(offsetRot, newRot);

          float rotYFloat = curveRotation.eulerAngles.y * Mathf.Deg2Rad;
          rotYFloat -= startRotYFloat;
          while (rotYFloat < -Mathf.PI) rotYFloat += Mathf.PI * 2;
          while (rotYFloat > Mathf.PI) rotYFloat += -Mathf.PI * 2;
          frameData.RotationY = FP.FromFloat_UNSAFE(rotYFloat);
        }

        if (hasPosition)
        {
          FP posx = FP.FromFloat_UNSAFE(curveTx.Evaluate(clipTime) - startPositionX);
          FP posy = FP.FromFloat_UNSAFE(curveTy.Evaluate(clipTime) - startPositionY);
          FP posz = FP.FromFloat_UNSAFE(curveTz.Evaluate(clipTime) - startPositionZ);

          if (i == 0)
          {
            posx = 0;
            posy = 0;
            posz = 0;
          }

          FPVector3 newPosition = new FPVector3(posx, posy, posz);

          if (settings.mirror) newPosition.X = -newPosition.X;
          frameData.Position = newPosition;
        }

        animationData.Frames[i] = frameData;
      }

      return animationData;
    }

    private static Quaternion GetCurveRotation(AnimationCurve curveRx, AnimationCurve curveRy, AnimationCurve curveRz,
      AnimationCurve curveRw, float clipTime)
    {
      float curveRxEval = curveRx.Evaluate(clipTime);
      float curveRyEval = curveRy.Evaluate(clipTime);
      float curveRzEval = curveRz.Evaluate(clipTime);
      float curveRwEval = curveRw.Evaluate(clipTime);
      return new Quaternion(curveRxEval, curveRyEval, curveRzEval, curveRwEval);
    }

    private static FPQuaternion CalculateFrameRotation(Quaternion curveRotation, AnimationClipSettings settings)
    {
      if (settings.mirror) //mirror the Y axis rotation
      {
        Quaternion mirrorRotation =
          new Quaternion(curveRotation.x, -curveRotation.y, -curveRotation.z, curveRotation.w);

        if (Quaternion.Dot(curveRotation, mirrorRotation) < 0)
        {
          mirrorRotation = new Quaternion(-mirrorRotation.x, -mirrorRotation.y, -mirrorRotation.z,
            -mirrorRotation.w);
        }

        curveRotation = mirrorRotation;
      }

      FP rotx = FP.FromFloat_UNSAFE(curveRotation.x);
      FP roty = FP.FromFloat_UNSAFE(curveRotation.y);
      FP rotz = FP.FromFloat_UNSAFE(curveRotation.z);
      FP rotw = FP.FromFloat_UNSAFE(curveRotation.w);
      FPQuaternion newRotation = new FPQuaternion(rotx, roty, rotz, rotw);
      return newRotation;
    }

    private static AnimatorEvent[] ProcessEvents(AnimationClip unityClip)
    {
      var clipEvents = new List<AnimatorEvent>();
      for (int i = 0; i < unityClip.events.Length; i++)
      {
        var unityEvent = unityClip.events[i];
        var animationEventData = (IAnimatorEventAsset)unityEvent.objectReferenceParameter;
        if (animationEventData != null)
        {
          var newEvent = animationEventData.OnBake(unityClip, unityEvent);
          if (newEvent != null)
          {
            clipEvents.Add(newEvent);
          }
        }
      }

      return clipEvents.ToArray();
    }
  }
}