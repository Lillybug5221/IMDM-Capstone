namespace Quantum
{
    using Photon.Deterministic;
    using System.Runtime.Versioning;
    using System.Collections.Generic;
    using Quantum.Physics3D;

    public unsafe class ActionHandlerSystem : SystemMainThreadFilter<ActionHandlerSystem.Filter>
    {
        public struct Filter{
            public EntityRef Entity;
            public Transform3D* Transform;
            public PlayerLink* Link;
            public AnimatorComponent* Animator;
            public CurrentAction* CurrAction;
            public KCC* KCC;
            public PhysicsCollider3D* Collider;
        }
        public override void Update(Frame frame,ref Filter filter)
        {
            var currAction = filter.CurrAction;
            var transform = filter.Transform;
            var attacksData = frame.SimulationConfig.AttackHitboxData;
            var collider = filter.Collider;

            if(HitstopTickSystem.GlobalHitstopActive(frame)){
                currAction -> StartTick += 1;
				return; //skip this frame
			}
            
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
                        Lifetime  = 0,   
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
                if(currentActionType == ActionType.Dodge){
                    
                    //directly transform the position along the inputed direction. check for collisions before applying each frame.
                    //for each frame, grab the action completeness precent and pass that through an animation curve to get the completed distance precentage and set the player position between a 
                    //roll start positon and end position at that lerp value. always draw a line between the start position and the target position, if there is a collision with something, stop there.
                                        
                    //zSetup endpos on startup
                    if(frameNumber == 0){

                        FPVector2 dashDirection2D = (currAction -> Direction).Normalized;
                        FP dashMagnitude = (currAction -> Direction).Magnitude;
                        FPVector3 dashDirectionWorld = new FPVector3(dashDirection2D.X,0,dashDirection2D.Y);

                        //maybe pass a player start position into the action instead of using the current position
                        FPVector3 dirToTarget = (new FPVector3((FP)currAction -> EnemyPosition.X, (FP)currAction->PlayerPosition.Y, (FP)currAction->EnemyPosition.Z) - currAction -> PlayerPosition).Normalized;
                        FPQuaternion lookRot = FPQuaternion.LookRotation(dirToTarget, FPVector3.Up);
                        FPVector3 rotatedVector = lookRot * dashDirectionWorld;
                        //Log.Debug(dashDirectionWorld + "rotated is" + rotatedVector);
                        FPVector3 endPosition = currAction -> PlayerPosition + (rotatedVector * dashMagnitude * frame.SimulationConfig.DashDistance);
                        Hit3D? foundHit = frame.Physics3D.Linecast(currAction -> PlayerPosition, endPosition);
                        currAction -> DashEndPos = endPosition;
                        if (!foundHit.HasValue){
                            currAction -> PrecentageOfDodgeCompletable = (FP)1;
                        }else{
                            FP TotalDistance = (endPosition - currAction -> PlayerPosition).Magnitude;
                            FP CompletableDistance = (foundHit.Value.Point - currAction -> PlayerPosition).Magnitude;
                            FP Ratio = (CompletableDistance/TotalDistance);
                            //this is a hack to stop clipping when a dash is initated into a wall when they are already toucing.
                            if(Ratio < FP.FromFloat_UNSAFE(0.2f)){Ratio = 0;}
                            currAction -> PrecentageOfDodgeCompletable = Ratio;
                            Log.Debug(currAction -> PrecentageOfDodgeCompletable);
                        }
                        
                    }
                    
                    FP t = (FP)frameNumber/(FP)(currAction -> EndLagFrames + frameNumber);
                    SimCurve dashCurve = frame.SimulationConfig.DashSimCurve;
                    Log.Debug("Pre t " + t);
                    t = dashCurve.Evaluate(t);
                    Log.Debug("Post t " + t);
                    if(t <= currAction -> PrecentageOfDodgeCompletable){
                        FPVector3 nextPosition = FPVector3.Lerp(currAction ->PlayerPosition, currAction -> DashEndPos, t);
                        FPVector3 collisionTestDir = nextPosition - transform -> Position;

                        
                        var hits = frame.Physics3D.ShapeCastAll(transform->Position, transform->Rotation, collider->Shape, collisionTestDir);
                        if (hits.Count <= 0){
                            transform -> Position = nextPosition; // safe
                        } else {
                            // hit a wall before nextPos, place at hit.Point instead
                            //playerTransform.Position = hit.Point;
                        }
                    }
                    
                }

                currAction -> EndLagFrames--;
                if(currAction -> EndLagFrames <= 0){
                    currAction -> ActionPhase++;
                    //Log.Debug("Action Cancelable");
                }
            }else if(currAction -> ActionPhase == 4){
                currAction -> CancelableFrames--;
                if(currAction -> CancelableFrames <= 0){
                    //Log.Debug("Action Over");
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
