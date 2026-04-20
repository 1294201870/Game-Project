using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Grounded, Flying, Parachuting, Crashed }
    public PlayerState currentState = PlayerState.Grounded;

    [Header("绑定物体")]
    public Transform visuals;
    public Transform groundCheck;

    [Header("果汁感(特效与表现)")]
    public GameObject splatPrefab;
    // ★ 调大这个值！默认改成 0.05f，如果还被挡住，在面板里把它改成 0.1 甚至 0.2
    public float splatOffset = 0.05f;
    // ★ 新增：要隐藏的头部模型（拖入 Head_Cube）
    public GameObject headModel;

    [Header("动态肢体绑定(程序化动画)")]
    public Transform lArmJoint;
    public Transform rArmJoint;
    public Transform lLegJoint;
    public Transform rLegJoint;

    [Header("肢体动态角度偏移量 (相对初始站姿)")]
    public float limbTransitionSpeed = 6f;
    public Vector3 lArmSpreadOffset = new Vector3(0, 0, 70);
    public Vector3 rArmSpreadOffset = new Vector3(0, 0, -70);
    public Vector3 lLegSpreadOffset = new Vector3(0, 0, 30);
    public Vector3 rLegSpreadOffset = new Vector3(0, 0, -30);
    public Vector3 lArmTuckedOffset = new Vector3(0, 0, 10);
    public Vector3 rArmTuckedOffset = new Vector3(0, 0, -10);
    public Vector3 lLegTuckedOffset = new Vector3(0, 0, 5);
    public Vector3 rLegTuckedOffset = new Vector3(0, 0, -5);
    public Vector3 lArmParachuteOffset = new Vector3(150, 0, 20);
    public Vector3 rArmParachuteOffset = new Vector3(150, 0, -20);

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

    [Header("翼装物理")]
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
    public float parachuteFallSpeed = 4f;
    public float parachuteForwardSpeed = 6f;
    public float parachuteDrag = 3f;
    public float parachuteUpwardJerk = 10f;
    public float parachuteTurnSpeed = 45f;

    [Header("秋千摇摆参数")]
    public float swingAmplitude = 45f;
    public float swingFrequency = 3f;
    public float swingDamping = 0.8f;

    [Header("碰撞与死亡惩罚")]
    public float fatalImpactSpeed = 18f;

    private Rigidbody rb;
    private Collider mainCollider;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private Quaternion targetVisualRotation;
    private float jumpTime;
    private float landedTime;
    private float lastHopTime;

    private float currentPitch;
    private float currentYaw;
    private float currentRoll;

    private float parachuteDeployTime;
    private float currentSwingAmplitude;

    private Transform mainCameraTransform;
    private string crashMessage = "";

    private Quaternion baseLArmRot;
    private Quaternion baseRArmRot;
    private Quaternion baseLLegRot;
    private Quaternion baseRLegRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCollider = GetComponent<Collider>();
        InitRagdoll();

        if (lArmJoint != null) baseLArmRot = lArmJoint.localRotation;
        if (rArmJoint != null) baseRArmRot = rArmJoint.localRotation;
        if (lLegJoint != null) baseLLegRot = lLegJoint.localRotation;
        if (rLegJoint != null) baseRLegRot = rLegJoint.localRotation;
    }

    void Start()
    {
        targetVisualRotation = visuals.localRotation;
        landedTime = Time.time - groundedStableTime;

        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    void InitRagdoll()
    {
        ragdollRigidbodies = visuals.GetComponentsInChildren<Rigidbody>();
        ragdollColliders = visuals.GetComponentsInChildren<Collider>();

        foreach (var col in ragdollColliders)
        {
            if (col != mainCollider)
            {
                Physics.IgnoreCollision(mainCollider, col, true);
                foreach (var otherCol in ragdollColliders)
                {
                    if (col != otherCol)
                        Physics.IgnoreCollision(col, otherCol, true);
                }
            }
        }
        SetRagdollState(false);
    }

    void SetRagdollState(bool isRagdoll)
    {
        foreach (var r in ragdollRigidbodies)
        {
            if (r != rb)
            {
                r.isKinematic = !isRagdoll;
                r.useGravity = isRagdoll;
            }
        }
        foreach (var c in ragdollColliders)
        {
            if (c != mainCollider)
                c.enabled = isRagdoll;
        }
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
            case PlayerState.Grounded: HandleGroundMovement(); break;
            case PlayerState.Flying: HandleFlying(); break;
            case PlayerState.Parachuting: HandleParachuting(); break;
        }

        visuals.localRotation = Quaternion.Lerp(visuals.localRotation, targetVisualRotation, Time.deltaTime * rotationSpeed);
        UpdateLimbAnimations();
    }

    void UpdateLimbAnimations()
    {
        Vector3 offsetLArm = Vector3.zero;
        Vector3 offsetRArm = Vector3.zero;
        Vector3 offsetLLeg = Vector3.zero;
        Vector3 offsetRLeg = Vector3.zero;

        if (currentState == PlayerState.Flying)
        {
            float diveRatio = Mathf.Clamp01(currentPitch / maxPitchDown);
            offsetLArm = Vector3.Lerp(lArmSpreadOffset, lArmTuckedOffset, diveRatio);
            offsetRArm = Vector3.Lerp(rArmSpreadOffset, rArmTuckedOffset, diveRatio);
            offsetLLeg = Vector3.Lerp(lLegSpreadOffset, lLegTuckedOffset, diveRatio);
            offsetRLeg = Vector3.Lerp(rLegSpreadOffset, rLegTuckedOffset, diveRatio);
        }
        else if (currentState == PlayerState.Parachuting)
        {
            offsetLArm = lArmParachuteOffset;
            offsetRArm = rArmParachuteOffset;
        }

        if (lArmJoint != null) lArmJoint.localRotation = Quaternion.Slerp(lArmJoint.localRotation, baseLArmRot * Quaternion.Euler(offsetLArm), Time.deltaTime * limbTransitionSpeed);
        if (rArmJoint != null) rArmJoint.localRotation = Quaternion.Slerp(rArmJoint.localRotation, baseRArmRot * Quaternion.Euler(offsetRArm), Time.deltaTime * limbTransitionSpeed);
        if (lLegJoint != null) lLegJoint.localRotation = Quaternion.Slerp(lLegJoint.localRotation, baseLLegRot * Quaternion.Euler(offsetLLeg), Time.deltaTime * limbTransitionSpeed);
        if (rLegJoint != null) rLegJoint.localRotation = Quaternion.Slerp(rLegJoint.localRotation, baseRLegRot * Quaternion.Euler(offsetRLeg), Time.deltaTime * limbTransitionSpeed);
    }

    void CheckAltitude()
    {
        if (Time.time - jumpTime < 0.2f) return;

        if (Physics.Raycast(groundCheck.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
        {
            if (!hit.transform.IsChildOf(transform))
            {
                if (rb.velocity.magnitude > fatalImpactSpeed)
                {
                    TriggerCrash("速度太快！双腿粉碎，变成了一滩果汁！\n(按 R 键重试)", hit.point, hit.normal);
                }
                else if (currentState == PlayerState.Flying || currentState == PlayerState.Parachuting)
                {
                    LandSafely();
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.IsChildOf(transform)) return;
        if (currentState == PlayerState.Crashed) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        bool isHeadFirst = currentPitch > 40f && currentState == PlayerState.Flying;

        Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 contactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        if (isHeadFirst)
        {
            TriggerCrash("脸先着地！致命倒栽葱！变成果汁！\n(按 R 键重试)", contactPoint, contactNormal);
        }
        else if (impactSpeed > fatalImpactSpeed)
        {
            TriggerCrash($"时速 {Mathf.RoundToInt(impactSpeed * 3.6f)} km/h 撞击！变成果汁！\n(按 R 键重试)", contactPoint, contactNormal);
        }
        else if (currentState == PlayerState.Flying || currentState == PlayerState.Parachuting)
        {
            LandSafely();
        }
    }

    void LandSafely()
    {
        currentState = PlayerState.Grounded;
        targetVisualRotation = Quaternion.Euler(0f, 0f, 0f);
        landedTime = Time.time;

        rb.useGravity = true;
        rb.drag = 0f;
        rb.velocity = new Vector3(rb.velocity.x * 0.3f, 0, rb.velocity.z * 0.3f);

        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        currentRoll = 0f;
    }

    void TriggerCrash(string reason, Vector3 contactPoint, Vector3 contactNormal)
    {
        crashMessage = reason;
        currentState = PlayerState.Crashed;

        Vector3 impactVelocity = rb.velocity;

        rb.isKinematic = true;
        mainCollider.enabled = false;
        SetRagdollState(true);

        // ★ 隐藏头部（爆头效果）
        if (headModel != null)
        {
            headModel.SetActive(false);
        }

        foreach (var r in ragdollRigidbodies)
        {
            if (r != rb)
            {
                r.velocity = impactVelocity;
                r.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }
        }

        if (splatPrefab != null)
        {
            // 利用 splatOffset 把贴花沿着表面法线向外推，防止被地面吃掉
            Vector3 spawnPos = contactPoint + contactNormal * splatOffset;

            Quaternion spawnRot = Quaternion.LookRotation(-contactNormal);
            spawnRot *= Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            GameObject splat = Instantiate(splatPrefab, spawnPos, spawnRot);

            float randomScale = Random.Range(0.8f, 1.5f);
            splat.transform.localScale = splatPrefab.transform.localScale * randomScale;
        }
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
        if (Input.GetKeyDown(KeyCode.F))
        {
            currentState = PlayerState.Parachuting;
            targetVisualRotation = Quaternion.Euler(0f, 0f, 0f);
            currentRoll = 0f;

            parachuteDeployTime = Time.time;
            currentSwingAmplitude = swingAmplitude;

            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y + parachuteUpwardJerk, rb.velocity.z);
            rb.useGravity = false;
            rb.drag = 0f;
            return;
        }

        rb.useGravity = false;
        rb.drag = 0f;

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

        currentYaw += horizontal * parachuteTurnSpeed * Time.deltaTime;

        float timeSinceDeploy = Time.time - parachuteDeployTime;
        currentSwingAmplitude = Mathf.Lerp(currentSwingAmplitude, 0f, Time.deltaTime * swingDamping);
        currentPitch = Mathf.Sin(timeSinceDeploy * swingFrequency) * currentSwingAmplitude;
        float playerInputPitch = vertical * 15f;

        transform.rotation = Quaternion.Euler(currentPitch + playerInputPitch, currentYaw, 0f);

        Vector3 vel = rb.velocity;

        vel.y -= gravityForce * Time.deltaTime;
        float verticalDragCoef = gravityForce / parachuteFallSpeed;

        if (vel.y < 0)
        {
            float upwardDrag = -vel.y * verticalDragCoef;
            vel.y += upwardDrag * Time.deltaTime;
        }

        vel.x = Mathf.Lerp(vel.x, 0, parachuteDrag * Time.deltaTime);
        vel.z = Mathf.Lerp(vel.z, 0, parachuteDrag * Time.deltaTime);

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