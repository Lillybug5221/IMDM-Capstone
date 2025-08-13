namespace Quantum {
  using UnityEngine;

  public class QuantumCameraFollow : QuantumEntityViewComponent<CustomViewContext> {
    public Vector3 MinOffset;
    public Vector3 MaxOffset;
    public float MinDistance = 5f;
    public float MaxDistance = 15f;
    public float LerpSpeed = 4;
    private bool _isPlayerLocal;
    private static Transform opponentTransform;
    public Transform TestTarget;

    public float smoothTime = 0.3f;

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
        
        Vector3 opponentDirection = opponentTransform.position - transform.position;
        opponentDirection = opponentDirection.normalized;

        float PlayerDistance = Vector3.Distance(opponentTransform.position, transform.position);
        float t = Mathf.InverseLerp(MinDistance, MaxDistance, PlayerDistance);
        Vector3 offset = Vector3.Lerp(MinOffset, MaxOffset, t);
        Vector3 temp = opponentDirection * offset.z;
        Vector3 directedOffset = new Vector3(temp.x, offset.y, temp.z);

        Vector3 targetPos = transform.position + directedOffset;

        // 5. Smooth camera movement
        ViewContext.MyCamera.transform.position = Vector3.SmoothDamp(ViewContext.MyCamera.transform.position, targetPos, ref velocity, smoothTime);

        // 6. always center camera on opponent
        ViewContext.MyCamera.transform.LookAt(opponentTransform.position);
        


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