using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [System.Serializable]
    public class MapInfo
    {
        public string mapName;      // 地图显示名称
        public string sceneName;    // 场景名称
    }

    [Header("可用地图")]
    public MapInfo[] maps = new MapInfo[3];

    public int selectedMapIndex = 0;  // 当前选中的地图索引

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

        LoadMapSelection();
    }

    /// <summary>
    /// 选择指定索引的地图
    /// </summary>
    public void SelectMap(int mapIndex)
    {
        if (mapIndex >= 0 && mapIndex < maps.Length)
        {
            selectedMapIndex = mapIndex;
            Debug.Log($"已选择地图: {maps[mapIndex].mapName}");
            SaveMapSelection();
        }
    }

    /// <summary>
    /// 获取选中地图的场景名称
    /// </summary>
    public string GetSelectedSceneName()
    {
        if (selectedMapIndex >= 0 && selectedMapIndex < maps.Length)
            return maps[selectedMapIndex].sceneName;
        return maps[0].sceneName;
    }

    /// <summary>
    /// 保存地图选择
    /// </summary>
    private void SaveMapSelection()
    {
        PlayerPrefs.SetInt("SelectedMapIndex", selectedMapIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 加载地图选择
    /// </summary>
    private void LoadMapSelection()
    {
        if (PlayerPrefs.HasKey("SelectedMapIndex"))
            selectedMapIndex = PlayerPrefs.GetInt("SelectedMapIndex");
        else
            selectedMapIndex = 0;
    }
}