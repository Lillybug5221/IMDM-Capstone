using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quantum;

public class AnimationViewHandler : QuantumEntityViewComponent<CustomViewContext>
{
    public float AnimSpeedMultiplier = 1.0f;
    private Animator anim;
    private ParticleSystem particleSys;
    
    public override void OnInitialize(){
        anim = GetComponentInChildren<Animator>();
        particleSys = GetComponentInChildren<ParticleSystem>();
    }
    public override void OnUpdateView(){

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
}
