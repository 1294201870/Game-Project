using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("跟随参数")]
    public Vector3 offset = new Vector3(0, 2, -5);
    public float positionSmooth = 5f;
    public float rotationSmooth = 3f;

    [Header("视野动态参数")]
    public float baseFOV = 60f;
    public float speedFOVFactor = 0.5f;
    public float maxFOV = 90f;

    [Header("视线与动态侧倾")]
    public float lookAheadFactor = 2f;
    public float maxLookAheadDistance = 4f;
    [Range(0f, 1f)]
    public float cameraRollRatio = 0.4f;

    private Camera cam;
    private PlayerController player;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target != null)
        {
            player = target.GetComponent<PlayerController>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 获取玩家实际侧倾，相机按比例同步倾斜
        float targetRoll = Mathf.DeltaAngle(0, target.eulerAngles.z);
        float cameraRoll = targetRoll * cameraRollRatio;

        // 2. 目标位置计算
        Quaternion baseRotation = Quaternion.Euler(target.eulerAngles.x, target.eulerAngles.y, cameraRoll);
        Vector3 desiredPosition = target.position + baseRotation * offset;

        // ★ 优化点：在使用刚体插值(Interpolation)时，相机的跟随也应当更加平滑
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmooth * Time.deltaTime);

        // 3. 视线预测
        Vector3 lookTarget = target.position;
        if (player != null)
        {
            Vector3 velOffset = player.GetVelocity() * 0.1f;
            velOffset = Vector3.ClampMagnitude(velOffset, maxLookAheadDistance);
            lookTarget += target.forward * lookAheadFactor + velOffset;
        }

        // 4. 旋转相机并加上侧倾 Roll
        Quaternion lookDirection = Quaternion.LookRotation(lookTarget - transform.position);
        Quaternion targetRot = lookDirection * Quaternion.Euler(0, 0, cameraRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmooth);

        // 5. 动态 FOV 控制
        if (player != null)
        {
            float speed = player.GetSpeed();
            float targetFOV = baseFOV + (speed * speedFOVFactor);
            targetFOV = Mathf.Clamp(targetFOV, baseFOV, maxFOV);

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
        }
    }
}