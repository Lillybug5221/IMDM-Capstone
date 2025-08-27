using Quantum;
using Photon.Deterministic;
namespace Quantum
{
    public partial class SimulationConfig
    {
        public AssetRef<EntityPrototype> HitboxPrototype;

        public AssetRef<EntityPrototype> ParryEffectPrototype;

        public SimCurve DashSimCurve;

        public FP DashDistance; 

        public bool HitboxesRemain;
        
    }
}