namespace Quantum
{
    using Photon.Deterministic;
    using System.Runtime.Versioning;
    using System.Collections.Generic;
    using Quantum;
    using Quantum.Addons.Animator;
    
    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>, ISignalOnPlayerAdded , ISignalOnAnimatorRootMotion3D{
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public KCC* KCC;
            public PlayerLink* Link;
            public AnimatorComponent* Animator;
        }

        

        
        
        
        
        public override void Update(Frame frame, ref Filter filter)
        {
            //calculate player positions and forward direction

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
            forwardDir.Y = FP._0;
            forwardDir = FPVector3.Normalize(forwardDir);
            

            

            //face player towards opponent
            FPQuaternion targetRotation = FPQuaternion.LookRotation(forwardDir, FPVector3.Up);
            FP rotationSpeed = FP._1;
            FPQuaternion currentRotation = filter.Transform->Rotation;  
            FPQuaternion slerpedRotation = FPQuaternion.Slerp(currentRotation, targetRotation, rotationSpeed);
            filter.Transform->Rotation = slerpedRotation;

            //stand still if an action is occuring
            if(frame.TryGet<ActionState>(filter.Entity, out var actionState)){
                kcc->SetInputDirection(GetMovementDirection(new FPVector2(0,0),forwardDir));
                //AnimatorComponent.SetBoolean(frame, filter.Animator, "Actionable", false);
                return;
            }else{
                //AnimatorComponent.SetBoolean(frame, filter.Animator, "Actionable", true);
            }

            //read inputs

            var moveDirection = input->LeftStickDirection;
            if(moveDirection.Magnitude > 1)
            {
                moveDirection = moveDirection.Normalized;
            }

            //update previous direction
            /*
            previousInputDirections.TryGetValue(filter.Entity, out var prevMove);
            FPVector2 smoothed = FPVector2.Lerp(prevMove, moveDirection, FP.FromFloat_UNSAFE(0.1f));
            previousInputDirections[filter.Entity] = smoothed;
            */

            if(input->LightAttack.IsDown){
                int startUp = 0;
                int active = 120;
                int endLag = 0;
                frame.Add(filter.Entity,new ActionState{
                    StartTick = frame.Number,
                    StartUpFrames = startUp,
                    ActiveFrames = active,
                    EndLagFrames = endLag,
                    TotalDuration = startUp + active + endLag,
                    HitboxSpawned = false,
                    Damage = 25
                });

                //set anim
                AnimatorComponent.SetBoolean(frame, filter.Animator, "Actionable", false);
                //AnimatorComponent.ResetTrigger(frame, filter.Animator, "Light_DL");
                AnimatorComponent.SetTrigger(frame, filter.Animator, "Light_DL");
                
            }

            if (input->Jump.IsDown && kcc->IsGrounded == true)
            {
                kcc->Jump(FPVector3.Up * 5);
            }

            FPVector3 moveDir = GetMovementDirection(moveDirection, forwardDir);
            kcc->SetInputDirection(moveDir);
            
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

        public void OnAnimatorRootMotion3D(Frame frame, EntityRef entity, AnimatorFrame deltaFrame, AnimatorFrame currentFrame){
            //Return in case there is no motion delta
            if (deltaFrame.Position == FPVector3.Zero && deltaFrame.RotationY == FP._0) return;
            if (frame.Unsafe.TryGetPointer<Transform3D>(entity, out var transform))
            {
                // Create a quaternion representing the inverse of the current frame's Y-axis rotation
                var currentFrameRotation = FPQuaternion.CreateFromYawPitchRoll(currentFrame.RotationY, 0, 0);
                currentFrameRotation = FPQuaternion.Inverse(currentFrameRotation);

                // Rotate the delta position by the inverse current rotation to align movement
                var newPosition = currentFrameRotation * deltaFrame.Position;

                // Apply the transform's rotation to the new position to get the world displacement
                var displacement = transform->Rotation * newPosition;

                var kccSettings = frame.FindAsset<KCCSettings>(frame.Unsafe.GetPointer<KCC>(entity)->Settings);

                // Compute an adjusted target hit position for raycasting
                var targetHitPosition =(displacement.XOZ.Normalized * FP._0_33 * 2 ) + displacement;

                // Perform a raycast in the direction of the intended motion to detect potential collisions with statics
                var hits = frame.Physics3D.RaycastAll(transform->Position, targetHitPosition.XOZ, targetHitPosition.Magnitude, -1,
                    QueryOptions.HitStatics);

                if (hits.Count <= 0)
                {
                    // If no collision, disable the character controller temporarily
                    /*
                    if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
                    {
                    //kcc->SetActive(false);
                    }
                    */

                    // Apply the motion and rotation to the transform
                    transform->Position += displacement;
                    transform->Rotate(FPVector3.Up, deltaFrame.RotationY * FP.Rad2Deg);
                }
                else
                {
                    // If there is collision, enable the character controller
                    /*
                    if (frame.Unsafe.TryGetPointer<KCC>(entity, out var kcc))
                    {
                    //kcc->SetActive(true);
                    }
                    */
                }
            }
            
        }
    }
}

