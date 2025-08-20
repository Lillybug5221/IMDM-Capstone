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
            public PlayerLink* Link;
            public AnimatorComponent* Animator;
            public CurrentAction* CurrAction;
        }
        public override void Update(Frame frame,ref Filter filter)
        {
            
            var currAction = filter.CurrAction;
            var transform = filter.Transform;
            var attacksData = frame.SimulationConfig.AttackHitboxData;
            
            //Log.Debug("Start Tick is" + CurrentAction->StartTick);
            ActionType currentActionType = (ActionType)(currAction->ActionType);
            if(currentActionType == ActionType.None || currentActionType == ActionType.Movement){
                return;
            }
            int frameNumber = frame.Number - currAction -> StartTick;
            //Log.Debug("current action phase is " + currAction ->ActionPhase);
            if(currAction -> ActionPhase == 1){
                currAction -> StartUpFrames--;
                if(currAction -> StartUpFrames <= 0){
                    currAction -> ActionPhase++;
                }
            }else if(currAction -> ActionPhase == 2){
                if(currentActionType == ActionType.Attack){
                     // activate hitboxes
                    int AttackDataIndex = GetAttackDataFromEnum((AttackName)currAction->AttackIndex, attacksData);
                    QAttackData AttackData = attacksData[AttackDataIndex];
                    
                    AssetRef<EntityPrototype> hitboxPrototype = frame.FindAsset(frame.SimulationConfig.HitboxPrototype);
                    var hitbox = frame.Create(hitboxPrototype);
                    frame.Add(hitbox, new MeleeHitbox{
                        Owner = filter.Entity,
                        Radius = FP.FromFloat_UNSAFE(0.1f),         // half a meter
                        Height = FP.FromFloat_UNSAFE(1.5f),
                        HitDirection = currAction->Direction,
                        Center = AttackData.Hitboxes[frameNumber].Position,
                        Rotation = AttackData.Hitboxes[frameNumber].Rotation,
                        Lifetime  = 1,   
                        SpawnFrame = frame.Number,
                        Damage = currAction->Damage,
                        DamageApplied = false
                    });
                }else if(currentActionType == ActionType.Parry){
                    Log.Debug("Parrying");
                    var parry = new ParryComponent()
                    {
                        HeavyParry = false,
                        Direction = new FPVector2(0,0)
                    };
                    frame.Add(filter.Entity, parry);
                    
                }

                currAction -> ActiveFrames--;
                if(currAction -> ActiveFrames <= 0){
                    currAction -> ActionPhase++;

                    //if parrying, start end animation
                    if(currentActionType == ActionType.Parry){
                        AnimatorComponent.SetTrigger(frame, filter.Animator, "Parry_Endlag");
                        if (frame.TryGet<ParryComponent>(filter.Entity, out var parryComponent))
                        {
                            frame.Remove<ParryComponent>(filter.Entity);
                        }else{
                            Log.Error("Parry Component Unexpectedly Disappeared");
                        }
                    }
                }
            }else if(currAction -> ActionPhase == 3){
                currAction -> EndLagFrames--;
                if(currAction -> EndLagFrames <= 0){
                    currAction -> ActionPhase++;
                    Log.Debug("Action Cancelable");
                }
            }else if(currAction -> ActionPhase == 4){
                currAction -> CancelableFrames--;
                if(currAction -> CancelableFrames <= 0){
                    Log.Debug("Action Over");
                    currAction -> ActionPhase++;
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
