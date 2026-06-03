using UnityEngine;
using UnityEngine.UI;

public class MapSelectionPanel : MonoBehaviour
{
    [Header("地图按钮")]
    public Button map1Button;
    public Button map2Button;
    public Button map3Button;

    [Header("确认和关闭按钮")]
    public Button confirmButton;
    public Button closeButton;

    private int currentSelectedMapIndex = 0;

    void Start()
    {
        // 绑定地图按钮事件
        if (map1Button != null)
            map1Button.onClick.AddListener(() => OnMapSelected(0));
        if (map2Button != null)
            map2Button.onClick.AddListener(() => OnMapSelected(1));
        if (map3Button != null) { 
            //map3Button.onClick.AddListener(() => OnMapSelected(2));
            map2Button.onClick.AddListener(() => OnMapSelected(1));
        }




        // 绑定确认和关闭按钮
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);

        // 默认选中当前已保存的地图
        currentSelectedMapIndex = MapManager.Instance.selectedMapIndex;
        UpdateButtonHighlight();
    }

    /// <summary>
    /// 地图按钮被点击
    /// </summary>
    void OnMapSelected(int mapIndex)
    {
        currentSelectedMapIndex = mapIndex;
        Debug.Log($"选择了地图: {MapManager.Instance.maps[mapIndex].mapName}");
        UpdateButtonHighlight();
    }

    /// <summary>
    /// 更新按钮高亮效果
    /// </summary>
    void UpdateButtonHighlight()
    {
        // 重置所有按钮为正常颜色
        if (map1Button != null)
            SetButtonColor(map1Button, currentSelectedMapIndex == 0);
        if (map2Button != null)
            SetButtonColor(map2Button, currentSelectedMapIndex == 1);
        if (map3Button != null) {
            //SetButtonColor(map3Button, currentSelectedMapIndex == 2);
            SetButtonColor(map2Button, currentSelectedMapIndex == 1);
        }
            
    }

    /// <summary>
    /// 设置按钮颜色（选中为蓝色，未选中为白色）
    /// </summary>
    void SetButtonColor(Button btn, bool isSelected)
    {
        Image img = btn.GetComponent<Image>();
        if (isSelected)
            img.color = new Color(0.7f, 0.9f, 1f);  // 浅蓝色
        else
            img.color = new Color(1f, 1f, 1f);      // 白色
    }

    /// <summary>
    /// 确认按钮
    /// </summary>
    void OnConfirm()
    {
        // 保存选择到 MapManager
        MapManager.Instance.SelectMap(currentSelectedMapIndex);

        // 关闭面板
        gameObject.SetActive(false);

        Debug.Log($"地图选择已保存: {MapManager.Instance.maps[currentSelectedMapIndex].mapName}");
    }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    void OnClose()
    {
        gameObject.SetActive(false);
    }
}