using UnityEngine;
using UnityEngine.UI;
using TMPro;  // ★ 新增：引入 TMP
using System.Collections.Generic;

public class RankingPanel : MonoBehaviour
{
    [Header("UI 元素")]
    public Transform recordContainer;
    public TextMeshProUGUI noRecordText;  // ★ 改成 TextMeshProUGUI
    public Button clearButton;
    public Button closeButton;

    [Header("记录项预制体")]
    public GameObject recordItemPrefab;

    void Start()
    {
        // ★ 自动查找 recordContainer
        if (recordContainer == null)
        {
            recordContainer = transform.Find("ScrollView/Viewport/Content");
            if (recordContainer != null)
                Debug.Log(" 自动查找到 recordContainer");
            else
                Debug.LogError(" 无法自动查找 recordContainer，请检查 UI 结构");
        }

        // ★ 自动查找 noRecordText
        if (noRecordText == null)
        {
            noRecordText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (noRecordText != null)
                Debug.Log("✓ 自动查找到 noRecordText");
        }

        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearButtonClick);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    void OnEnable()
    {
        RefreshRankingList();
    }

    public void RefreshRankingList()
    {
        Debug.Log("=== 开始刷新排行榜 ===");

        if (recordContainer == null)
        {
            recordContainer = transform.Find("ScrollView/Viewport/Content");
            if (recordContainer == null)
            {
                Debug.LogError(" recordContainer 为 null，无法继续！");
                return;
            }
        }

        // 清空容器
        foreach (Transform child in recordContainer)
        {
            Destroy(child.gameObject);
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogError(" ScoreManager 实例不存在！");
            if (noRecordText != null)
                noRecordText.gameObject.SetActive(true);
            return;
        }

        List<ScoreManager.ScoreRecord> records = ScoreManager.Instance.GetAllScores();
        Debug.Log($" 获取到 {records.Count} 条记录");

        if (records.Count == 0)
        {
            Debug.Log("暂无记录，显示提示文本");
            if (noRecordText != null)
                noRecordText.gameObject.SetActive(true);
            return;
        }
        else
        {
            if (noRecordText != null)
                noRecordText.gameObject.SetActive(false);
        }

        // 创建记录项
        for (int i = 0; i < records.Count; i++)
        {
            if (recordItemPrefab == null)
            {
                Debug.LogError(" recordItemPrefab 未配置！");
                return;
            }

            GameObject item = Instantiate(recordItemPrefab, recordContainer);
            item.name = $"RecordItem_{i}";

            // ★ 改成查找 TextMeshProUGUI
            Transform rankTextTrans = item.transform.Find("RankText");
            Transform scoreTextTrans = item.transform.Find("ScoreText");
            Transform timeTextTrans = item.transform.Find("TimeText");

            if (rankTextTrans != null && scoreTextTrans != null && timeTextTrans != null)
            {
                TextMeshProUGUI rankText = rankTextTrans.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI scoreText = scoreTextTrans.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI timeText = timeTextTrans.GetComponent<TextMeshProUGUI>();

                rankText.text = $"#{i + 1}";
                scoreText.text = $"Score: {records[i].score}";
                timeText.text = $"Time: {records[i].time:F1}s | {records[i].date}";

                Debug.Log($" 记录 {i}: 分数={records[i].score}, 用时={records[i].time:F1}s");
            }
            else
            {
                Debug.LogError($" 记录 {i} 的 Text 组件查找失败！rankTextTrans={rankTextTrans}, scoreTextTrans={scoreTextTrans}, timeTextTrans={timeTextTrans}");
            }
        }

        Debug.Log($"=== 排行榜刷新完成，共显示 {records.Count} 条记录 ===");
    }

    void OnClearButtonClick()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ClearAllScores();
            RefreshRankingList();
            Debug.Log("记录已清空");
        }
    }

    void OnCloseButtonClick()
    {
        gameObject.SetActive(false);
    }
}