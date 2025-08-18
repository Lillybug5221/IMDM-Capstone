namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class HitboxTriggerSystem : SystemSignalsOnly, ISignalOnTriggerEnter3D
    {
        public void OnTriggerEnter3D(Frame frame, TriggerInfo3D info){
            if(frame.Unsafe.TryGetPointer<MeleeHitbox>(info.Entity, out var hitbox)){
                if(hitbox->DamageApplied == false && info.Other != hitbox->Owner && frame.Unsafe.TryGetPointer<Damageable>(info.Other, out var damageable)){
                    hitbox ->DamageApplied = true;
                    if(frame.Unsafe.TryGetPointer<ParryComponent>(info.Other, out var activeParry)){
                        frame.Remove<ParryComponent>(info.Other);
                        if(frame.Unsafe.TryGetPointer<CurrentAction>(info.Other, out var currAction) && frame.Unsafe.TryGetPointer<AnimatorComponent>(info.Other, out var parryAnimator)){
                            currAction -> ActionType = (byte)ActionType.Stun;
                            currAction -> AttackIndex = (byte)(0); 
                            currAction -> EnemyPosition = currAction -> EnemyPosition;
                            currAction -> StartTick = frame.Number;
                            currAction -> StartUpFrames = (ushort)0;
                            currAction -> ActiveFrames = (ushort)0;
                            currAction -> EndLagFrames = (ushort)5;
                            currAction -> CancelableFrames = (ushort)30;
                            currAction -> ActionPhase = (byte)3;// we start in phase 3 because there is no startup or active
                            currAction -> Damage = (ushort)0;
                            currAction -> ActionNumber += 1;
                            AnimatorComponent.SetTrigger(frame, parryAnimator, "Parry_Deflect");
                        }
                    }else{
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
}
