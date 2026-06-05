using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI 绑定")]
    public Text scoreText;
    public Text popupText;

    [Header("游戏数据")]
    public int currentScore = 0;
    public float flightTime = 0f;
    public bool isGameActive = true;

    private float popupTimer = 0f;
    private float gameStartTime = 0f;
    private bool hasScoreSaved = false;  // ★ 新增：防止重复保存

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        gameStartTime = Time.time;
        hasScoreSaved = false;  // ★ 新增：初始化为 false
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
            c.a = popupTimer / 2f;
            popupText.color = c;
        }
    }

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
        UpdateScoreUI();

        if (popupText != null)
        {
            popupText.text = $"+{amount} {reason}";
            Color c = popupText.color;
            c.a = 1f;
            popupText.color = c;
            popupTimer = 2f;
        }
    }

    public void EndGame(bool success)
    {
        // ★ 新增：如果已经保存过分数，就不再保存
        if (hasScoreSaved)
        {
            Debug.LogWarning("分数已保存过，忽略重复保存");
            return;
        }

        isGameActive = false;

        if (success)
        {
            if (popupText != null)
            {
                popupText.text = $"A perfect landing! Total score: {currentScore}";
                popupText.color = Color.yellow;
                popupTimer = 10f;
            }

            // ★ 修改：保存分数前标记为已保存
            SaveScoreToRanking();
        }
    }

    void SaveScoreToRanking()
    {
        // ★ 新增：标记为已保存
        hasScoreSaved = true;

        float totalTime = Time.time - gameStartTime;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(currentScore, totalTime);
            Debug.Log($" 成绩已保存 - 得分: {currentScore}, 用时: {totalTime}s");
        }
        else
        {
            Debug.LogWarning(" ScoreManager 实例不存在！");
        }
    }

    // ★ 新增：重置方法（返回主菜单时调用）
    public void ResetGameState()
    {
        hasScoreSaved = false;
        isGameActive = true;
        currentScore = 0;
        flightTime = 0f;
        gameStartTime = Time.time;
    }
}