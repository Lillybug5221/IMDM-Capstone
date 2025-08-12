namespace Quantum {
  using Photon.Deterministic;
  using UnityEngine;
  using UnityEngine.InputSystem;

  /// <summary>
  /// A Unity script that creates empty input for any Quantum game.
  /// </summary>
  public class PlayerInputHandler : MonoBehaviour {

    private PlayerControls controls;
    [SerializeField]
    private Vector2 moveInput;
    [SerializeField]
    private bool jumpInput;
    [SerializeField]
    private bool lightAttack;

    private void Awake()
    {
        controls = new PlayerControls();

        // Movement
        controls.Gameplay.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Movement.canceled += ctx => moveInput = Vector2.zero;

        // Jump
        controls.Gameplay.Jump.performed += ctx => jumpInput = true;
        controls.Gameplay.Jump.canceled += ctx => jumpInput = false;

        // Light Attack
        controls.Gameplay.LightAttack.performed += ctx => lightAttack = true;
        controls.Gameplay.LightAttack.canceled += ctx => lightAttack = false;
        
    }

    private void OnEnable() {
      QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
      controls.Enable();
    }

    private void OnDisable() => controls.Disable();

    /// <summary>
    /// Set an empty input when polled by the simulation.
    /// </summary>
    /// <param name="callback"></param>
    public void PollInput(CallbackPollInput callback) {

      //find gamepad
      Gamepad gamepad = Gamepad.current;
      if(gamepad == null){return;}//no gamepad found
      
      Quantum.Input i = new Quantum.Input();
      i.LeftStickDirection = new FPVector2(moveInput.x.ToFP(),moveInput.y.ToFP());
      i.Jump = jumpInput;
      i.LightAttack = lightAttack;

#if DEBUG
      if (callback.IsInputSet) {
        Debug.LogWarning($"{nameof(QuantumDebugInput)}.{nameof(PollInput)}: Input was already set by another user script, unsubscribing from the poll input callback. Please delete this component.", this);
        QuantumCallback.UnsubscribeListener(this);
        return;
      }
#endif

      callback.SetInput(i, DeterministicInputFlags.Repeatable);
    }
  }
}

