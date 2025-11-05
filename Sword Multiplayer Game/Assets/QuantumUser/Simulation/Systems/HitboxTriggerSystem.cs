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
                    if(hitterAction->DamageApplied || damageable -> Invincible){
                        return;
                    }else{
                        hitterAction->DamageApplied = true;
                    }


                    frame.Unsafe.TryGetPointer<PlayerLink>(info.Other, out var otherPlayerLink);
                    frame.Unsafe.TryGetPointer<PlayerLink>(hitbox->Owner, out var hitterPlayerLink);
                    frame.Unsafe.TryGetPointer<CurrentStunVals>(info.Other, out var otherPlayerStunVals);
                    frame.Unsafe.TryGetPointer<CurrentStunVals>(hitbox->Owner, out var hitterStunVals);
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
                                //DealDamage(frame, hitterPlayerLink, damageable, 0, 0);
                                ApplyStun(frame, hitterPlayerLink, hitterStunVals, KnockBackType.GuardBreak, 0, 180);

                                //play sfx
                                frame.Events.PlaySound((ushort)SFX.HeavyParry);

                            }else{
                                //Wrong Way Heavy Parry
                                //hit
                                DealDamage(frame, otherPlayerLink, damageable, hitterAttackConfig.HitHPDamage, hitterAttackConfig.HitStanceDamage);
                                //play sfx
                                frame.Events.PlaySound((ushort)SFX.HitConncted);
                                if(damageable -> CurrStance >= 0){
                                    ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, hitterAttackConfig.HitKnockBackType, hitterAttackConfig.HitKnockbackDistance, hitterAttackConfig.HitStunTime);
                                }else{
                                    //stance break
                                    //hard coded values for now.
                                    Log.Debug("STANCE BROKEN ON HIT");
                                    ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, KnockBackType.StanceBreak, 0, 120);
                                    damageable -> CurrStance = damageable -> MaxStance;
                                    frame.Events.BarChange(otherPlayerLink -> Player, damageable -> MaxStance, damageable -> CurrStance, 1);

                                    //play sfx
                                        frame.Events.PlaySound((ushort)SFX.StanceBreak);
                                }

                            }
                        }else if(!activeParry->HeldBlock){
                            //perfect parry
                            DealDamage(frame, otherPlayerLink, damageable, hitterAttackConfig.ParryHPDamage, hitterAttackConfig.ParryStanceDamage);
                            ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, hitterAttackConfig.ParryKnockBackType, hitterAttackConfig.ParryKnockbackDistance, hitterAttackConfig.ParryStunTime);
                            //trigger sfx event for view
                            frame.Events.PlaySound((ushort)SFX.PerfectParry);
                        }else{
                            //block
                            DealDamage(frame, otherPlayerLink, damageable, hitterAttackConfig.BlockHPDamage, hitterAttackConfig.BlockStanceDamage);
                            
                            //play sfx
                            frame.Events.PlaySound((ushort)SFX.Block);

                            if(damageable -> CurrStance >= 0){
                                ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, hitterAttackConfig.BlockKnockBackType, hitterAttackConfig.BlockKnockbackDistance, hitterAttackConfig.BlockStunTime);
                            }else{
                                //stance break
                                //hard coded values for now.
                                Log.Debug("STANCE BROKEN ON BLOCK");
                                ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, KnockBackType.StanceBreak, 0, 120);
                                damageable -> CurrStance = damageable -> MaxStance;
                                frame.Events.BarChange(otherPlayerLink -> Player, damageable -> MaxStance, damageable -> CurrStance, 1);

                                //play sfx
                                frame.Events.PlaySound((ushort)SFX.StanceBreak);
                            }
                        }
                    }else{
                        //hit
                        DealDamage(frame, otherPlayerLink, damageable, hitterAttackConfig.HitHPDamage, hitterAttackConfig.HitStanceDamage);
                        //play sfx
                        frame.Events.PlaySound((ushort)SFX.HitConncted);
                        if(damageable -> CurrStance >= 0){
                            ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, hitterAttackConfig.HitKnockBackType, hitterAttackConfig.HitKnockbackDistance, hitterAttackConfig.HitStunTime);
                        }else{
                            //stance break
                            //hard coded values for now.
                            Log.Debug("STANCE BROKEN ON HIT");
                            ApplyStun(frame, otherPlayerLink, otherPlayerStunVals, KnockBackType.StanceBreak, 0, 120);
                            damageable -> CurrStance = damageable -> MaxStance;
                            frame.Events.BarChange(otherPlayerLink -> Player, damageable -> MaxStance, damageable -> CurrStance, 1);

                            //play sfx
                                frame.Events.PlaySound((ushort)SFX.StanceBreak);
                        }
                        

                    }
                    
                    
                }
            }
        }

        private void DealDamage(Frame frame, PlayerLink* playerToDamage, Damageable* damageable, FP hpDamage, FP stanceDamage){
            damageable -> CurrHealth = (damageable -> CurrHealth - hpDamage);
            frame.Events.BarChange(playerToDamage->Player, damageable->MaxHealth, damageable->CurrHealth, 0);
            
            frame.Unsafe.TryGetPointer<CurrentGameStateFlags>(playerToDamage->Entity, out var gameStateFlags);
            if(((GameStateFlags)(gameStateFlags->Flags)).HasFlag(GameStateFlags.IsStanceBroken))
            {
                return;
            }
            damageable -> CurrStance = (damageable -> CurrStance - stanceDamage);
            if(damageable -> CurrStance == 0){damageable -> CurrStance = 0;}
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
