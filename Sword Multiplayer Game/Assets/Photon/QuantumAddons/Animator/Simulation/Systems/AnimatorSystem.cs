namespace Quantum.Addons.Animator
{
  using Photon.Deterministic;
  using UnityEngine;
  using System.Collections.Generic;
  using UnityEngine.Scripting;

  [Preserve]
  public unsafe partial class AnimatorSystem : SystemMainThreadFilter<AnimatorSystem.Filter>,
    ISignalOnComponentAdded<AnimatorComponent>, ISignalOnComponentRemoved<AnimatorComponent>
  {
    public struct Filter
    {
      public EntityRef Entity;
      public AnimatorComponent* AnimatorComponent;
    }

    private List<AnimatorRuntimeBlendData> _blendList;
    private List<AnimatorMotion> _motionList;

    public override void OnInit(Frame f)
    {
      _blendList = new List<AnimatorRuntimeBlendData>();
      _motionList = new List<AnimatorMotion>();
    }

    public override void Update(Frame f, ref Filter filter)
    {
      if(HitstopTickSystem.GlobalHitstopActive(f)){
				return; //skip this frame
			}
      var animatorComponent = filter.AnimatorComponent;
      var layers = f.ResolveList<LayerData>(animatorComponent->Layers);
      var graph = f.FindAsset(animatorComponent->AnimatorGraph);
      for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
      {
        var layerData = layers.GetPointer(layerIndex);
        if (layerData->Freeze)
          continue;
        graph.UpdateGraphState(f, animatorComponent, layerData, layerIndex, f.DeltaTime * layerData->Speed);
      }
      ProcessRootMotion(f, filter.Entity, animatorComponent, graph);
    }

    private void ProcessRootMotion(Frame f, EntityRef entity, AnimatorComponent* animatorComponent, AnimatorGraph graph)
    {
      if (graph.RootMotion)
      {
        _blendList.Clear();
        _motionList.Clear();

        graph.CalculateRootMotion(f, animatorComponent, _blendList, _motionList, out AnimatorFrame deltaFrame, out AnimatorFrame currentFrame);
        f.Signals.OnAnimatorRootMotion3D(entity, deltaFrame, currentFrame);
        f.Signals.OnAnimatorRootMotion2D(entity, deltaFrame, currentFrame);
      }
    }

    public void OnAdded(Frame f, EntityRef entity, AnimatorComponent* component)
    {
      component->Self = entity;
      if (component->AnimatorGraph.Id != default)
      {
        var animatorGraphAsset = f.FindAsset<AnimatorGraph>(component->AnimatorGraph.Id);
        AnimatorComponent.SetAnimatorGraph(f, component, animatorGraphAsset);
      }
    }

    public void OnRemoved(Frame f, EntityRef entity, AnimatorComponent* component)
    {
      if (component->AnimatorVariables.Ptr != default)
      {
        f.FreeList(component->AnimatorVariables);
      }
    }
  }
}