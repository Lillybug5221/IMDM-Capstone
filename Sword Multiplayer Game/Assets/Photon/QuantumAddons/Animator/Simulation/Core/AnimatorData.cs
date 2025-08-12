namespace Quantum.Addons.Animator
{
  using Photon.Deterministic;
  using System;
  using UnityEngine;

  /// <summary>
  /// Contains data extracted during the AnimationGraph construction, it is stored in the AnimatorClip 
  /// </summary>
  [Serializable]
  public class AnimatorData
  {
    public string ClipName;
    public int MotionId;
    public FP Length;
    public int Index;
    public int FrameRate;
    public int FrameCount;
    public AnimatorFrame[] Frames;
    [SerializeReference] 
    public AnimatorEvent[] Events;

    public bool LoopTime;
    public bool Mirror;
    public bool DisableRootMotion;

    public AnimatorFrame CalculateDelta(FP lastTime, FP currentTime)
    {
      var currentFrame = GetFrameAtTime(currentTime); 
      var lastFrame = GetFrameAtTime(lastTime);
      if (lastTime > currentTime)
      {
        var excessFrame  = GetFrameAtTime(Length) - lastFrame;
        return excessFrame - currentFrame;
      }
      return lastFrame - currentFrame;
    }

    public AnimatorFrame GetFrameAtTime(FP time)
    {
      AnimatorFrame output = new AnimatorFrame(FPQuaternion.Identity);
      if (Length == FP._0)
        return Frames[0];

      while (time > Length)
      {
        time -= Length;
        output += Frames[FrameCount - 1];
      }
      

      int timeIndex = FrameCount - 1;
      for (int f = 1; f < FrameCount; f++)
      {
        if (Frames[f].Time > time)
        {
          timeIndex = f;
          break;
        }
      }

      AnimatorFrame frameA = Frames[timeIndex - 1];
      AnimatorFrame frameB = Frames[timeIndex];
      FP currentTime = time - frameA.Time;
      FP frameTime = frameB.Time - frameA.Time;
      FP lerp = currentTime / frameTime;
      return output + AnimatorFrame.Lerp(frameA, frameB, lerp);
    }
  }
}