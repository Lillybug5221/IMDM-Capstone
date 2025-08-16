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
            public InputBuffer* Buffer;
            
        }

        public static Dictionary<PlayerLink, List<InputStruct>> InputBuffers = new Dictionary<PlayerLink, List<InputStruct>>();

        public static Dictionary<PlayerLink, InputStruct> CurrentActions = new Dictionary<PlayerLink, InputStruct>();
        
        public override void Update(Frame frame, ref Filter filter)
        {
            var input = frame.GetPlayerInput(filter.Link->Player);
            var attackData = frame.SimulationConfig.AttackHitboxData;

            var moveDirection = input->LeftStickDirection;
            if(moveDirection.Magnitude > 1)
            {
                moveDirection = moveDirection.Normalized;
            }

            //add current in[ut to buffer]
            if(input->LightAttack.IsDown){
                //do a light attack
                EnqueueInputBuffer(*(filter.Link), 
                new AttackStruct{
                    Direction = moveDirection,
                    AttackName = AttackName.Light_DL
                }, 
                filter.Buffer->framesSaved);
            }else{
                //no action input, add a walk action to the buffer
                EnqueueInputBuffer(*(filter.Link), 
                new MovementStruct{
                    Direction = moveDirection,
                }, 
                filter.Buffer->framesSaved);
            }

            Log.Debug("Input count in buffer is: " + InputBuffers[*(filter.Link)].Count);

            //read oldest input, skip movementstructs
            if(CurrentActions[*(filter.Link)] is ActionStruct){
                return;
            }
            //this is inefficient and loops the list twice, but the buffer will remain small so it should be fine.
            bool nonMovementInputInBuffer = false;
            foreach(InputStruct bufferedInput in InputBuffers[*(filter.Link)]){
                if(bufferedInput is ActionStruct){nonMovementInputInBuffer = true;}
            }

            if(nonMovementInputInBuffer){
                ActionStruct foundAction = null;
                for(int i = 0; i < InputBuffers[*(filter.Link)].Count && foundAction == null; i++){
                    var action = DequeueInputBuffer(*(filter.Link));
                    if(action is ActionStruct){
                        foundAction = (ActionStruct)action;
                    }
                }
                if(foundAction != null){
                    CurrentActions[*(filter.Link)] = foundAction;

                }else{
                    Log.Error("No non-movement input found in buffer.");
                }
            }else{
                //gets most recent directional input, no buffer for that
                CurrentActions[*(filter.Link)] = InputBuffers[*(filter.Link)][InputBuffers[*(filter.Link)].Count-1];
                ClearInputBuffer(*(filter.Link));
            }

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

            InputBuffers.Add(link, new List<InputStruct>());
            CurrentActions.Add(link, null);

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

        void ClearInputBuffer(PlayerLink playerLink){
            InputBuffers[playerLink].Clear();
        }

        void EnqueueInputBuffer(PlayerLink playerLink, InputStruct addedInput, int bufferMax){
            var list = InputBuffers[playerLink];
            list.Add(addedInput);
            if(list.Count > bufferMax){
                list.RemoveAt(0);
            }
            
        }

        InputStruct DequeueInputBuffer(PlayerLink playerLink){
            var list = InputBuffers[playerLink];
            var returnVal = list[0];
            InputBuffers[playerLink].RemoveAt(0);
            return returnVal;
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


