using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Quantum
{
    public unsafe class AttackConfigAsset : ActionConfigAsset
    {
        public string AttackName;

        //the QAttackData should probably be tied to the asset object, but for now we will just use this index to the AttackData list in the sim config.
        public int AttackDataIndex;

        public override void Initialize(Frame frame, ref ActionStateMachine.Filter filter){
            AnimatorComponent.SetTrigger(frame, filter.Animator, AttackName);
        }

        public override void Deinitialize(Frame frame, ref ActionStateMachine.Filter filter){
            return;
        }

        public override void StartupLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void ActiveLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
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
                    Damage = currAction->Damage,
                    DamageApplied = false
                });

            }
        }
        public override void RecoveryLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
        public override void CancelableLogic(Frame frame, ref ActionHandlerSystem.Filter filter, int frameNumber){
            return;
        }
    }
}
