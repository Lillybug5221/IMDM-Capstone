namespace Quantum
{
    using Photon.Deterministic;
    using System.Runtime.Versioning;
    using System.Collections.Generic;

    public unsafe class ActionHandlerSystem : SystemMainThreadFilter<ActionHandlerSystem.Filter>
    {
        public struct Filter{
            public EntityRef Entity;
            public Transform3D* Transform;
            public ActionState* ActionState;
            public PlayerLink* Link;
            public AnimatorComponent* Animator;
        }
        public override void Update(Frame frame,ref Filter filter)
        {
            
            var action = frame.Unsafe.GetPointer<ActionState>(filter.Entity);
            var transform = filter.Transform;
            var attacksData = frame.SimulationConfig.AttackHitboxData;
            
            Log.Debug("Start Tick is" + action -> StartTick);
            if(InputBufferSystem.CurrentActions[*(filter.Link)] == null){
                return;
            }
            var currentInput = InputBufferSystem.CurrentActions[*(filter.Link)];
            if(currentInput is ActionStruct){
                var currentAction = (ActionStruct)currentInput;
                if(!currentAction.ActionInitiated){
                    currentAction.ActionInitiated = true;

                    //Initiate action
                    if(currentAction is AttackStruct){
                        Log.Debug("initiating Attack");
                        AttackStruct currentAttack = (AttackStruct)currentAction;
                        int AttackDataIndex = GetAttackDataFromEnum(currentAttack.AttackName, attacksData);
                        QAttackData AttackData = attacksData[AttackDataIndex];
                        action -> AttackIndex = AttackDataIndex;
                        action -> StartTick = frame.Number;
                        action -> StartUpFrames = AttackData.AttackVals.startupFrames;
                        action -> ActiveFrames = AttackData.AttackVals.activeFrames;
                        action -> EndLagFrames = AttackData.AttackVals.endlagFrames;
                        action -> CancelableFrames = AttackData.AttackVals.cancelableFrames;
                        action -> ActionPhase = 1;
                        action -> Damage = AttackData.AttackVals.damage;

                        //trigger animation
                        
                    }else if(currentAction is ParryStruct){
                        Log.Debug("Initiating Parrying");
                    }
                }
                //continue action, update frame values, spawn hitboxes.
                if(currentAction is AttackStruct){
                    AttackStruct currentAttack = (AttackStruct)currentAction;
                    int AttackDataIndex = GetAttackDataFromEnum(currentAttack.AttackName, attacksData);
                    QAttackData AttackData = attacksData[AttackDataIndex];
                    int frameNumber = frame.Number - action -> StartTick;
                    Log.Debug("Phase is " + action -> ActionPhase);

                    if(action -> ActionPhase == 1){
                        Log.Debug("Startup");
                        Log.Debug("Before" + action -> StartUpFrames);
                        action -> StartUpFrames--;
                        Log.Debug("After" + action -> StartUpFrames);
                        if(action -> StartUpFrames <= 0){
                            Log.Debug("Phase Changing");
                            action -> ActionPhase++;
                        }
                    }else if(action -> ActionPhase == 2){
                        // activate hitboxes
                        
                        AssetRef<EntityPrototype> hitboxPrototype = frame.FindAsset(frame.SimulationConfig.HitboxPrototype);
                        var hitbox = frame.Create(hitboxPrototype);

                        frame.Add(hitbox, new MeleeHitbox{
                            Owner = filter.Entity,
                            Radius = FP.FromFloat_UNSAFE(0.1f),         // half a meter
                            Height = FP.FromFloat_UNSAFE(1.5f),
                            Center = AttackData.Hitboxes[frameNumber].Position,
                            Rotation = AttackData.Hitboxes[frameNumber].Rotation,
                            Lifetime  = 1,   
                            SpawnFrame = frame.Number,
                            Damage = action->Damage,
                            DamageApplied = false
                        });

                        action -> ActiveFrames--;
                        if(action -> ActiveFrames <= 0){
                            action -> ActionPhase++;
                        }
                    }else if(action -> ActionPhase == 3){
                        action -> EndLagFrames--;
                        if(action -> EndLagFrames <= 0){
                            action -> ActionPhase++;
                            Log.Debug("Action Cancelable");
                        }
                    }else if(action -> ActionPhase == 4){
                        action -> CancelableFrames--;
                        if(action -> CancelableFrames <= 0){
                            Log.Debug("Action Over");
                            InputBufferSystem.CurrentActions[*(filter.Link)] = null;
                        }
                    }
                }else if(currentAction is ParryStruct){
                    Log.Debug("Parrying");
                }
                
            }
        }

        public int GetAttackDataFromEnum(AttackName attackName, List<QAttackData> attackData){
            for(int i = 0; i < attackData.Count; i++){
                if(attackData[i].AttackVals.attackName == attackName){
                    return i;
                }
            }
            Log.Error("No attack by that name found");
            return 0;
        }
    }
}
