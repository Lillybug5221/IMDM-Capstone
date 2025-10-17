using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Deterministic;

namespace Quantum
{

    public enum KnockBackType{
        None,
        InPlaceStagger,
        GroundSplat,
        FlyBack,
        Parry,
        Block,
        GuardBreak,

    }

    public unsafe class AttackConfigAsset : ActionConfigAsset
    {
        public string AttackName;

        //the QAttackData should probably be tied to the asset object, but for now we will just use this index to the AttackData list in the sim config.
        public int AttackDataIndex;

        [Header("On Hit")]
        public ushort HitHPDamage = 5;
        public ushort HitStanceDamage = 5;
        public FP HitKnockbackDistance;
        public ushort HitStunTime;
        public KnockBackType HitKnockBackType;

        [Header("On Block")]
        public ushort BlockHPDamage = 5;
        public ushort BlockStanceDamage = 5;
        public FP BlockKnockbackDistance;
        public ushort BlockStunTime;
        public KnockBackType BlockKnockBackType;

        [Header("On Parry")]
        public ushort ParryHPDamage = 5;
        public ushort ParryStanceDamage = 5;
        public FP ParryKnockbackDistance;
        public ushort ParryStunTime;
        public KnockBackType ParryKnockBackType;

        [Header("On Reversal")]
        public ushort HeavyParryHPDamage = 5;
        public ushort HeavyParryStanceDamage = 5;
        public FP HeavyParryKnockbackDistance;
        //Heavy parry can't be stunned

        

        

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
                Log.Debug("direciton is " + RequiredDirection);
                frame.Add(hitbox, new MeleeHitbox
                {
                    Owner = filter.Entity,
                    Radius = hitboxData.Radius,         // half a meter
                    Height = hitboxData.Length,
                    HitDirection = RequiredDirection,
                    BasePoint = hitboxData.BasePosition,
                    EndPoint = hitboxData.EndPosition,
                    Lifetime = 0,
                    SpawnFrame = frame.Number,
                    Damage = HitHPDamage,
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
