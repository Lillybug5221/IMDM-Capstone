namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine.Scripting;

    public unsafe class HitboxSystem : SystemMainThreadFilter<HitboxSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public MeleeHitbox* Hitbox;
            public Transform3D* Transform;
            public PhysicsCollider3D* Collider;
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            var hitbox = filter.Hitbox;
            if(frame.Number - hitbox->SpawnFrame > hitbox->Lifetime){
                frame.Destroy(filter.Entity);
                Log.Debug("hitbox over");
                return;
            }else{
                //get player transform
                if(frame.Unsafe.TryGetPointer<Transform3D>(hitbox->Owner, out var playerTransform))
                {
                    //set hitbox position and rotation to player's
                    var transform = filter.Transform;
                    transform->Position = playerTransform->Position;
                    transform->Rotation = playerTransform->Rotation;

                    //offset collider according to input.
                    var collider = filter.Collider;
                    var Extent = (hitbox -> Height / 2) - hitbox -> Radius;
                    collider -> Shape =  Shape3D.CreateCapsule(hitbox -> Radius, Extent, hitbox -> Center, hitbox -> Rotation);
                    //Log.Debug("opponent pos is" + opponentPosition);
                }else{
                    frame.Destroy(filter.Entity);
                    Log.Debug("no player found");
                }

                
            }

            //foreach(var target in frame.Filter<Transform3D)
        }

        
    }
}
