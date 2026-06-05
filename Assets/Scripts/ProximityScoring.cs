using UnityEngine;

public class ProximityScoring : MonoBehaviour
{
    [Header("贴地飞行参数")]
    public float maxProximityDistance = 6f; // 距离地面/山体 6 米以内开始算分
    public int basePointsPerSecond = 500;   // 基础每秒加分

    private PlayerController player;
    private float scoreAccumulator = 0f;

    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameActive) return;

        // 只有在“飞行状态(翼装)”时才计算贴地，跳伞和走路不算
        if (player.currentState != PlayerController.PlayerState.Flying) return;

        // 向下打出一根射线检测山体
        // 注意：过滤掉了 Trigger，防止把光圈当成地板
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, maxProximityDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            // 防止射线打到玩家自己的身体上
            if (!hit.transform.IsChildOf(transform))
            {
                // 距离越近，乘数越大！（比如贴地1米，得分是贴地5米的5倍！）
                float closenessFactor = 1f - (hit.distance / maxProximityDistance);

                // 结合飞行速度（飞得越快，贴地加分越恐怖！）
                float speedFactor = player.GetSpeed() / 20f;

                float pointsThisFrame = closenessFactor * basePointsPerSecond * speedFactor * Time.deltaTime;
                scoreAccumulator += pointsThisFrame;

                // 每攒够 50 分才提交一次给大管家（防止 UI 屏幕中间的字抽风狂闪）
                if (scoreAccumulator >= 50f)
                {
                    int addAmount = Mathf.FloorToInt(scoreAccumulator);
                    GameManager.Instance.AddScore(addAmount, "Skimming the ground!");
                    scoreAccumulator -= addAmount; // 扣除已加的分数
                }
            }
        }
    }
}