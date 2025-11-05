using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;

namespace Quantum
{
    public unsafe class Heavy_DConfigAsset : AttackConfigAsset
    {

        public FP EndpointDistanceFromEnemy;

        public FP MaxDistance;

        public SimCurve MovementSimCurve;

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
            AnimatorComponent.SetTrigger(frame, filter.Animator, "Heavy_D");
            filter.CurrAction->TrackingActive = true;
        }

        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber)
        {
            var currAction = filter.CurrAction;
            var enemyPosition = currAction->EnemyPosition;
            var startPosition = currAction->PlayerPosition;
            var direction = FPVector3.Normalize(startPosition - enemyPosition);
            var endPosition = enemyPosition + direction * EndpointDistanceFromEnemy;

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

        public override void ActiveLogicFirstFrame(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber)
        {
            base.ActiveLogicFirstFrame(frame, ref filter, frameNumber);
            AnimatorComponent.SetTrigger(frame, filter.Animator, "Heavy_D_3");
        }


        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
    }
}
