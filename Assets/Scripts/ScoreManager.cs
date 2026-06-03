using UnityEngine;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [System.Serializable]
    public class ScoreRecord
    {
        public float score;
        public float time;
        public string date;
    }

    [SerializeField] private List<ScoreRecord> scoreRecords = new List<ScoreRecord>();

    private const int MaxRecords = 10;
    private const string ScoreKey = "ScoreRecord_";
    private const string CountKey = "ScoreCount";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadAllScores();
    }

    public void AddScore(float score, float time)
    {
        ScoreRecord record = new ScoreRecord
        {
            score = score,
            time = time,
            date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        scoreRecords.Add(record);
        scoreRecords.Sort((a, b) => b.score.CompareTo(a.score));

        if (scoreRecords.Count > MaxRecords)
            scoreRecords.RemoveAt(scoreRecords.Count - 1);

        SaveAllScores();
        Debug.Log($"记录已保存 - 得分: {score}, 用时: {time}s");
    }

    public List<ScoreRecord> GetAllScores()
    {
        return new List<ScoreRecord>(scoreRecords);
    }

    public void ClearAllScores()
    {
        scoreRecords.Clear();
        PlayerPrefs.DeleteKey(CountKey);
        for (int i = 0; i < MaxRecords; i++)
        {
            PlayerPrefs.DeleteKey($"{ScoreKey}{i}_score");
            PlayerPrefs.DeleteKey($"{ScoreKey}{i}_time");
            PlayerPrefs.DeleteKey($"{ScoreKey}{i}_date");
        }
        PlayerPrefs.Save();
        Debug.Log("所有记录已清空");
    }

    private void SaveAllScores()
    {
        PlayerPrefs.SetInt(CountKey, scoreRecords.Count);

        for (int i = 0; i < scoreRecords.Count; i++)
        {
            PlayerPrefs.SetFloat($"{ScoreKey}{i}_score", scoreRecords[i].score);
            PlayerPrefs.SetFloat($"{ScoreKey}{i}_time", scoreRecords[i].time);
            PlayerPrefs.SetString($"{ScoreKey}{i}_date", scoreRecords[i].date);
        }

        PlayerPrefs.Save();
    }

    private void LoadAllScores()
    {
        scoreRecords.Clear();

        int count = PlayerPrefs.GetInt(CountKey, 0);

        for (int i = 0; i < count; i++)
        {
            if (PlayerPrefs.HasKey($"{ScoreKey}{i}_score"))
            {
                ScoreRecord record = new ScoreRecord
                {
                    score = PlayerPrefs.GetFloat($"{ScoreKey}{i}_score"),
                    time = PlayerPrefs.GetFloat($"{ScoreKey}{i}_time"),
                    date = PlayerPrefs.GetString($"{ScoreKey}{i}_date", "Unknown")
                };
                scoreRecords.Add(record);
            }
        }

        Debug.Log($"已加载 {scoreRecords.Count} 条记录");
    }
}