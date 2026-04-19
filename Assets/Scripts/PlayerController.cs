using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Speed")]
    public float minSpeed = 10f;
    public float maxSpeed = 50f;

    [Header("Control")]
    public float turnSpeed = 60f;
    public float pitchSpeed = 40f;
    public float airControl = 0.8f;   // 越小越“重”

    [Header("Input Smooth")]
    public float inputSmooth = 2f;

    [Header("Physics")]
    public float gravity = 15f;
    public float drag = 0.2f;

    private Vector3 velocity;

    private float smoothH;
    private float smoothV;

    void Start()
    {
        velocity = transform.forward * 20f;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 🎮 输入平滑（关键）
        smoothH = Mathf.Lerp(smoothH, h, inputSmooth * Time.deltaTime);
        smoothV = Mathf.Lerp(smoothV, v, inputSmooth * Time.deltaTime);

        // ✈️ 控制朝向
        transform.Rotate(Vector3.up, smoothH * turnSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right, smoothV * pitchSpeed * Time.deltaTime);

        // 当前速度大小
        float speed = velocity.magnitude;

        // 🎯 目标方向
        Vector3 targetVelocity = transform.forward * speed;

        // 🪂 惯性（不会瞬间转向）
        velocity = Vector3.Lerp(velocity, targetVelocity, airControl * Time.deltaTime);

        // 🌍 重力
        velocity += Vector3.down * gravity * Time.deltaTime;

        // 🌬️ 空气阻力
        velocity *= (1f - drag * Time.deltaTime);

        // 🚀 俯冲加速（来自方向）
        float diveFactor = Vector3.Dot(transform.forward, Vector3.down);
        velocity += transform.forward * diveFactor * 10f * Time.deltaTime;

        // 🛑 限制速度
        float newSpeed = Mathf.Clamp(velocity.magnitude, minSpeed, maxSpeed);
        velocity = velocity.normalized * newSpeed;

        // 🪂 移动
        transform.position += velocity * Time.deltaTime;

        // 🎯 视觉倾斜（Roll）
        float roll = -smoothH * 45f;
        Vector3 euler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(euler.x, euler.y, roll);
    }

    // 💥 碰撞减震（防抖）
    void OnCollisionEnter(Collision collision)
    {
        velocity *= 0.3f;
    }

    void OnCollisionStay(Collision collision)
    {
        velocity *= 0.9f;
    }

    // 🎥 给摄像机用
    public Vector3 GetVelocity()
    {
        return velocity;
    }

    public float GetSpeed()
    {
        return velocity.magnitude;
    }
}