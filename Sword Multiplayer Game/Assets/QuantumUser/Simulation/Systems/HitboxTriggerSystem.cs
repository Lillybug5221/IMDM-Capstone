namespace Quantum
{
    using Photon.Deterministic;
    using Quantum;

    public unsafe class HitboxTriggerSystem : SystemSignalsOnly, ISignalOnTriggerEnter3D
    {
        public void OnTriggerEnter3D(Frame frame, TriggerInfo3D info){
            if(HitstopTickSystem.GlobalHitstopActive(frame)){
				return; //skip this frame
			}
            if(frame.Unsafe.TryGetPointer<MeleeHitbox>(info.Entity, out var hitbox)){
                if(hitbox->DamageApplied == false && info.Other != hitbox->Owner && frame.Unsafe.TryGetPointer<Damageable>(info.Other, out var damageable) && frame.Unsafe.TryGetPointer<CurrentAction>(hitbox->Owner, out var hitterAction)){
                    //return if damage already dealt by this action.
                    if(hitterAction->DamageApplied){
                        return;
                    }else{
                        hitterAction->DamageApplied = true;
                    }

                    frame.Unsafe.TryGetPointer<CurrentGameStateFlags>(info.Other, out var gameStateFlags);

                    if(frame.Unsafe.TryGetPointer<ParryComponent>(info.Other, out var activeParry)){
                        if(!activeParry->HeldBlock){
                            Log.Debug("perfect blocked");
                            //perfect parry
                            gameStateFlags->Flags |= (int) GameStateFlags.IsPerfectParryConnected;
                        }else{
                            Log.Debug("normal blocked");
                            //block
                            gameStateFlags->Flags |= (int) GameStateFlags.IsBlockConnected;
                        }
                    }else{
                        //hit
                        gameStateFlags->Flags |= (int) GameStateFlags.IsHitConnected;
                    }


                    /*
                    hitbox ->DamageApplied = true;
                    if(frame.Unsafe.TryGetPointer<ParryComponent>(info.Other, out var activeParry)){
                        frame.Remove<ParryComponent>(info.Other);
                        if(frame.Unsafe.TryGetPointer<CurrentAction>(info.Other, out var currAction) && frame.Unsafe.TryGetPointer<AnimatorComponent>(info.Other, out var parryAnimator)){
                            //currAction -> ActionType = (byte)ActionType.Stun;
                            currAction -> AttackIndex = (byte)(0); 
                            currAction -> EnemyPosition = currAction -> EnemyPosition;
                            currAction -> StartTick = frame.Number;
                            currAction -> StartUpFrames = (ushort)0;
                            currAction -> ActiveFrames = (ushort)0;
                            currAction -> RecoveryFrames = (ushort)30;
                            currAction -> CancelableFrames = (ushort)30;
                            currAction -> ActionPhase = (byte)3;// we start in phase 3 because there is no startup or active
                            currAction -> Damage = (ushort)0;
                            currAction -> ActionNumber += 1;
                            AnimatorComponent.SetTrigger(frame, parryAnimator, "Parry_Deflect");
                            AddGlobalHitstop(frame, 3, 3);
                        }
                    }else{
                        if(frame.Unsafe.TryGetPointer<CurrentAction>(info.Other, out var currAction) && frame.Unsafe.TryGetPointer<AnimatorComponent>(info.Other, out var hitAnimator)){
                            Log.Debug("trying to animate the hit");
                            //currAction -> ActionType = (byte)ActionType.Stun;
                            currAction -> AttackIndex = (byte)(0); 
                            currAction -> EnemyPosition = currAction -> EnemyPosition;
                            currAction -> StartTick = frame.Number;
                            currAction -> StartUpFrames = (ushort)0;
                            currAction -> ActiveFrames = (ushort)0;
                            currAction -> RecoveryFrames = (ushort)45;
                            currAction -> CancelableFrames = (ushort)30;
                            currAction -> ActionPhase = (byte)3;// we start in phase 3 because there is no startup or active
                            currAction -> Damage = (ushort)0;
                            currAction -> ActionNumber += 1;
                            AnimatorComponent.SetFixedPoint(frame, hitAnimator, "HitDirX", hitterAction->Direction.X);
                            AnimatorComponent.SetFixedPoint(frame, hitAnimator, "HitDirY", hitterAction->Direction.Y);
                            AnimatorComponent.SetTrigger(frame, hitAnimator, "Hit_Stagger");
                            AddGlobalHitstop(frame, 3, 3);
                        }else{
                            Log.Debug("couldn't animate the hit");
                        }
                        damageable->Health -= hitbox->Damage;
                        Log.Debug("damageable hit, health remaining: " + damageable->Health);
                        if(damageable->Health <= 0){
                            Log.Debug("player died");
                            //frame.Destroy(info.Other);
                        }
                    }
                    */
                    
                    
                }
            }
        }

        
    }
}
