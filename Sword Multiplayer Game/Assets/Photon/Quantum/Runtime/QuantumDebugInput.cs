namespace Quantum {
  using Photon.Deterministic;
  using UnityEngine;

  /// <summary>
  /// A Unity script that creates empty input for any Quantum game.
  /// </summary>
  public class QuantumDebugInput : MonoBehaviour {

    private void OnEnable() {
      QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    /// <summary>
    /// Set an empty input when polled by the simulation.
    /// </summary>
    /// <param name="callback"></param>
    public void PollInput(CallbackPollInput callback) {
      
      Quantum.Input i = new Quantum.Input();
      float x = UnityEngine.Input.GetAxisRaw("Horizontal");
      float y = UnityEngine.Input.GetAxisRaw("Vertical");
      bool jump = UnityEngine.Input.GetButton("Jump");
      FPVector2 temp = new FPVector2(x.ToFP(), y.ToFP());
      //Debug.Log("vals are:" + temp + ". Jump is" + jump);
      i.LeftStickDirection = temp;
      i.Jump = jump;

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
