using UnityEngine;

// 强制要求挂载这个脚本的物体，必须带有线段渲染器和球形碰撞体
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(SphereCollider))]
public class GuideRing : MonoBehaviour
{
    [Header("圆环视觉参数 (纯代码画圆)")]
    public float radius = 10f;       // 圆环的半径（大小）
    public float thickness = 1f;     // 圆环的粗细
    public int segments = 50;        // 圆滑程度（边数，越大越圆）

    [Header("圆环触发参数")]
    public ParticleSystem collectEffect; // 吃掉时的特效
    public float boostMultiplier = 1.1f; // 加速倍率

    private bool isCollected = false;

    void Start()
    {
        // 1. 自动设置碰撞体大小
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius; // 让判定区域刚好等于圆环的内部

        // 2. 使用三角函数，画出一个完美的 3D 圆环！
        LineRenderer line = GetComponent<LineRenderer>();
        line.positionCount = segments + 1;
        line.useWorldSpace = false; // 跟随物体的旋转和缩放
        line.startWidth = thickness;
        line.endWidth = thickness;
        line.loop = true; // 首尾相连

        for (int i = 0; i <= segments; i++)
        {
            // 计算每个点的角度
            float angle = ((float)i / segments) * 2f * Mathf.PI;
            // 计算 X 和 Y 坐标 (Z为0，保证它是个扁平的圈)
            float x = Mathf.Sin(angle) * radius;
            float y = Mathf.Cos(angle) * radius;

            line.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            isCollected = true;

            // 给玩家加速
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity *= boostMultiplier;

            // 播放特效
            if (collectEffect != null)
            {
                collectEffect.transform.SetParent(null);
                collectEffect.Play();
                Destroy(collectEffect.gameObject, 3f);
            }

            // 销毁本体
            Destroy(gameObject);
        }
    }
}