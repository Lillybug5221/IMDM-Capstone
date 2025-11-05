namespace Quantum {

    public unsafe interface IAnimatorEntityViewComponent {
        void Animate(Frame frame, AnimatorComponent* animator);
        void Init(Frame frame, AnimatorComponent* animator);
    }
}
