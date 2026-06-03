using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("Logo 摆荡")]
    public RectTransform logoTransform;
    public float logoSwingAngle = 10f;
    public float logoSwingSpeed = 2f;

    [Header("小人man 摆荡")]
    public RectTransform manTransform;
    public float manSwingAngle = 10f;
    public float manSwingSpeed = 2.5f;
    public float manSwingPhase = 1.2f;

    [System.Serializable]
    public class ButtonAnimConfig
    {
        public Button button;
        public Image buttonImage;
        public Color normalColor = new Color(1f, 1f, 1f, 1f);
        public Color hoverColor = new Color(0.8f, 0.9f, 1f, 1f);
        public float hoverScale = 1.08f;
        public float normalScale = 1.0f;
        [HideInInspector] public Vector3 targetScale;
    }

    [Header("所有需要动效的按钮")]
    public List<ButtonAnimConfig> animatedButtons = new List<ButtonAnimConfig>();
    public float buttonAnimSpeed = 10f;

    [Header("游戏主场景名")]
    public string gameSceneName = "GameScene";

    [Header("挂上你的设置面板")]
    public GameObject settingsPanel;

    [Header("Settings面板中的Slider")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("挂上你的地图面板")]
    public GameObject mapSelectionPanel;

    [Header("排行榜面板")]
    public GameObject rankingPanel;

    void Start()
    {
        AudioManager.Instance.PlayBGM(1);

        // ★ 新增：用代码动态绑定 Slider 的 OnValueChanged 事件
        // 这样每次场景重新加载时都会重新绑定，避免事件丢失
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners(); // 清除旧的绑定
            bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
            Debug.Log("BGM Slider 事件已绑定");
        }
        else
        {
            Debug.LogWarning("bgmSlider 未在Inspector中挂载！");
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners(); // 清除旧的绑定
            sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
            Debug.Log("SFX Slider 事件已绑定");
        }
        else
        {
            Debug.LogWarning("sfxSlider 未在Inspector中挂载！");
        }

        

        // ======== 按钮动效初始化 ========
        foreach (var cfg in animatedButtons)
        {
            if (cfg.button == null) continue;
            cfg.targetScale = Vector3.one * cfg.normalScale;
            cfg.button.transform.localScale = cfg.targetScale;

            // 修复：用当前image的颜色为正常色/确保alpha=1
            if (cfg.buttonImage != null)
            {
                var colorNow = cfg.buttonImage.color;
                colorNow.a = 1f;
                cfg.normalColor.a = 1f;
                cfg.hoverColor.a = 1f;
                // 若用户没自定义normalColor，则用现有image色
                if (cfg.normalColor == new Color(1f, 1f, 1f, 1f))
                    cfg.normalColor = colorNow;
                cfg.buttonImage.color = cfg.normalColor;
            }

            // 绑定事件：按按钮名称或tag抓并分配功能
            cfg.button.onClick.RemoveAllListeners();

            string buttonName = cfg.button.name.ToLower();
            if (buttonName.Contains("play"))
                cfg.button.onClick.AddListener(OnPlayButtonClick);
            else if (buttonName.Contains("map"))
                cfg.button.onClick.AddListener(OnMapButtonClick);
            else if (buttonName.Contains("rank"))
                cfg.button.onClick.AddListener(OnRankButtonClick);
            else if (buttonName.Contains("setting"))
                cfg.button.onClick.AddListener(OnSettingButtonClick);
            else if (buttonName.Contains("exit"))
                cfg.button.onClick.AddListener(OnExitButtonClick);

            // 鼠标悬停事件动效
            EventTrigger et = cfg.button.gameObject.GetComponent<EventTrigger>();
            if (et == null)
                et = cfg.button.gameObject.AddComponent<EventTrigger>();
            et.triggers.Clear();

            // 悬停进入
            var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((eventData) =>
            {
                cfg.targetScale = Vector3.one * cfg.hoverScale;
                if (cfg.buttonImage != null)
                    cfg.buttonImage.color = cfg.hoverColor;
            });
            et.triggers.Add(entryEnter);

            // 悬停离开
            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((eventData) =>
            {
                cfg.targetScale = Vector3.one * cfg.normalScale;
                if (cfg.buttonImage != null)
                    cfg.buttonImage.color = cfg.normalColor;
            });
            et.triggers.Add(entryExit);
        }

        if (rankingPanel != null) rankingPanel.SetActive(false);
        if (mapSelectionPanel != null) mapSelectionPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (logoTransform != null)
        {
            float angle = Mathf.Sin(Time.time * logoSwingSpeed) * logoSwingAngle;
            logoTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }
        if (manTransform != null)
        {
            float angle = Mathf.Sin(Time.time * manSwingSpeed + manSwingPhase) * manSwingAngle;
            manTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }
        foreach (var cfg in animatedButtons)
        {
            if (cfg.button != null)
            {
                cfg.button.transform.localScale = Vector3.Lerp(
                    cfg.button.transform.localScale,
                    cfg.targetScale,
                    Time.deltaTime * buttonAnimSpeed);
            }
        }
    }

    // ====== 按钮功能区 ======
    public void OnPlayButtonClick()
    {
        AudioManager.Instance.PlayBGM(2);

        // 获取选中的地图场景名称
        string selectedScene = MapManager.Instance.GetSelectedSceneName();
        Debug.Log($"加载场景: {selectedScene}");

        SceneManager.LoadScene(selectedScene);
    }

    public void OnMapButtonClick()
    {
        // 打开地图选择面板
        if (mapSelectionPanel != null)
        {
            mapSelectionPanel.SetActive(true);
            Debug.Log("打开地图选择面板");
        }
        else
            Debug.LogError("mapSelectionPanel 未在Inspector中挂载！");
    }

    public void OnRankButtonClick()
    {
        // ★ 修改为
        if (rankingPanel != null)
        {
            rankingPanel.SetActive(true);
            Debug.Log("打开排行榜");
        }
        else
            Debug.LogError("rankingPanel 未在Inspector中挂载！");
    }

    public void OnSettingButtonClick()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            // 打开设置面板时，立即同步 Slider 值到当前 AudioManager 的音量
            if (bgmSlider != null && AudioManager.Instance != null)
            {
                bgmSlider.value = AudioManager.Instance.bgmVolume;
                Debug.Log($"BGM Slider 同步到: {AudioManager.Instance.bgmVolume}");
            }
            if (sfxSlider != null && AudioManager.Instance != null)
            {
                sfxSlider.value = AudioManager.Instance.sfxVolume;
                Debug.Log($"SFX Slider 同步到: {AudioManager.Instance.sfxVolume}");
            }
        }
        else
            Debug.LogError("请在Inspector中挂上SettingsPanel对象！");
    }

    public void OnExitButtonClick()
    {
        Debug.Log("退出游戏。");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}