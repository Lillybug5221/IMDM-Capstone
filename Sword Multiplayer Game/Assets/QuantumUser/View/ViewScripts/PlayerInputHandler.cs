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
    private bool jump;
    [SerializeField]
    private bool dodge;
    [SerializeField]
    private bool lightAttack;
    [SerializeField]
    private bool heavyAttack;
    [SerializeField]
    private bool parry;
    [SerializeField]
    private bool special;

    [SerializeField]
    private bool jumpHeld;
    [SerializeField]
    private bool dodgeHeld;
    [SerializeField]
    private bool lightAttackHeld;
    [SerializeField]
    private bool heavyAttackHeld;
    [SerializeField]
    private bool parryHeld;
    [SerializeField]
    private bool specialHeld;

    private void Awake()
    {
        controls = new PlayerControls();

        // Movement
        controls.Gameplay.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Movement.canceled += ctx => moveInput = Vector2.zero;

        // Jump
        controls.Gameplay.Jump.started += ctx => jump = true;
        controls.Gameplay.Jump.canceled += ctx => jump = false;
        controls.Gameplay.Jump.started += ctx => jumpHeld = true;
        controls.Gameplay.Jump.canceled += ctx => jumpHeld = false;

        // Dodge
        controls.Gameplay.Dodge.started += ctx => dodge = true;
        controls.Gameplay.Dodge.canceled += ctx => dodge = false;
        controls.Gameplay.Dodge.started += ctx => dodgeHeld = true;
        controls.Gameplay.Dodge.canceled += ctx => dodgeHeld = false;

        // Light Attack
        controls.Gameplay.LightAttack.started += ctx => lightAttack = true;
        controls.Gameplay.LightAttack.canceled += ctx => lightAttack = false;
        controls.Gameplay.LightAttack.started += ctx => lightAttackHeld = true;
        controls.Gameplay.LightAttack.canceled += ctx => lightAttackHeld = false;

        //heavy
        controls.Gameplay.HeavyAttack.started += ctx => heavyAttack = true;
        controls.Gameplay.HeavyAttack.canceled += ctx => heavyAttack = false;
        controls.Gameplay.HeavyAttack.started += ctx => heavyAttackHeld = true;
        controls.Gameplay.HeavyAttack.canceled += ctx => heavyAttackHeld = false;

        ////parry
        controls.Gameplay.Parry.started += ctx => parry = true;
        controls.Gameplay.Parry.canceled += ctx => parry = false;
        controls.Gameplay.Parry.started += ctx => parryHeld = true;
        controls.Gameplay.Parry.canceled += ctx => parryHeld = false;

        //special
        controls.Gameplay.Special.started += ctx => special = true;
        controls.Gameplay.Special.canceled += ctx => special = false;
        controls.Gameplay.Special.started += ctx => specialHeld = true;
        controls.Gameplay.Special.canceled += ctx => specialHeld = false;
        
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
      
      Quantum.Input i = new Quantum.Input();
      i.LeftStickDirection = new FPVector2(moveInput.x.ToFP(),moveInput.y.ToFP());
      i.Jump = jump;
      i.JumpHeld = jumpHeld;
      i.Dodge = dodge;
      i.DodgeHeld = dodgeHeld;
      i.LightAttack = lightAttack;
      i.LightAttackHeld = lightAttackHeld;
      i.HeavyAttack = heavyAttack;
      i.HeavyAttackHeld = heavyAttackHeld;
      i.Parry = parry;
      i.ParryHeld = parryHeld;
      i.Special = special;
      i.SpecialHeld = specialHeld;

      if(jump){jump = false;}
      if(dodge){dodge = false;}
      if(lightAttack){lightAttack = false;}
      if(heavyAttack){heavyAttack = false;}
      if(parry){parry = false;}
      if(special){special = false;}

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

