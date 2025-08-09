namespace Quantum {
  using UnityEngine;

  public class QuantumCameraFollow : QuantumEntityViewComponent<CameraViewContext> {
    public Vector3 Offset;
    public float LerpSpeed = 4;
    private bool _isPlayerLocal;
    private static Transform opponentTransform;
    public Transform TestTarget;

    public float smoothTime = 0.3f;
    public float minDistance = 5f; // Closest camera can be
    public float maxDistance = 15f; // Farthest camera can be
    public float maxTargetDistance = 10f; // Target distance that triggers max zoom

    private Vector3 velocity;

    public override void OnActivate(Frame frame) {
        TestTarget = GameObject.FindWithTag("TestCamTarget").transform;
      var playerLink = frame.Get<PlayerLink>(EntityRef);
      _isPlayerLocal = Game.PlayerIsLocal(playerLink.Player);

      if (_isPlayerLocal == false) {
        QuantumCameraFollow.opponentTransform = transform;
        return;
      }

    }

    public override void OnUpdateView() {
      if (_isPlayerLocal == false) {
        return;
      }
    /*
      if(QuantumCameraFollow.opponentTransform = null){
        return;
      }
    */
        if(opponentTransform == null){
            opponentTransform = TestTarget.transform;
        }
        

        // 1. Midpoint between both
        Vector3 midpoint = (transform.position + opponentTransform.position) / 2f;

        Vector3 opponentDirection = opponentTransform.position - transform.position;
        opponentDirection = opponentDirection.normalized;

        Vector3 targetPos = transform.position + Offset - (opponentDirection * 3);

        // 5. Smooth camera movement
        ViewContext.MyCamera.transform.position = Vector3.SmoothDamp(ViewContext.MyCamera.transform.position, targetPos, ref velocity, smoothTime);

        // 6. Always look at midpoint
        ViewContext.MyCamera.transform.LookAt(midpoint);
        


/*
      var myPosition = transform.position;
      var opponentPosition = myPosition;
      if(QuantumCameraFollow.opponentTransform != null){
        opponentPosition = QuantumCameraFollow.opponentTransform.position;
      }
      var middlePosition = Vector3.Lerp(myPosition, opponentPosition, 0.5f);
      var desiredPos = middlePosition + Offset;
      var currentCameraPos = ViewContext.MyCamera.transform.position;

      ViewContext.MyCamera.transform.position = Vector3.Lerp(currentCameraPos, desiredPos, Time.deltaTime * LerpSpeed);
      ViewContext.MyCamera.transform.LookAt(TestTarget);
      */
    }
  }
}