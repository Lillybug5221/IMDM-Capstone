namespace Quantum.Asteroids
{
  using Photon.Deterministic;
  using UnityEngine;

  /// <summary>
  /// The <c>AsteroidsQuantumInput</c> class handles the input for the Asteroids game
  /// by subscribing to Quantum's input callback system.
  /// </summary>
  public class AsteroidsQuantumInput : MonoBehaviour
  {
    /// <summary>
    /// Subscribes to the Quantum input callback when the script is enabled.
    /// </summary>
    private void OnEnable()
    {
      QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    /// <summary>
    /// Polls the current input state and sets it in the Quantum input callback.
    /// </summary>
    /// <param name="callback">The input callback provided by Quantum.</param>
    public void PollInput(CallbackPollInput callback)
    {
      Quantum.Input i = new Quantum.Input();
#if ENABLE_INPUT_SYSTEM && QUANTUM_ENABLE_INPUTSYSTEM
      var keyboard = UnityEngine.InputSystem.Keyboard.current;
      if (keyboard != null) {
        i.Left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        i.Right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        i.Up = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        i.Fire = keyboard.spaceKey.isPressed;
      }
#elif ENABLE_LEGACY_INPUT_MANAGER
      i.Left = UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow);
      i.Right = UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow);
      i.Up = UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow);
      i.Fire = UnityEngine.Input.GetKey(KeyCode.Space);
#endif
      callback.SetInput(i, DeterministicInputFlags.Repeatable);
    }
  }
}