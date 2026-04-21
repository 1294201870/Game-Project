using UnityEngine;

public class LandingZone : MonoBehaviour
{
    [Header("降落计分参数 (单位：米)")]
    public float bullseyeRadius = 3f;    // 靶心半径：10000分
    public float greatRadius = 8f;       // 优秀半径：5000分
    public float goodRadius = 15f;       // 及格半径：1000分

    private bool hasScored = false;

    // 当玩家双脚（或脸）接触到靶盘时触发
    void OnCollisionEnter(Collision collision)
    {
        // 确保只触发一次，且碰到的是玩家
        if (hasScored || !collision.gameObject.CompareTag("Player")) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null) return;

        hasScored = true;

        // 1. 检查玩家死活
        if (player.currentState == PlayerController.PlayerState.Crashed)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(0, "砸中靶心...但变成了果汁！");
            }
            return;
        }

        // 2. 玩家安全落地，计算接触点到靶心的“水平距离”
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 center = transform.position;

        // 忽略高度差，只算平面距离
        hitPoint.y = 0;
        center.y = 0;
        float distance = Vector3.Distance(hitPoint, center);

        // 3. 评级与加分
        int landingScore = 0;
        string landingMsg = "";

        if (distance <= bullseyeRadius)
        {
            landingScore = 10000;
            landingMsg = "正中靶心！完美降落！";
        }
        else if (distance <= greatRadius)
        {
            landingScore = 5000;
            landingMsg = "优秀降落！";
        }
        else if (distance <= goodRadius)
        {
            landingScore = 1000;
            landingMsg = "安全着陆！";
        }
        else
        {
            landingScore = 100;
            landingMsg = "偏离靶心";
        }

        // 4. 提交分数给大管家
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(landingScore, landingMsg);

            // 触发游戏胜利逻辑
            GameManager.Instance.EndGame(true);
        }
    }
}