using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Photon.Deterministic;

public class AnimationViewHandler : QuantumEntityViewComponent<CustomViewContext>
{
    private Transform swordBase;
    private Transform swordEnd;
    private Animator anim;
    [SerializeField]
    private ParticleSystem attackParticleSys;
    [SerializeField]
    private GameObject ParryIndicator;
    
    private int startFrame = -1;
    private int currFrame = 0;
    
    public override void OnInitialize(){
        anim = GetComponentInChildren<Animator>();
        if(HitboxJSONBuilder.Instance.CreateJSON){
            swordBase = FindInChildrenByName(transform, "Sword_Base");
            swordEnd = FindInChildrenByName(transform, "Sword_End");
        }
        
    }
    public override void OnUpdateView(){
        //hitbox builder
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if(HitboxJSONBuilder.Instance.CreateJSON){
            if(stateInfo.IsName(HitboxJSONBuilder.Instance.TestingAnimationName)){
                if(startFrame == -1){
                    startFrame = PredictedFrame.Number;
                    currFrame = 0;
                } 
                int temp = PredictedFrame.Number - startFrame;
                if(temp != currFrame){
                    currFrame ++;
                    Vector3 relativeBasePos = transform.InverseTransformPoint(swordBase.position);
                    Vector3 relativeEndPos = transform.InverseTransformPoint(swordEnd.position);
                    HitboxJSONBuilder.Instance.AddToLists(relativeBasePos, relativeEndPos, currFrame);

                }
            }else{
                if(currFrame != 0){
                    HitboxJSONBuilder.Instance.Save();
                    currFrame = 0;
                }
                startFrame = -1;
            }
        }

        //handle hitpause
        if(HitstopTickSystem.GlobalHitstopActive(PredictedFrame)){
            if(attackParticleSys.isPlaying){attackParticleSys.Pause();}
            return;
        }else{
            if(attackParticleSys.isPaused){attackParticleSys.Play();}
        }
        
        //parry indicator
        if(ParryIndicator != null) {
            if(PredictedFrame.TryGet<ParryComponent>(EntityRef, out var currParry)){
                ParryIndicator.SetActive(true);
            }else{
                ParryIndicator.SetActive(false);
            }
        }
        
        if (PredictedFrame.TryGet<CurrentAction>(EntityRef, out var currAction)) {
            //refactor this
            /*
            if((ActionType)(currAction.ActionType) == ActionType.Attack && !attackParticleSys.isPlaying){
                if(currAction.ActionPhase == 2){
                    attackParticleSys.Play();
                }
            }else if(((ActionType)(currAction.ActionType) != ActionType.Attack && attackParticleSys.isPlaying) ||
                     ((ActionType)(currAction.ActionType) == ActionType.Attack && currAction.ActionPhase != 2)){
                attackParticleSys.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            */


            
            if (PredictedFrame.TryGet<KCC>(EntityRef, out var kcc)) {
                var vel = kcc.Data.KinematicVelocity;

                var transform = PredictedFrame.Get<Transform3D>(EntityRef);
                var forward = transform.Forward; 
                var up = transform.Up;          
                var right = transform.Right;

                Vector3 worldVel = vel.ToUnityVector3();

                Quaternion worldRot = Quaternion.LookRotation(forward.ToUnityVector3(), up.ToUnityVector3());

                Vector3 localVel = Quaternion.Inverse(worldRot) * worldVel;

                // Pass to animator (X = right, Y = forward)
                anim.SetFloat("MoveX", localVel.x);
                anim.SetFloat("MoveY", localVel.z);
            }

        }
    }

    Transform FindInChildrenByName(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true)) // true = include inactive
        {
            if (t.name == name)
                return t;
        }
        return null;
    }
}
