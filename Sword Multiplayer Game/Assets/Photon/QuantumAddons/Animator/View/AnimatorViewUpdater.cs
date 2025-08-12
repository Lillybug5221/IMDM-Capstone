namespace Quantum.Addons.Animator
{
  using System.Collections.Generic;
  using UnityEngine;

  public unsafe class AnimatorViewUpdater : QuantumSceneViewComponent
  {
    /// <summary>
    /// Changes to uses the IAnimatorEntityViewComponent interface instead of just AnimatorPlayables
    /// </summary>
    private Dictionary<EntityRef, IAnimatorEntityViewComponent>
      _animatorEntityViewComponent = new Dictionary<EntityRef, IAnimatorEntityViewComponent>();

        private List<EntityRef> _removedEntities = new List<EntityRef>();

        public override void OnUpdateView()
        {
            var frame = Game.Frames.Predicted;

      // Remove destroyed entities
      foreach (var kvp in _animatorEntityViewComponent)
      {
        if (frame.Exists(kvp.Key) == false)
        {
          _removedEntities.Add(kvp.Key);
        }
      }

      for (int i = 0; i < _removedEntities.Count; i++)
      {
        _animatorEntityViewComponent.Remove(_removedEntities[i]);
      }

      // Animate
      var animators = frame.Filter<AnimatorComponent>();
      while (animators.NextUnsafe(out var entity, out var animator) == true)
      {
        var entityView = Updater.GetView(entity);
        if (entityView == null)
        {
          continue;
        }

        if (_animatorEntityViewComponent.TryGetValue(entity, out var ap) == false)
        {
          var animatorViewComponent = entityView.GetComponent<IAnimatorEntityViewComponent>();
          if (animatorViewComponent != null)
          {
            _animatorEntityViewComponent.Add(entity, animatorViewComponent);
            animatorViewComponent.Init(frame, animator);
          }
          else
          {
            Debug.LogWarning(
              $"[Quantum Animator] Trying to update animations of entity {entity} but it's EntityView does not have a {nameof(IAnimatorEntityViewComponent)}. Please add the component" +
              $" {nameof(AnimatorMecanim)} or {nameof(AnimatorPlayables)}.");
          }
        }

        if (ap != null)
        {
          ap.Animate(frame, animator);
        }
      }
    }
  }
}