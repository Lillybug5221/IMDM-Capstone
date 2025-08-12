namespace Quantum
{
  using Photon.Deterministic;
  using Quantum.Addons.Animator;

  public unsafe class AnimatorStateBehaviourDebug : AnimatorStateBehaviour
  {
    public string DebugMessage;

    public override bool OnStateEnter(Frame f, EntityRef entity, AnimatorComponent* animator,
      AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time)
    {
            UnityEngine.Debug.Log("Entered State:  " + state.Name + " || " + DebugMessage);
            return false;
    }

    public override bool OnStateExit(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph,
      LayerData* layerData, AnimatorState state, FP time)
    {
      UnityEngine.Debug.Log("Exited State:  " + state.Name + " || " + DebugMessage);
            return false;
    }

    public override bool OnStateUpdate(Frame f, EntityRef entity, AnimatorComponent* animator,
      AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time, AnimatorStateType stateType)
    {
      UnityEngine.Debug.Log("Updating State:  " + state.Name + " || state is " + stateType + " || " + DebugMessage);
      return false;
    }
  }
}