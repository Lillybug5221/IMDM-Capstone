namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class HitboxTriggerSystem : SystemSignalsOnly, ISignalOnTriggerEnter3D
    {
        public void OnTriggerEnter3D(Frame frame, TriggerInfo3D info){
            if(frame.Unsafe.TryGetPointer<MeleeHitbox>(info.Entity, out var hitbox)){
                if(hitbox->DamageApplied == false && info.Other != hitbox->Owner && frame.Unsafe.TryGetPointer<Damageable>(info.Other, out var damageable)){
                    hitbox ->DamageApplied = true;
                    damageable->Health -= hitbox->Damage;
                    Log.Debug("damageable hit, health remaining: " + damageable->Health);
                    if(damageable->Health <= 0){
                        Log.Debug("player died");
                        frame.Destroy(info.Other);
                    }
                }
            }
        }
    }
}
