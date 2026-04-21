using UnityEngine;

public class CheckpointRing : MonoBehaviour
{
    [Header("圆环参数")]
    public int scoreReward = 500;
    public float rotationSpeed = 30f;

    private bool isCollected = false;

    void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected || !other.CompareTag("Player")) return;

        isCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreReward, "完美穿环！");
        }

        // 1. 隐藏所有视觉元素
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        // 2. ★ 核心修复：立刻禁用自身和所有子物体的碰撞体，防���物理引擎延迟判定导致玩家撞死
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 2f);
    }
}