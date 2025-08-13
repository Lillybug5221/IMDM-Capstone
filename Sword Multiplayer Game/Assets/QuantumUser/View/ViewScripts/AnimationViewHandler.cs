using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;
using Photon.Deterministic;

public class AnimationViewHandler : QuantumEntityViewComponent<CustomViewContext>
{
    
    [SerializeField]
    private bool DebugSwordPosition;
    [SerializeField]
    private string TestingAnimationName;
    private Transform swordPosition;
    private Animator anim;
    private ParticleSystem particleSys;
    private int startFrame = -1;
    private int currFrame = 0;
    
    public override void OnInitialize(){
        anim = GetComponentInChildren<Animator>();
        particleSys = GetComponentInChildren<ParticleSystem>();
        if(DebugSwordPosition){
            swordPosition = FindInChildrenByName(transform, "Weapon_Uchigatana");
        }
        
    }
    public override void OnUpdateView(){
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if(DebugSwordPosition){
            if(stateInfo.IsName(TestingAnimationName)){
                if(startFrame == -1){
                    startFrame = PredictedFrame.Number;
                    currFrame = 0;
                } 
                int temp = PredictedFrame.Number - startFrame;
                if(temp != currFrame){
                    currFrame ++;
                    Vector3 relativePos = transform.InverseTransformPoint(swordPosition.position);
                    Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * swordPosition.rotation;
                    Debug.Log(TestingAnimationName + " playing, Frame: " +  currFrame + ":"+ (relativePos) +":" + relativeRot);
                }
            }else{
                startFrame = -1;
            }
        }
        
        if (PredictedFrame.TryGet<KCC>(EntityRef, out var kcc)) {
            if(PredictedFrame.TryGet<ActionState>(EntityRef, out var ActionState) && !particleSys.isPlaying){
                particleSys.Play();
            }else if(particleSys.isPlaying){
                particleSys.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
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
