using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0, 2, -6);
    public float smoothSpeed = 5f;

    public float baseFOV = 60f;
    public float speedFOVFactor = 0.5f;

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

        // 🎯 目标位置（跟随玩家方向）
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // 平滑移动
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 🚀 预测视线方向（关键）
        Vector3 lookTarget = target.position;

        if (player != null)
        {
            Vector3 vel = player.GetVelocity();
            lookTarget += vel * 0.2f;
        }

        // 平滑旋转
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);

        // 🎥 动态FOV
        if (player != null)
        {
            float speed = player.GetSpeed();
            float targetFOV = baseFOV + speed * speedFOVFactor;

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
        }
    }
}