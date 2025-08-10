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
        }
        public override void Update(Frame frame,ref Filter filter)
        {
            var action = filter.ActionState;
            var transform = filter.Transform;
            
            if(frame.Number - action->StartTick > action->TotalDuration){
                Log.Debug("Action Over");
                frame.Remove<ActionState>(filter.Entity);
            }else{
                if(action->HitboxSpawned == false){
                    AssetRef<EntityPrototype> hitboxPrototype = frame.FindAsset(frame.SimulationConfig.HitboxPrototype);
                    var hitbox = frame.Create(hitboxPrototype);
                    FPVector3 forward = transform->Rotation * FPVector3.Forward;
                    FPVector3 spawnPos = transform->Position + forward * FP.FromFloat_UNSAFE(1.0f);
                    Log.Debug("hitbox spawned");

                    frame.Add(hitbox, new MeleeHitbox{
                        Owner = filter.Entity,
                        Radius = FP.FromFloat_UNSAFE(0.5f),         // half a meter
                        Height = 2,
                        Center = new FPVector3(0,1,1),
                        Rotation = FPQuaternion.Euler(90,0,0),
                        Lifetime  = 60,   
                        SpawnFrame = frame.Number,
                        Damage = action->Damage,
                        DamageApplied = false
                    });
                }

                action ->HitboxSpawned = true;
            }
        }
    }
}
