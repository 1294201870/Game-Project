using UnityEngine;
using UnityEngine.UI; // ★ 必须引入这个才能控制UI！

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 绑定")]
    public Text scoreText; // 拖入左上角的 ScoreText
    public Text popupText; // 拖入屏幕中间的 PopupText

    [Header("游戏数据")]
    public int currentScore = 0;
    public float flightTime = 0f;
    public bool isGameActive = true;
    private float gameStartTime = 0f;
    private float popupTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        gameStartTime = Time.time;  // ★ 新增：记录游戏开始时间
    }

    void Update()
    {
        if (isGameActive)
        {
            flightTime += Time.deltaTime;
        }

        // 处理屏幕中间弹字的淡出特效
        if (popupTimer > 0 && popupText != null)
        {
            popupTimer -= Time.deltaTime;
            Color c = popupText.color;
            c.a = popupTimer / 2f; // 2秒内渐渐透明消失
            popupText.color = c;
        }
    }

    // 更新左上角的常驻分数
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + currentScore.ToString();
        }
    }

    public void AddScore(int amount, string reason)
    {
        if (!isGameActive) return;

        currentScore += amount;
        UpdateScoreUI(); // 立刻刷新左上角分数

        // 如果绑定了弹字UI，就在屏幕中间爆出得分提示
        if (popupText != null)
        {
            popupText.text = $"+{amount} {reason}";
            Color c = popupText.color;
            c.a = 1f; // 完全不透明
            popupText.color = c;
            popupTimer = 2f; // 停留并淡出2秒
        }
    }

    public void EndGame(bool success)
    {
        isGameActive = false;
        if (success)
        {
            if (popupText != null)
            {
                popupText.text = $"完美落地！总分: {currentScore}";
                popupText.color = Color.yellow;
                popupTimer = 10f;
            }

            // ★ 新增：保存成绩
            SaveScoreToRanking();
        }
    }

    /// <summary>
    /// 将成绩保存到排行榜
    /// </summary>
    void SaveScoreToRanking()
    {
        float totalTime = Time.time - gameStartTime;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(currentScore, totalTime);
            Debug.Log($"成绩已保存 - 得分: {currentScore}, 用时: {totalTime}s");
        }
        else
        {
            Debug.LogWarning("ScoreManager 实例不存在！");
        }
    }
}