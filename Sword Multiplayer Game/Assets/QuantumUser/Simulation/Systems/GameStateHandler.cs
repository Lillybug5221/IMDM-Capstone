namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class GameStateHandler : SystemMainThreadFilter<GameStateHandler.Filter>, ISignalOnPlayerAdded
    {
        public struct Filter
        {
            public EntityRef Entity;
            public PlayerLink* Link;
            public Damageable* Damageable;
            
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            if(filter.Damageable -> CurrHealth <= 0){
                EndRound(frame, filter.Link -> Player);
            }
        }


        public void OnPlayerAdded(Frame frame, PlayerRef player, bool firstTime)
        {
            
            var globalEntity = Globals.Get(frame);
            GameState gs = frame.Get<GameState>(globalEntity);

            gs.playersConnected += 1;
            if(gs.playersConnected == 1){
                gs.Player1Player = player;
            }
            
            if(gs.playersConnected == 2){
                gs.Player2Player = player;
                gs = StartRound(frame, gs);
            }
            
            frame.Set(globalEntity, gs);
            
        }

        private GameState StartRound(Frame frame, GameState gs){
            
            Log.Debug("Round starting");
            //create player 1
            gs.Player1Entity = CreatePlayerCharacter(frame, gs.Player1Player);
            if(frame.Unsafe.TryGetPointer<Transform3D>(gs.Player1Entity, out var p1Transform))
            {
                p1Transform->Position = new FPVector3(0, 2, -3);
            }

            //create player 2
            gs.Player2Entity = CreatePlayerCharacter(frame, gs.Player2Player);
            if(frame.Unsafe.TryGetPointer<Transform3D>(gs.Player2Entity, out var p2Transform))
            {
                p2Transform->Position = new FPVector3(0, 2, 3);
            }

            return gs;
        }

        private void EndRound(Frame frame, PlayerRef PlayerLost){
            Log.Debug("Round over");
            var globalEntity = Globals.Get(frame);
            GameState gs = frame.Get<GameState>(globalEntity);

            //update score
            if(PlayerLost == gs.Player1Player){
                gs.Player2Score ++;
            }else{
                gs.Player1Score ++;
            }

            //reset UI bars

            //these values shouldn't be hardcoded, update when rounds are more fleshed out.
            frame.Events.BarChange(gs.Player1Player, 100, 100, 0);
            frame.Events.BarChange(gs.Player1Player, 100, 100, 1);
            frame.Events.BarChange(gs.Player2Player, 100, 100, 0);
            frame.Events.BarChange(gs.Player2Player, 100, 100, 1);
            frame.Events.UpdateScore(gs.Player1Player, gs.Player1Score, gs.Player2Score);

            
            frame.Destroy(gs.Player1Entity);
            frame.Destroy(gs.Player2Entity);
            gs = StartRound(frame, gs);
            frame.Set(globalEntity, gs);
        }

        private EntityRef CreatePlayerCharacter(Frame frame, PlayerRef player){
            
            var runtimePlayer = frame.GetPlayerData(player);
            var entity = frame.Create(runtimePlayer.PlayerAvatar);

            var link = new PlayerLink()
            {
                Player = player,
                Entity = entity
            };
            frame.Add(entity, link);

            return entity;

        }

    }
}
