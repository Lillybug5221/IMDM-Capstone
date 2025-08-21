namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class HitstopTickSystem : SystemMainThread
    {

        public override void Update(Frame f) {
            // Per-entity
            foreach (var (e, hs) in f.Unsafe.GetComponentBlockIterator<Hitstop>()) {
                if (hs->FramesLeft > 0) {
                    hs->FramesLeft--;
                    f.Set(e, *hs);
                }
                if (hs->FramesLeft == 0) {
                    f.Remove<Hitstop>(e);
                }
            }

            // Global
            var globalEntity = Globals.Get(f);
            var ghs = f.Get<GlobalHitstop>(globalEntity);
            if (ghs.DelayLeft > 0) {
                ghs.DelayLeft--;
                f.Set(globalEntity, ghs);
            }else if (ghs.FramesLeft > 0) {
                ghs.FramesLeft--;
                f.Set(globalEntity, ghs);
            }

            // Clear one-frame start events
            foreach (var (e, _) in f.Unsafe.GetComponentBlockIterator<HitstopStarted>()) {
            f.Remove<HitstopStarted>(e);
            }
        }

        public static bool GlobalHitstopActive(Frame f){
            var globalEntity = Globals.Get(f);
            var ghs = f.Get<GlobalHitstop>(globalEntity);
            if (ghs.FramesLeft > 0 && ghs.DelayLeft == 0) {
                return true;
            }else{
                return false;
            }
        } 

        
    }
}
