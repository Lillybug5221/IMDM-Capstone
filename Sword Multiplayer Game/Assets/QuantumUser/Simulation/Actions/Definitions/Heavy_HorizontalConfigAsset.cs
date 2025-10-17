using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe class Heavy_HorizontalConfigAsset : AttackConfigAsset
    {

        public FP EndpointDistanceFromEnemy;

        public FP MaxDistance;

        public SimCurve MovementSimCurve;

        public bool TracksLeft;

        public int frameNumTrackingEnds;
        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            return; 
        }

        public override void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter){
            var currAction = filter.CurrAction;
            currAction->TrackingActive = false;
            return;
        }

        public override void StartupLogicFirstFrame(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber)
        {
            AnimatorComponent.SetTrigger(frame, filter.Animator, AttackName);
            filter.CurrAction->TrackingActive = true;
        }

        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber)
        {
            var currAction = filter.CurrAction;
            var enemyPosition = currAction->EnemyPosition;
            var startPosition = currAction->PlayerPosition;
            var direction = FPVector3.Normalize(startPosition - enemyPosition);
            var endPosition = enemyPosition + direction * EndpointDistanceFromEnemy;

            //check for left or right
            var ogEnemyPosition = currAction -> EnemyPositionAtActionStart;
            var directionToStartEnemyPos = FPVector3.Normalize(startPosition - ogEnemyPosition);
            FPVector3 cross = FPVector3.Cross(direction, directionToStartEnemyPos);
            FP side = FPVector3.Dot(cross, FPVector3.Up);

            if(TracksLeft){
                if(side >= 0){
                    currAction -> TrackingActive = true;
                }else{
                    currAction -> TrackingActive = false;
                }
            }else{
                if(side <= 0){
                    currAction -> TrackingActive = true;
                }else{
                    currAction -> TrackingActive = false;
                }
            }


            if(frameNumber > frameNumTrackingEnds)
            {
                currAction->TrackingActive = false;
            }
            

            if(FPVector3.Distance(startPosition, endPosition) > MaxDistance)
            {
                endPosition = startPosition + (-direction * MaxDistance);
            }
            
            var playerTransform = filter.Transform;
            FP PrecentageComplete = (FP)frameNumber / (FP)StartUpFrames;

           //evealute position according to sim curve.
            PrecentageComplete = MovementSimCurve.Evaluate(PrecentageComplete);

            playerTransform->Position = FPVector3.Lerp(startPosition, endPosition, PrecentageComplete);
            return;
        }

        /*public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber)
        {
            var attacksData = frame.SimulationConfig.AttackHitboxData;
            var currAction = filter.CurrAction;
            QAttackData AttackData = attacksData[AttackDataIndex];

            AssetRef<EntityPrototype> hitboxPrototype = frame.FindAsset(frame.SimulationConfig.HitboxPrototype);

            //get a list of hitboxes that corrospond to this frame
            List<QHitboxData> hitboxesToSpawn = new List<QHitboxData>();
            for (int i = 0; i < AttackData.Hitboxes.Count; i++)
            {
                if (AttackData.Hitboxes[i].FrameNum == (ushort)frameNumber)
                {
                    hitboxesToSpawn.Add(AttackData.Hitboxes[i]);
                }
            }
            //Log.Debug(hitboxesToSpawn.Count + " hitboxes this frame");
            foreach (QHitboxData hitboxData in hitboxesToSpawn)
            {
                var hitbox = frame.Create(hitboxPrototype);
                //compute base and end points
                //calculate direction towards end point from basePoint
                frame.Add(hitbox, new MeleeHitbox
                {
                    Owner = filter.Entity,
                    Radius = hitboxData.Radius,         // half a meter
                    Height = hitboxData.Length,
                    HitDirection = currAction->Direction,
                    BasePoint = hitboxData.BasePosition,
                    EndPoint = hitboxData.EndPosition,
                    Lifetime = 0,
                    SpawnFrame = frame.Number,
                    DamageApplied = false
                });

            }
        }*/

        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
    }
}
