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
    public float splatOffset = 0.05f;
    public GameObject headModel;

    [Header("降落伞视觉表现")]
    public GameObject parachuteVisual;
    public float parachuteDeployDuration = 0.5f;
    public float parachuteRopeLength = 4f;

    [Header("降落伞脱离(布娃娃)参数")]
    public float detachedChuteLifeTime = 6f;
    public bool detachedChuteHasCollision = true;
    public float detachedChuteMass = 0.1f;
    public float detachedChuteDrag = 3f;
    public float detachedChuteAngularDrag = 2f;

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

    [Header("风声效果")]
    public float minSpeed = 1f;
    public float maxSpeed = 30f;

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
    private float parachuteScaleProgress = 1f;
    private float parachuteInitialPitch;
    private Vector3 currentParachuteUp = Vector3.up;

    private Transform mainCameraTransform;
    private string crashMessage = "";

    private Quaternion baseLArmRot;
    private Quaternion baseRArmRot;
    private Quaternion baseLLegRot;
    private Quaternion baseRLegRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 开启刚体插值，让物理引擎与画面渲染平滑对齐，消除颤动！
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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

        if (parachuteVisual != null)
        {
            parachuteVisual.transform.SetParent(null);
            parachuteVisual.SetActive(false);
        }
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
        if (Input.GetKeyDown(KeyCode.P))
        {

            SceneManager.LoadScene("MainMenu");
            return;
        }
        if (currentState == PlayerState.Crashed) return;

        CheckAltitude();

        switch (currentState)
        {
            case PlayerState.Grounded: HandleGroundMovement(); break;
            case PlayerState.Flying: HandleFlying(); UpdateWindSfx(); break;
            case PlayerState.Parachuting: HandleParachuting(); UpdateWindSfx(); break;
        }

        visuals.localRotation = Quaternion.Lerp(visuals.localRotation, targetVisualRotation, Time.deltaTime * rotationSpeed);
        UpdateLimbAnimations();
    }

    // 降落伞的平滑跟随（世界坐标系）
    void LateUpdate()
    {
        if (currentState == PlayerState.Parachuting && parachuteVisual != null && parachuteVisual.activeSelf)
        {
            float timeSinceDeploy = Time.time - parachuteDeployTime;
            float controlBlend = Mathf.Clamp01(timeSinceDeploy / 1.5f);

            Vector3 windDir = Vector3.up;
            if (rb.velocity.magnitude > 0.1f)
            {
                windDir = -rb.velocity.normalized;
            }

            Vector3 targetUp = Vector3.Slerp(windDir, transform.up, controlBlend);
            currentParachuteUp = Vector3.Slerp(currentParachuteUp, targetUp, Time.deltaTime * 5f);

            Vector3 playerAnchor = transform.position + transform.up * 0.5f;
            parachuteVisual.transform.position = playerAnchor + currentParachuteUp * parachuteRopeLength;

            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = transform.forward;

            Vector3 trueForward = Vector3.ProjectOnPlane(flatForward, currentParachuteUp).normalized;
            if (trueForward.sqrMagnitude < 0.01f) trueForward = transform.forward;

            Quaternion baseRot = Quaternion.LookRotation(trueForward, currentParachuteUp);

            float verticalInput = Input.GetAxis("Vertical");
            Quaternion extraPitch = Quaternion.Euler(verticalInput * 20f * controlBlend, 0, 0);
            Quaternion targetRot = baseRot * extraPitch;

            parachuteVisual.transform.rotation = Quaternion.Slerp(parachuteVisual.transform.rotation, targetRot, Time.deltaTime * 8f);
        }
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

        // 核心修复：QueryTriggerInteraction.Ignore让摔死判定射线穿透圆环等触发器
        if (Physics.Raycast(groundCheck.position, Vector3.down, out RaycastHit hit, groundCheckDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(transform))
            {
                float verticalSpeed = rb.velocity.magnitude; // 总速度，也可只用y分量
                if (verticalSpeed > fatalImpactSpeed)
                {
                    // 原提示: "速度太快！双腿粉碎！\n(按 R 键重试,按P返回主菜单)"
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.ForceMuteAllWind();
                    TriggerCrash(
                        $"速度太快！时速 {Mathf.RoundToInt(verticalSpeed * 3.6f)} km/h 双腿粉碎！\n(按 R 键重试,按P返回主菜单)",
                        hit.point,
                        hit.normal
                    );
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
        // ★ 核心修复：如果碰到的物体是触发器（如光圈），无视它，绝不判定为撞墙
        if (collision.collider.isTrigger) return;

        if (collision.transform.IsChildOf(transform)) return;
        if (currentState == PlayerState.Crashed) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        bool isHeadFirst = currentPitch > 40f && currentState == PlayerState.Flying;

        Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 contactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up;

        if (isHeadFirst)
        {

            if (AudioManager.Instance != null)
                AudioManager.Instance.ForceMuteAllWind();
            TriggerCrash($"脸着地!时速 {Mathf.RoundToInt(impactSpeed * 3.6f)} km/h 撞击！\n(按 R 重试)", contactPoint, contactNormal);
        }
        else if (impactSpeed > fatalImpactSpeed)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.ForceMuteAllWind();
            TriggerCrash($"时速 {Mathf.RoundToInt(impactSpeed * 3.6f)} km/h 撞击！\n(按 R 重试)", contactPoint, contactNormal);
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

        // ★ 修复物理抽搐：落地时也使用刚体API旋转
        rb.MoveRotation(Quaternion.Euler(0, transform.eulerAngles.y, 0));
        currentRoll = 0f;

        if (parachuteVisual != null && parachuteVisual.activeSelf)
        {
            DetachParachute();
        }

        // ★ 通知 GameManager 成功落地
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndGame(true);
        }
    }

    void TriggerCrash(string reason, Vector3 contactPoint, Vector3 contactNormal)
    {
        crashMessage = reason;
        currentState = PlayerState.Crashed;

        Vector3 impactVelocity = rb.velocity;

        rb.isKinematic = true;
        mainCollider.enabled = false;
        SetRagdollState(true);

        if (headModel != null) headModel.SetActive(false);

        if (parachuteVisual != null && parachuteVisual.activeSelf)
        {
            DetachParachute();
        }
        try { AudioManager.Instance.PlayCrash(); }
        catch { };


        // ★ 通知 GameManager 死亡
        if (GameManager.Instance != null)
        {
            
            GameManager.Instance.EndGame(false);
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
            Vector3 spawnPos = contactPoint + contactNormal * splatOffset;
            Quaternion spawnRot = Quaternion.LookRotation(-contactNormal);
            spawnRot *= Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            GameObject splat = Instantiate(splatPrefab, spawnPos, spawnRot);
            float randomScale = Random.Range(0.8f, 1.5f);
            splat.transform.localScale = splatPrefab.transform.localScale * randomScale;
        }
    }

    void DetachParachute()
    {
        GameObject deadChute = Instantiate(parachuteVisual, parachuteVisual.transform.position, parachuteVisual.transform.rotation);
        parachuteVisual.SetActive(false);

        Destroy(deadChute.GetComponent<ParachuteRopes>());
        foreach (Transform child in deadChute.transform)
        {
            if (child.name.StartsWith("DynamicRope")) Destroy(child.gameObject);
        }

        Transform canopy = deadChute.transform.Find("Canopy");
        if (canopy != null)
        {
            Transform center = canopy.Find("Center_Canopy");
            Transform left = canopy.Find("Left_Canopy");
            Transform right = canopy.Find("Right_Canopy");

            if (center != null && left != null && right != null)
            {
                Rigidbody rbCenter = AddSoftPhysics(center.gameObject);
                Rigidbody rbLeft = AddSoftPhysics(left.gameObject);
                Rigidbody rbRight = AddSoftPhysics(right.gameObject);

                Vector3 detachVel = rb.velocity * 0.5f + Vector3.up * 4f - transform.forward * 2f;
                rbCenter.velocity = detachVel;
                rbLeft.velocity = detachVel;
                rbRight.velocity = detachVel;

                rbCenter.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);

                CharacterJoint jointL = left.gameObject.AddComponent<CharacterJoint>();
                jointL.connectedBody = rbCenter;

                CharacterJoint jointR = right.gameObject.AddComponent<CharacterJoint>();
                jointR.connectedBody = rbCenter;

                left.SetParent(deadChute.transform);
                right.SetParent(deadChute.transform);
                center.SetParent(deadChute.transform);
            }
        }

        Destroy(deadChute, detachedChuteLifeTime);
    }

    Rigidbody AddSoftPhysics(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col == null) col = obj.AddComponent<BoxCollider>();

        col.isTrigger = !detachedChuteHasCollision;

        Rigidbody r = obj.GetComponent<Rigidbody>();
        if (r == null) r = obj.AddComponent<Rigidbody>();

        r.mass = detachedChuteMass;
        r.drag = detachedChuteDrag;
        r.angularDrag = detachedChuteAngularDrag;
        r.useGravity = true;

        return r;
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

            // ★ 修复物理抽搐：使用刚体API旋转
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.deltaTime * turnSpeed));

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
            try { AudioManager.Instance.PlayParachute(); }
            catch { };
            currentState = PlayerState.Parachuting;
            targetVisualRotation = Quaternion.Euler(0f, 0f, 0f);

            parachuteDeployTime = Time.time;
            parachuteInitialPitch = currentPitch;
            currentSwingAmplitude = swingAmplitude;

            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y + parachuteUpwardJerk, rb.velocity.z);
            rb.useGravity = false;
            rb.drag = 0f;

            if (parachuteVisual != null)
            {
                parachuteScaleProgress = 0f;
                parachuteVisual.transform.localScale = Vector3.zero;

                Vector3 windDir = -rb.velocity.normalized;
                if (windDir == Vector3.zero) windDir = Vector3.up;

                currentParachuteUp = windDir;
                parachuteVisual.transform.position = transform.position + windDir * parachuteRopeLength;

                parachuteVisual.SetActive(true);
            }

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

        // ★ 修复物理抽搐：使用刚体API旋转
        rb.MoveRotation(Quaternion.Euler(currentPitch, currentYaw, currentRoll));

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
        if (parachuteVisual != null && parachuteScaleProgress < 1f)
        {
            parachuteScaleProgress += Time.deltaTime / parachuteDeployDuration;
            float scale = Mathf.Clamp01(parachuteScaleProgress);
            parachuteVisual.transform.localScale = Vector3.one * scale;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        currentYaw += horizontal * parachuteTurnSpeed * Time.deltaTime;

        float timeSinceDeploy = Time.time - parachuteDeployTime;
        currentSwingAmplitude = Mathf.Lerp(currentSwingAmplitude, 0f, Time.deltaTime * swingDamping);

        float swingPitch = Mathf.Sin(timeSinceDeploy * swingFrequency) * currentSwingAmplitude;
        float transitionFactor = Mathf.Clamp01(timeSinceDeploy * 2f);
        float baselinePitch = Mathf.Lerp(parachuteInitialPitch, 0f, transitionFactor);

        currentPitch = baselinePitch + swingPitch;
        currentRoll = Mathf.Lerp(currentRoll, 0f, Time.deltaTime * 5f);

        float playerInputPitch = vertical * 15f;

        // ★ 修复物理抽搐：使用刚体API旋转
        rb.MoveRotation(Quaternion.Euler(currentPitch + playerInputPitch, currentYaw, currentRoll));

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

    void UpdateWindSfx()
    {
        float normSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, rb.velocity.magnitude);
        try { AudioManager.Instance.UpdateWindSound(normSpeed); }
        catch { }


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