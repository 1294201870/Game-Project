using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;

    void OnEnable()
    {
        if (AudioManager.Instance == null) return;

        // 打开时同步 Slider 到当前音量
        bgmSlider.value = AudioManager.Instance.bgmVolume;
        sfxSlider.value = AudioManager.Instance.sfxVolume;

        Debug.Log($"Settings打开 - BGM: {AudioManager.Instance.bgmVolume}, SFX: {AudioManager.Instance.sfxVolume}");
    }

    void OnDisable()
    {
        // 关闭时保存音量（可选）
        if (AudioManager.Instance != null)
        {
            Debug.Log($"Settings关闭 - BGM已设为: {AudioManager.Instance.bgmVolume}, SFX已设为: {AudioManager.Instance.sfxVolume}");
        }
    }
}