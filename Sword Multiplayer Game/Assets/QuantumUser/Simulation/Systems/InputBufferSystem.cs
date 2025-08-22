namespace Quantum
{
    using Photon.Deterministic;
    using System.Runtime.Versioning;
    using System.Collections.Generic;
    using Quantum;
    using Quantum.Addons.Animator;
    
    public unsafe class InputBufferSystem : SystemMainThreadFilter<InputBufferSystem.Filter>, ISignalOnPlayerAdded{
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public KCC* KCC;
            public PlayerLink* Link;
            public AnimatorComponent* Animator;
            public CurrentAction* CurrAction;
            public InputBuffer* Buffer;
            
        }

        const int BUFFER_FRAMES = 10;

        public override void Update(Frame frame, ref Filter filter)
        {
            var input = frame.GetPlayerInput(filter.Link->Player);
            var attackData = frame.SimulationConfig.AttackHitboxData;
            var currAction = filter.CurrAction;
            var buffer = filter.Buffer;

            //the direction of an attack is based off the current direction, no matter what direction was held when an attack was buffered
            var moveDirection = input->LeftStickDirection;
            if(moveDirection.Magnitude > 1)
            {
                moveDirection = moveDirection.Normalized;
            }

            //return if hitstop
            if(HitstopTickSystem.GlobalHitstopActive(frame)){
                UpdateInputBuffer(buffer, frame, filter.Link->Player);
                return;
            }

            if(currAction -> ActionPhase < 4){
                //attack not yet cancelable
                UpdateInputBuffer(buffer, frame, filter.Link->Player);
                return;
            }else if((ActionType)(currAction -> ActionType) != ActionType.Movement && currAction -> ActionPhase == 4){
                if(input -> LightAttack == false &&
                    input -> HeavyAttack == false &&
                    input -> Parry == false &&
                    input -> Special == false &&
                    input -> Dodge == false &&
                    input -> LeftStickDirection == new FPVector2(0,0)){
                    UpdateInputBuffer(buffer, frame, filter.Link->Player);
                    return;
                }

            }
            //update enemy position
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

            //read buffer and update current action
            //trigger animations here too
            
            var bufferedAction = GetOldestActionInBuffer(buffer);
            if(bufferedAction.exists){
                // Trigger the attack this frame
                if(bufferedAction.input.LightAttack || bufferedAction.input.HeavyAttack){
                    QAttackData FoundAttack = attackData[0];//this is a magic number for now. Ill read the direcitonalinput when I implement more attacks.
                    currAction -> ActionType = (byte)ActionType.Attack;
                    currAction -> AttackIndex = (byte)(FoundAttack.AttackVals.attackName); 
                    currAction -> EnemyPosition = opponentPosition;
                    currAction -> StartTick = frame.Number;
                    currAction -> StartUpFrames = (ushort)FoundAttack.AttackVals.startupFrames;
                    currAction -> ActiveFrames = (ushort)FoundAttack.AttackVals.activeFrames;
                    currAction -> EndLagFrames = (ushort)FoundAttack.AttackVals.endlagFrames;
                    currAction -> CancelableFrames = (ushort)FoundAttack.AttackVals.cancelableFrames;
                    currAction -> ActionPhase = (byte)1;
                    currAction -> Damage = (ushort)FoundAttack.AttackVals.damage;
                    currAction -> ActionNumber += 1;
                    currAction -> DamageApplied = false;
                    //trigger attack anim
                    AnimatorComponent.SetTrigger(frame, filter.Animator, "Light_DL");
                }if(bufferedAction.input.Parry){
                    currAction -> ActionType = (byte)ActionType.Parry;
                    currAction -> AttackIndex = (byte)(0); 
                    currAction -> EnemyPosition = opponentPosition;
                    currAction -> StartTick = frame.Number;
                    currAction -> StartUpFrames = (ushort)0;
                    currAction -> ActiveFrames = (ushort)12;
                    currAction -> EndLagFrames = (ushort)12;
                    currAction -> CancelableFrames = (ushort)30;
                    currAction -> ActionPhase = (byte)2;// we start in phase 2 because there is no startup
                    currAction -> Damage = (ushort)0;
                    currAction -> ActionNumber += 1;
                    AnimatorComponent.SetTrigger(frame, filter.Animator, "Parry_Activate");
                }if(bufferedAction.input.Dodge){
                    currAction -> ActionType = (byte)ActionType.Dodge;
                    currAction -> AttackIndex = (byte)(0); 
                    currAction -> Direction = input-> LeftStickDirection;
                    currAction -> EnemyPosition = opponentPosition;
                    currAction -> StartTick = frame.Number;
                    currAction -> StartUpFrames = (ushort)0;
                    currAction -> ActiveFrames = (ushort)0;
                    currAction -> EndLagFrames = (ushort)60;
                    currAction -> CancelableFrames = (ushort)30;
                    currAction -> ActionPhase = (byte)3;// we start in phase 2 because there is no startup
                    currAction -> Damage = (ushort)0;
                    currAction -> ActionNumber += 1;

                    FPVector2 DodgeDir = input->LeftStickDirection;
                    if(DodgeDir == new FPVector2(0,0)){
                        DodgeDir = new FPVector2(0,1);
                    }
                    AnimatorComponent.SetFixedPoint(frame, filter.Animator, "MoveX", DodgeDir.X);
                    AnimatorComponent.SetFixedPoint(frame, filter.Animator, "MoveY", DodgeDir.Y);
                    AnimatorComponent.SetTrigger(frame, filter.Animator, "Dodge");
                }
                
            }else{
                //trigger movement
                currAction -> ActionType = (byte)ActionType.Movement;
                currAction -> EnemyPosition = opponentPosition;
                currAction -> StartTick = frame.Number;
                currAction -> ActionPhase = 4;
                currAction -> Direction = moveDirection;
                //trigger movement anim
                AnimatorComponent.SetTrigger(frame, filter.Animator, "Walk");
            }
            
            UpdateInputBuffer(buffer, frame, filter.Link->Player);

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

        //you have to change this if you wanna change how many frames the input buffer holds
        //I know its gross but I couldn't find another way.
        private void UpdateInputBuffer(InputBuffer* buffer, Frame frame, PlayerRef player){
            var currInput = frame.GetPlayerInput(player);

            buffer->LastDirection9 = buffer->LastDirection8;
            buffer->Jump9 = buffer->Jump8;
            buffer->Dodge9 = buffer->Dodge8;
            buffer->LightAttack9 = buffer->LightAttack8;
            buffer->HeavyAttack9 = buffer->HeavyAttack8;
            buffer->Parry9 = buffer->Parry8;
            buffer->Special9 = buffer->Special9;

            buffer->LastDirection8 = buffer->LastDirection7;
            buffer->Jump8 = buffer->Jump7;
            buffer->Dodge8 = buffer->Dodge7;
            buffer->LightAttack8 = buffer->LightAttack7;
            buffer->HeavyAttack8 = buffer->HeavyAttack7;
            buffer->Parry8 = buffer->Parry7;
            buffer->Special8 = buffer->Special7;

            buffer->LastDirection7 = buffer->LastDirection6;
            buffer->Jump7 = buffer->Jump6;
            buffer->Dodge7 = buffer->Dodge6;
            buffer->LightAttack7 = buffer->LightAttack6;
            buffer->HeavyAttack7 = buffer->HeavyAttack6;
            buffer->Parry7 = buffer->Parry6;
            buffer->Special7 = buffer->Special6;

            buffer->LastDirection6 = buffer->LastDirection5;
            buffer->Jump6 = buffer->Jump5;
            buffer->Dodge6 = buffer->Dodge5;
            buffer->LightAttack6 = buffer->LightAttack5;
            buffer->HeavyAttack6 = buffer->HeavyAttack5;
            buffer->Parry6 = buffer->Parry5;
            buffer->Special6 = buffer->Special5;

            buffer->LastDirection5 = buffer->LastDirection4;
            buffer->Jump5 = buffer->Jump4;
            buffer->Dodge5 = buffer->Dodge4;
            buffer->LightAttack5 = buffer->LightAttack4;
            buffer->HeavyAttack5 = buffer->HeavyAttack4;
            buffer->Parry5 = buffer->Parry4;
            buffer->Special5 = buffer->Special4;

            buffer->LastDirection4 = buffer->LastDirection3;
            buffer->Jump4 = buffer->Jump3;
            buffer->Dodge4 = buffer->Dodge3;
            buffer->LightAttack4 = buffer->LightAttack3;
            buffer->HeavyAttack4 = buffer->HeavyAttack3;
            buffer->Parry4 = buffer->Parry3;
            buffer->Special4 = buffer->Special3;

            buffer->LastDirection3 = buffer->LastDirection2;
            buffer->Jump3 = buffer->Jump2;
            buffer->Dodge3 = buffer->Dodge2;
            buffer->LightAttack3 = buffer->LightAttack2;
            buffer->HeavyAttack3 = buffer->HeavyAttack2;
            buffer->Parry3 = buffer->Parry2;
            buffer->Special3 = buffer->Special2;

            buffer->LastDirection2 = buffer->LastDirection1;
            buffer->Jump2 = buffer->Jump1;
            buffer->Dodge2 = buffer->Dodge1;
            buffer->LightAttack2 = buffer->LightAttack1;
            buffer->HeavyAttack2 = buffer->HeavyAttack1;
            buffer->Parry2 = buffer->Parry1;
            buffer->Special2 = buffer->Special1;

            buffer->LastDirection1 = buffer->LastDirection0;
            buffer->Jump1 = buffer->Jump0;
            buffer->Dodge1 = buffer->Dodge0;
            buffer->LightAttack1 = buffer->LightAttack0;
            buffer->HeavyAttack1 = buffer->HeavyAttack0;
            buffer->Parry1 = buffer->Parry0;
            buffer->Special1 = buffer->Special0;

            buffer->LastDirection0 = currInput -> LeftStickDirection;
            buffer->Jump0 = currInput-> Jump == true;
            buffer->Dodge0 = currInput-> Dodge == true;
            buffer->LightAttack0 = currInput-> LightAttack == true;
            buffer->HeavyAttack0 = currInput-> HeavyAttack == true;
            buffer->Parry0 = currInput-> Parry == true;
            buffer->Special0 = currInput-> Special == true;
        }

        private (bool exists, Input input) GetOldestActionInBuffer(InputBuffer* buffer){
            Quantum.Input input = new Quantum.Input();
            for (int i = 0; i < 3; i++) { // last 3 frames in buffer
                bool actionPressed = i switch {
                    0 => buffer->Jump0||buffer->Dodge0||buffer->LightAttack0||buffer->HeavyAttack0||buffer->Parry0||buffer->Special0,
                    1 => buffer->Jump1||buffer->Dodge1||buffer->LightAttack1||buffer->HeavyAttack1||buffer->Parry1||buffer->Special1,
                    2 => buffer->Jump2||buffer->Dodge2||buffer->LightAttack2||buffer->HeavyAttack2||buffer->Parry2||buffer->Special2,
                    3 => buffer->Jump3||buffer->Dodge3||buffer->LightAttack3||buffer->HeavyAttack3||buffer->Parry3||buffer->Special3,
                    4 => buffer->Jump4||buffer->Dodge4||buffer->LightAttack4||buffer->HeavyAttack4||buffer->Parry4||buffer->Special4,
                    5 => buffer->Jump5||buffer->Dodge5||buffer->LightAttack5||buffer->HeavyAttack5||buffer->Parry5||buffer->Special5,
                    6 => buffer->Jump6||buffer->Dodge6||buffer->LightAttack6||buffer->HeavyAttack6||buffer->Parry6||buffer->Special6,
                    7 => buffer->Jump7||buffer->Dodge7||buffer->LightAttack7||buffer->HeavyAttack7||buffer->Parry7||buffer->Special7,
                    8 => buffer->Jump8||buffer->Dodge8||buffer->LightAttack8||buffer->HeavyAttack8||buffer->Parry8||buffer->Special8,
                    9 => buffer->Jump9||buffer->Dodge9||buffer->LightAttack9||buffer->HeavyAttack9||buffer->Parry9||buffer->Special9,
                    _ => false
                };

                if (actionPressed) {
                    input.LeftStickDirection = i switch {
                    0 => buffer->LastDirection0,
                    1 => buffer->LastDirection1,
                    2 => buffer->LastDirection2,
                    3 => buffer->LastDirection3,
                    4 => buffer->LastDirection4,
                    5 => buffer->LastDirection5,
                    6 => buffer->LastDirection6,
                    7 => buffer->LastDirection7,
                    8 => buffer->LastDirection8,
                    9 => buffer->LastDirection9,
                    _ => new FPVector2(0,0)
                    };
                    input.Jump = i switch {
                        0 => buffer->Jump0==true,
                        1 => buffer->Jump1==true,
                        2 => buffer->Jump2==true,
                        3 => buffer->Jump3==true,
                        4 => buffer->Jump4==true,
                        5 => buffer->Jump5==true,
                        6 => buffer->Jump6==true,
                        7 => buffer->Jump7==true,
                        8 => buffer->Jump8==true,
                        9 => buffer->Jump9==true,
                        _ => false
                    };
                    input.Dodge = i switch {
                        0 => buffer->Dodge0==true,
                        1 => buffer->Dodge1==true,
                        2 => buffer->Dodge2==true,
                        3 => buffer->Dodge3==true,
                        4 => buffer->Dodge4==true,
                        5 => buffer->Dodge5==true,
                        6 => buffer->Dodge6==true,
                        7 => buffer->Dodge7==true,
                        8 => buffer->Dodge8==true,
                        9 => buffer->Dodge9==true,
                        _ => false
                    };
                    input.LightAttack = i switch {
                        0 => buffer->LightAttack0==true,
                        1 => buffer->LightAttack1==true,
                        2 => buffer->LightAttack2==true,
                        3 => buffer->LightAttack3==true,
                        4 => buffer->LightAttack4==true,
                        5 => buffer->LightAttack5==true,
                        6 => buffer->LightAttack6==true,
                        7 => buffer->LightAttack7==true,
                        8 => buffer->LightAttack8==true,
                        9 => buffer->LightAttack9==true,
                        _ => false
                    };
                    input.HeavyAttack = i switch {
                        0 => buffer->HeavyAttack0==true,
                        1 => buffer->HeavyAttack1==true,
                        2 => buffer->HeavyAttack2==true,
                        3 => buffer->HeavyAttack3==true,
                        4 => buffer->HeavyAttack4==true,
                        5 => buffer->HeavyAttack5==true,
                        6 => buffer->HeavyAttack6==true,
                        7 => buffer->HeavyAttack7==true,
                        8 => buffer->HeavyAttack8==true,
                        9 => buffer->HeavyAttack9==true,
                        _ => false
                    };
                    input.Parry = i switch {
                        0 => buffer->Parry0==true,
                        1 => buffer->Parry1==true,
                        2 => buffer->Parry2==true,
                        3 => buffer->Parry3==true,
                        4 => buffer->Parry4==true,
                        5 => buffer->Parry5==true,
                        6 => buffer->Parry6==true,
                        7 => buffer->Parry7==true,
                        8 => buffer->Parry8==true,
                        9 => buffer->Parry9==true,
                        _ => false
                    };
                    input.Special = i switch {
                        0 => buffer->Special0==true,
                        1 => buffer->Special1==true,
                        2 => buffer->Special2==true,
                        3 => buffer->Special3==true,
                        4 => buffer->Special4==true,
                        5 => buffer->Special5==true,
                        6 => buffer->Special6==true,
                        7 => buffer->Special7==true,
                        8 => buffer->Special8==true,
                        9 => buffer->Special9==true,
                        _ => false
                    };
                    return(true, input);
                }
            }

            return (false, input);
        }
    }

    
}


