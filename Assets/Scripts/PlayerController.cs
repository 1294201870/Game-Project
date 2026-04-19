using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Grounded, Flying, Parachuting, Crashed }
    public PlayerState currentState = PlayerState.Grounded;

    [Header("绑定物体")]
    public Transform visuals;
    public Transform groundCheck;

    [Header("姿态参数")]
    public float rotationSpeed = 5f;

    [Header("起飞跳跃参数")]
    public float jumpForce = 15f;
    public Vector3 jumpDirection = new Vector3(0, 1, 0.8f);
    public float groundedStableTime = 0.2f;
    public float groundCheckDistance = 0.5f;

    [Header("地面移动参数")]
    public float hopUpForce = 2f;
    public float hopMoveForce = 3f;
    public float hopInterval = 0.3f;
    public float turnSpeed = 15f;

    [Header("翼装物理(完美纸飞机引擎)")]
    public float gravityForce = 20f;
    public float minDrag = 0.002f;
    public float maxDrag = 0.02f;
    public float glideResponsiveness = 3f;
    public float stallSpeed = 12f;
    public float pitchTurnRate = 120f;
    public float yawTurnRate = 90f;
    public float maxRollAngle = 45f;
    public float rollSpeed = 5f;
    public float maxPitchDown = 80f;
    public float maxPitchUp = -60f;

    [Header("降落伞参数")]
    public float parachuteFallSpeed = 4f;      // 最终稳定的下坠速度
    public float parachuteForwardSpeed = 6f;
    public float parachuteDrag = 3f;           // ★ 空气阻力：决定开伞后减速的平滑度，越大刹车越猛
    public float parachuteUpwardJerk = 10f;    // ★ 开伞瞬间把人往上拽的力
    public float parachuteTurnSpeed = 45f;

    // ★ 秋千摇摆参数
    public float swingAmplitude = 45f;         // 初始摇摆幅度(角度)
    public float swingFrequency = 3f;          // 摇摆的频率(速度)
    public float swingDamping = 0.8f;          // 摇摆衰减速度(越小摇得越久)

    [Header("碰撞与死亡惩罚")]
    public float fatalImpactSpeed = 18f;

    private Rigidbody rb;
    private Quaternion targetVisualRotation;
    private float jumpTime;
    private float landedTime;
    private float lastHopTime;

    private float currentPitch;
    private float currentYaw;
    private float currentRoll;

    // 降落伞内部变量
    private float parachuteDeployTime;
    private float currentSwingAmplitude;

    private Transform mainCameraTransform;
    private string crashMessage = "";

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetVisualRotation = visuals.localRotation;
        landedTime = Time.time - groundedStableTime;

        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (currentState == PlayerState.Crashed && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (currentState == PlayerState.Crashed) return;

        CheckAltitude();

        switch (currentState)
        {
            case PlayerState.Grounded:
                HandleGroundMovement();
                break;
            case PlayerState.Flying:
                HandleFlying();
                break;
            case PlayerState.Parachuting:
                HandleParachuting();
                break;
        }

        visuals.localRotation = Quaternion.Lerp(visuals.localRotation, targetVisualRotation, Time.deltaTime * rotationSpeed);
    }

    void CheckAltitude()
    {
        if (Time.time - jumpTime < 0.2f) return;

        if (Physics.Raycast(groundCheck.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            if (currentState == PlayerState.Flying || currentState == PlayerState.Parachuting)
            {
                if (rb.velocity.magnitude > fatalImpactSpeed)
                {
                    TriggerCrash("速度太快！双腿粉碎，变成了一滩果汁！\n(按 R 键重试)");
                }
                else
                {
                    LandSafely();
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (currentState == PlayerState.Flying || currentState == PlayerState.Parachuting)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            bool isHeadFirst = currentPitch > 40f && currentState == PlayerState.Flying;

            if (isHeadFirst)
            {
                TriggerCrash("脸先着地！致命倒栽葱！变成果汁！\n(按 R 键重试)");
            }
            else if (impactSpeed > fatalImpactSpeed)
            {
                TriggerCrash($"时速 {Mathf.RoundToInt(impactSpeed * 3.6f)} km/h 撞击！变成果汁！\n(按 R 键重试)");
            }
            else
            {
                LandSafely();
            }
        }
    }

    void LandSafely()
    {
        currentState = PlayerState.Grounded;
        targetVisualRotation = Quaternion.Euler(0f, 0f, 0f);
        landedTime = Time.time;

        rb.useGravity = true;
        rb.drag = 0f; // 落地时清空降落伞的阻力
        rb.velocity = new Vector3(rb.velocity.x * 0.3f, 0, rb.velocity.z * 0.3f);

        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        currentRoll = 0f;
    }

    void TriggerCrash(string reason)
    {
        crashMessage = reason;
        currentState = PlayerState.Crashed;

        rb.useGravity = true;
        rb.drag = 0f;
        rb.constraints = RigidbodyConstraints.None;
        rb.AddTorque(Random.insideUnitSphere * 50f, ForceMode.Impulse);
    }

    void HandleGroundMovement()
    {
        bool isStable = (Time.time - landedTime) >= groundedStableTime;
        if (!isStable) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 actualJumpDir = transform.TransformDirection(jumpDirection.normalized);
            rb.velocity = Vector3.zero;
            rb.AddForce(actualJumpDir * jumpForce, ForceMode.Impulse);

            currentYaw = transform.eulerAngles.y;
            currentPitch = 0f;
            currentRoll = 0f;

            currentState = PlayerState.Flying;
            jumpTime = Time.time;
            targetVisualRotation = Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 moveDir = Vector3.zero;
            if (mainCameraTransform != null)
            {
                Vector3 camForward = mainCameraTransform.forward;
                camForward.y = 0f;
                camForward.Normalize();
                Vector3 camRight = mainCameraTransform.right;
                camRight.y = 0f;
                camRight.Normalize();
                moveDir = camForward * inputDir.z + camRight * inputDir.x;
                moveDir.Normalize();
            }
            else moveDir = inputDir;

            Quaternion lookRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * turnSpeed);

            if (Time.time - lastHopTime > hopInterval && rb.velocity.y <= 0.1f)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
                Vector3 hopForce = (Vector3.up * hopUpForce) + (moveDir * hopMoveForce);
                rb.AddForce(hopForce, ForceMode.Impulse);
                lastHopTime = Time.time;
            }
        }
    }

    void HandleFlying()
    {
        // ★ 开伞指令！
        if (Input.GetKeyDown(KeyCode.F))
        {
            currentState = PlayerState.Parachuting;
            targetVisualRotation = Quaternion.Euler(0f, 0f, 0f);
            currentRoll = 0f;

            // 记录开伞时刻，重置摇摆幅度
            parachuteDeployTime = Time.time;
            currentSwingAmplitude = swingAmplitude;

            // ★ 开伞瞬间：不仅不向下掉，反而给你一个巨大的向上提拉力！
            rb.velocity = new Vector3(rb.velocity.x, parachuteUpwardJerk, rb.velocity.z);

            // 开启 Unity 的阻力系统来实现顺滑刹车
            rb.drag = parachuteDrag;
            rb.useGravity = true;
            return;
        }

        rb.useGravity = false;
        rb.drag = 0f; // 飞行时不使用 Unity 阻力，用我们的空气动力学

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 velocity = rb.velocity;
        float currentSpeed = velocity.magnitude;

        float stallRatio = Mathf.InverseLerp(stallSpeed * 0.5f, stallSpeed * 1.2f, currentSpeed);

        if (stallRatio < 1f)
        {
            if (vertical < 0) vertical = 0;
            currentPitch = Mathf.Lerp(currentPitch, maxPitchDown, Time.deltaTime * (1f - stallRatio) * 3f);
        }

        currentPitch += vertical * pitchTurnRate * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, maxPitchUp, maxPitchDown);
        currentYaw += horizontal * yawTurnRate * Time.deltaTime;

        float targetRoll = -horizontal * maxRollAngle;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSpeed);
        transform.rotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);

        velocity += Vector3.down * gravityForce * Time.deltaTime;
        currentSpeed = velocity.magnitude;

        if (currentSpeed > 0.1f)
        {
            float angleOfAttack = Vector3.Angle(velocity.normalized, transform.forward);
            float currentDragCoef = Mathf.Lerp(minDrag, maxDrag, angleOfAttack / 90f);

            Vector3 dragForce = velocity.normalized * (currentSpeed * currentSpeed * currentDragCoef);
            velocity -= dragForce * Time.deltaTime;
            currentSpeed = velocity.magnitude;

            float currentGlide = glideResponsiveness * stallRatio;
            Vector3 targetGlideDirection = Vector3.Slerp(velocity.normalized, transform.forward, currentGlide * Time.deltaTime);

            velocity = targetGlideDirection * currentSpeed;
        }

        rb.velocity = velocity;
    }

    void HandleParachuting()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 1. 降落伞状态下缓慢转弯(Yaw)
        currentYaw += horizontal * parachuteTurnSpeed * Time.deltaTime;

        // 2. ★ 核心摇摆算法 (荡秋千)
        float timeSinceDeploy = Time.time - parachuteDeployTime;

        // 随着时间推移，摇摆幅度越来越小
        currentSwingAmplitude = Mathf.Lerp(currentSwingAmplitude, 0f, Time.deltaTime * swingDamping);

        // 使用正弦波计算当前的 Pitch 角度 (前后摇摆)
        currentPitch = Mathf.Sin(timeSinceDeploy * swingFrequency) * currentSwingAmplitude;

        // 加上玩家主动按 W/S 的倾斜
        float playerInputPitch = vertical * 15f;

        // 将摇摆角度应用给模型
        transform.rotation = Quaternion.Euler(currentPitch + playerInputPitch, currentYaw, 0f);

        // 3. 物理受力：限制下坠极限，并施加向前的漂移力
        Vector3 vel = rb.velocity;

        // 当阻力把你刹停后，你只会以安全速度平稳下降
        if (vel.y < -parachuteFallSpeed)
        {
            vel.y = -parachuteFallSpeed;
        }

        // 按 W 给一点微弱的前冲力
        vel += transform.forward * vertical * parachuteForwardSpeed * Time.deltaTime;

        rb.velocity = vel;
    }

    public Vector3 GetVelocity() { return rb != null ? rb.velocity : Vector3.zero; }
    public float GetSpeed() { return rb != null ? rb.velocity.magnitude : 0f; }

    void OnGUI()
    {
        if (currentState == PlayerState.Crashed)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 40;
            style.normal.textColor = Color.red;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), crashMessage, style);
        }
    }
}