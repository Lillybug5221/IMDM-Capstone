namespace Quantum {
    using Photon.Deterministic;
    using Quantum.Addons.Animator;

    public unsafe abstract class AnimatorStateBehaviour : AssetObject {

        public abstract bool OnStateEnter(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time);
        public abstract bool OnStateUpdate(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time, AnimatorStateType stateType);
        public abstract bool OnStateExit(Frame f, EntityRef entity, AnimatorComponent* animator, AnimatorGraph graph, LayerData* layerData, AnimatorState state, FP time);
    }
}
