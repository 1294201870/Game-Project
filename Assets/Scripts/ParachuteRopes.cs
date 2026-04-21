using UnityEngine;

public class ParachuteRopes : MonoBehaviour
{
    [Header("伞面四个角落的锚点")]
    public Transform anchorFL; // 前左
    public Transform anchorFR; // 前右
    public Transform anchorBL; // 后左
    public Transform anchorBR; // 后右

    [Header("玩家身上的连接点 (把 Backpack 拖进来)")]
    public Transform backpackAttachment;

    [Header("绳索外观")]
    public float ropeWidth = 0.02f;
    public Color ropeColor = Color.black;

    private LineRenderer[] ropes;

    void Start()
    {
        // 游戏开始时，代码自动为你生成4条动态绳索！
        ropes = new LineRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject ropeObj = new GameObject("DynamicRope_" + i);
            ropeObj.transform.SetParent(transform);

            LineRenderer lr = ropeObj.AddComponent<LineRenderer>();
            lr.positionCount = 2; // 一根绳子两个点(起点和终点)
            lr.startWidth = ropeWidth;
            lr.endWidth = ropeWidth;

            // 使用 Unity 自带的基础材质来渲染纯色线条
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = ropeColor;
            lr.endColor = ropeColor;

            ropes[i] = lr;
        }
    }

    // ★ 使用 LateUpdate 确保在玩家身体移动完毕后，绳子再跟上，防止绳索延迟脱节
    void LateUpdate()
    {
        if (backpackAttachment == null) return;

        // 时刻更新绳子的 起点(伞面) 和 终点(背包)
        if (anchorFL != null) DrawRope(ropes[0], anchorFL.position, backpackAttachment.position);
        if (anchorFR != null) DrawRope(ropes[1], anchorFR.position, backpackAttachment.position);
        if (anchorBL != null) DrawRope(ropes[2], anchorBL.position, backpackAttachment.position);
        if (anchorBR != null) DrawRope(ropes[3], anchorBR.position, backpackAttachment.position);
    }

    void DrawRope(LineRenderer lr, Vector3 start, Vector3 end)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}