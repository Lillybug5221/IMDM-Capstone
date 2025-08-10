namespace Quantum
{
    using Photon.Deterministic;
    using System.Runtime.Versioning;
    using System.Collections.Generic;

    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>, ISignalOnPlayerAdded {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public KCC* KCC;
            public PlayerLink* Link;
        }
        
        
        
        public override void Update(Frame frame, ref Filter filter)
        {
            
            KCC* kcc = filter.KCC;
            var input = frame.GetPlayerInput(filter.Link->Player);
            
            FPVector3 playerPosition = filter.Transform->Position;
            FPVector3 opponentPosition = new FPVector3(0,0,0);

            foreach (var pair in frame.GetComponentIterator<PlayerLink>()) {
                EntityRef entity = pair.Entity;
                PlayerLink playerLink = pair.Component;
                if(playerLink.Player != filter.Link -> Player){
                    if(frame.Unsafe.TryGetPointer<Transform3D>(entity, out var enemyTransform))
                    {
                        opponentPosition = enemyTransform->Position;
                        //Log.Debug("opponent pos is" + opponentPosition);
                    }
                }
            }

            FPVector3 forwardDir = opponentPosition - playerPosition;
            forwardDir = FPVector3.Normalize(forwardDir);

            var moveDirection = input->LeftStickDirection;
            if(moveDirection.Magnitude > 1)
            {
                moveDirection = moveDirection.Normalized;
            }

            var actionDirection = input->RightStickDirection;
            //magic numbers in this context are bad, I need to figure out components later, just prototyping for now.
            if(actionDirection.Magnitude > FP.FromFloat_UNSAFE(0.2f)){
                int startUp = 60;
                int active = 60;
                int endLag = 60;
                frame.Add(filter.Entity,new ActionState{
                    StartTick = frame.Number,
                    StartUpFrames = startUp,
                    ActiveFrames = active,
                    EndLagFrames = endLag,
                    TotalDuration = startUp + active + endLag,
                    HitboxSpawned = false,
                    Damage = 25
                });
            }

            if (input->Jump.IsDown && kcc->IsGrounded == true)
            {
                kcc->Jump(FPVector3.Up * 5);
            }

            FPVector3 moveDir = GetMovementDirection(moveDirection, forwardDir);
            kcc->SetInputDirection(moveDir);
            if(moveDir != FPVector3.Zero){
                FPQuaternion targetRotation = FPQuaternion.LookRotation(moveDir, FPVector3.Up);
                FP rotationSpeed = FP._1;
                FPQuaternion currentRotation = filter.Transform->Rotation;
                FPQuaternion slerpedRotation = FPQuaternion.Slerp(currentRotation, targetRotation, rotationSpeed);
                filter.Transform->Rotation = slerpedRotation;
            }
            
        }

    public static FPVector3 GetMovementDirection(FPVector2 inputDirection, FPVector3 forward) {
        // Assume up is Y-up (0,1,0)
        FPVector3 up = FPVector3.Up;

        // Calculate right vector = cross(up, forward)
        FPVector3 right = FPVector3.Cross(up, forward).Normalized;

        // Flatten forward to horizontal plane (project on plane)
        FPVector3 flatForward = FPVector3.ProjectOnPlane(forward, up).Normalized;

        // Combine input: forward * input.y + right * input.x
        FPVector3 moveDir = flatForward * inputDirection.Y + right * inputDirection.X;
        //Log.Debug(inputDirection +","+forward + "," + right);
        // Normalize if needed
        /*
        if (moveDir.MagnitudeSquared > FP._1) {
            moveDir = moveDir.Normalized;
        }
        */

        return moveDir;
    }

        

        public void OnPlayerAdded(Frame frame, PlayerRef player, bool firstTime)
        {
            var runtimePlayer = frame.GetPlayerData(player);
            var entity = frame.Create(runtimePlayer.PlayerAvatar);

            var link = new PlayerLink()
            {
                Player = player,
                Entity = entity
            };
            frame.Add(entity, link);

            if(frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform))
            {
                transform->Position = new FPVector3(player * 2, 2, -5);
            }
        }
    }
}
