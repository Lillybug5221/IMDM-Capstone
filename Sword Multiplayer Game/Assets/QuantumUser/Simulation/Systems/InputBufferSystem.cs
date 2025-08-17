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

        //public static Dictionary<PlayerLink, List<InputStruct>> InputBuffers = new Dictionary<PlayerLink, List<InputStruct>>();

        //public static Dictionary<PlayerLink, InputStruct> CurrentActions = new Dictionary<PlayerLink, InputStruct>();
        
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

            if(currAction -> ActionPhase < 4){
                //attack not yet cancelable
                UpdateInputBuffer(buffer, frame, filter.Link->Player);
                return;
            }else if((ActionType)(currAction -> ActionType) != ActionType.Movement && currAction -> ActionPhase == 4){
                if(input -> LightAttack == false &&
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
                AnimatorComponent.SetTrigger(frame, filter.Animator, "Light_DL");
                //trigger attack anim
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

            /*
            var inputBuffer = new InputBuffer(){
                ActionType = 0,
                Movement = new FPVector2(0,0),
                AttackEnumInt  = 0
            };
            frame.Add(entity, inputBuffer);
            */

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
            buffer->LightAttack9 = buffer->LightAttack8;

            buffer->LastDirection8 = buffer->LastDirection7;
            buffer->LightAttack8 = buffer->LightAttack7;

            buffer->LastDirection7 = buffer->LastDirection6;
            buffer->LightAttack7 = buffer->LightAttack6;

            buffer->LastDirection6 = buffer->LastDirection5;
            buffer->LightAttack6 = buffer->LightAttack5;

            buffer->LastDirection5 = buffer->LastDirection4;
            buffer->LightAttack5 = buffer->LightAttack4;

            buffer->LastDirection4 = buffer->LastDirection3;
            buffer->LightAttack4 = buffer->LightAttack3;

            buffer->LastDirection3 = buffer->LastDirection2;
            buffer->LightAttack3 = buffer->LightAttack2;

            buffer->LastDirection2 = buffer->LastDirection1;
            buffer->LightAttack2 = buffer->LightAttack1;

            buffer->LastDirection1 = currInput -> LeftStickDirection;
            buffer->LightAttack1 = currInput-> LightAttack == true;
        }

        private (bool exists, Input input) GetOldestActionInBuffer(InputBuffer* buffer){
            Quantum.Input input = new Quantum.Input();
            for (int i = 0; i < 3; i++) { // last 3 frames in buffer
                bool actionPressed = i switch {
                    0 => buffer->LightAttack0 == true,
                    1 => buffer->LightAttack1,
                    2 => buffer->LightAttack2,
                    3 => buffer->LightAttack3,
                    4 => buffer->LightAttack4,
                    5 => buffer->LightAttack5,
                    6 => buffer->LightAttack6,
                    7 => buffer->LightAttack7,
                    8 => buffer->LightAttack8,
                    9 => buffer->LightAttack9,
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
                    return(true, input);
                }
            }

            return (false, input);
        }
    }

    
}


