namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class BootstrapSystem : SystemMainThread
    {
        public override void Update(Frame frame)
        {
        }

        public override void OnInit(Frame f) {
            var globals = f.Create();
            f.Add<GlobalHitstop>(globals);
            f.Add<GlobalTag>(globals); // optional marker
            f.Add<GameState>(globals);
        }
    }
}
