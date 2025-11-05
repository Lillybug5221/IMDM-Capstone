namespace Quantum.Addons.Animator {
    using Photon.Deterministic;
    using Quantum.Addons.Animator;
  using UnityEngine.Scripting;

  [Preserve]
    public unsafe class AnimatorBehaviourSystem : SystemSignalsOnly, ISignalOnAnimatorStateEnter, ISignalOnAnimatorStateUpdate, ISignalOnAnimatorStateExit {

        void ISignalOnAnimatorStateEnter.OnAnimatorStateEnter(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time) {
            foreach (var behaviour in state.StateBehaviours)
                if (behaviour.OnStateEnter(f, entity, animator, graph, layerData, state, time))
                    break;
        }

        void ISignalOnAnimatorStateExit.OnAnimatorStateExit(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time) {
            foreach (var behaviour in state.StateBehaviours)
                if (behaviour.OnStateExit(f, entity, animator, graph, layerData, state, time))
                    break;
        }

        void ISignalOnAnimatorStateUpdate.OnAnimatorStateUpdate(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time, AnimatorStateType stateType) {
            foreach (var behaviour in state.StateBehaviours)
            {
                if (behaviour.OnStateUpdate(f, entity, animator, graph, layerData, state, time, stateType))
                    break;
            }
        }
    }
}
