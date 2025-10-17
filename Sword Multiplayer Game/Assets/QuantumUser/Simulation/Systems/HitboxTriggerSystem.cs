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


                    frame.Unsafe.TryGetPointer<PlayerLink>(info.Other, out var otherPlayerLink);
                    frame.Unsafe.TryGetPointer<PlayerLink>(hitbox->Owner, out var hitterPlayerLink);
                    frame.Unsafe.TryGetPointer<CurrentStunVals>(info.Other, out var otherPlayerStunVals);
                    frame.Unsafe.TryGetPointer<CurrentAction>(hitbox->Owner, out var hitterCurrAction);
                    var actionConfigs = frame.SimulationConfig.ActionConfigs;
                    ActionConfigAsset hitterActionConfig = actionConfigs[hitterAction -> ActionIndex];
                    AttackConfigAsset hitterAttackConfig = null;
                    if(hitterActionConfig is AttackConfigAsset){
                        hitterAttackConfig = (AttackConfigAsset) hitterActionConfig;
                    }else{
                        Log.Error("NO ATTACK CONFIG FOUND FOR HITTER");
                        return;
                    }


                    if(frame.Unsafe.TryGetPointer<ParryComponent>(info.Other, out var activeParry)){
                        if(activeParry -> HeavyParry){
                            FPVector2 ParryDirection = activeParry -> Direction;
                            FPVector2 HitDirection = hitbox -> HitDirection;
                            //reverse horizontal
                            HitDirection = new FPVector2(-HitDirection.X, HitDirection.Y);
                            if(ParryDirection + HitDirection == FPVector2.Zero){
                                Log.Debug("Successful Heavy Parry");
                                //Expose these hard coded values to the a config later.
                                DealDamage(frame, hitterPlayerLink, damageable, 0, 20);
                                ApplyStun(frame, hitterPlayerLink, otherPlayerStunVals, KnockBackType.GuardBreak, 0, 60);

                            }else{
                                //Wrong Way Heavy Parry
                            }
                        }else if(!activeParry->HeldBlock){
                            //perfect parry
                            DealDamage(frame, otherPlayerLink, damageable, hitterAttackConfig.ParryHPDamage, hitterAttackConfig.ParryStanceDamage);
                            ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, hitterAttackConfig.ParryKnockBackType, hitterAttackConfig.ParryKnockbackDistance, hitterAttackConfig.ParryStunTime);
                        }else{
                            //block
                            DealDamage(frame, otherPlayerLink, damageable, hitterAttackConfig.BlockHPDamage, hitterAttackConfig.BlockStanceDamage);
                            ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, hitterAttackConfig.BlockKnockBackType, hitterAttackConfig.BlockKnockbackDistance, hitterAttackConfig.BlockStunTime);
                        }
                    }else{
                        //hit
                        DealDamage(frame, otherPlayerLink, damageable, hitterAttackConfig.HitHPDamage, hitterAttackConfig.HitStanceDamage);
                        ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, hitterAttackConfig.HitKnockBackType, hitterAttackConfig.HitKnockbackDistance, hitterAttackConfig.HitStunTime);

                    }
                    
                    
                }
            }
        }

        private void DealDamage(Frame frame, PlayerLink* playerToDamage, Damageable* damageable, ushort hpDamage, ushort stanceDamage){
            damageable -> CurrHealth = (ushort)(damageable -> CurrHealth - hpDamage);
            frame.Events.BarChange(playerToDamage -> Player, damageable -> MaxHealth, damageable -> CurrHealth, 0);

            damageable -> CurrStance = (ushort)(damageable -> CurrStance - stanceDamage);
            frame.Events.BarChange(playerToDamage -> Player, damageable -> MaxStance, damageable -> CurrStance, 1);
        }

        private void ApplyStun(Frame frame, PlayerLink* playerToStun, CurrentStunVals* stunVals, KnockBackType knockbackType, FP knockbackDistance, ushort stunTime){
            frame.Unsafe.TryGetPointer<CurrentGameStateFlags>(playerToStun->Entity, out var gameStateFlags);
            stunVals -> KnockbackType = (int)knockbackType;
            stunVals -> KnockbackDistance = knockbackDistance;
            stunVals -> StunTime = stunTime;

            gameStateFlags->Flags |= (int) GameStateFlags.IsStunActive;
        }

        
    }
}
