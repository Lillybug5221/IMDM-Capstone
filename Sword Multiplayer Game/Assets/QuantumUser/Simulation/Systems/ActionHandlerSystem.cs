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
            var action = filter.ActionState;
            var transform = filter.Transform;
            var attackData = frame.SimulationConfig.AttackHitboxData[action->AttackIndex];

            
            if(frame.Number - action->StartTick > action->TotalDuration){
                Log.Debug("Action Over");
                AnimatorComponent.SetBoolean(frame, filter.Animator, "Actionable", true);
                frame.Remove<ActionState>(filter.Entity);
            }else if(frame.Number - action->StartTick > action->StartUpFrames && frame.Number - action->StartTick < action->StartUpFrames + action->ActiveFrames){
                var frameNumber =  frame.Number - action->StartTick;
                AssetRef<EntityPrototype> hitboxPrototype = frame.FindAsset(frame.SimulationConfig.HitboxPrototype);
                var hitbox = frame.Create(hitboxPrototype);
                FPVector3 forward = transform->Rotation * FPVector3.Forward;
                FPVector3 spawnPos = transform->Position + forward * FP.FromFloat_UNSAFE(1.0f);
                Log.Debug("hitbox spawned");
                if(frameNumber >= attackData.Hitboxes.Count){
                    Log.Debug("Attempted to get hitbox data outside list length, Index: " + frameNumber);
                    return;
                }
                frame.Add(hitbox, new MeleeHitbox{
                    Owner = filter.Entity,
                    Radius = FP.FromFloat_UNSAFE(0.1f),         // half a meter
                    Height = FP.FromFloat_UNSAFE(1.5f),
                    Center = attackData.Hitboxes[frameNumber].Position,
                    Rotation = attackData.Hitboxes[frameNumber].Rotation,
                    Lifetime  = 1,   
                    SpawnFrame = frame.Number,
                    Damage = action->Damage,
                    DamageApplied = false
                });
            }else if(frame.Number - action ->StartTick > action->StartUpFrames + action-> ActiveFrames + action ->EndLagFrames){
                action->Cancelable = true;
            }
        }
    }
}
