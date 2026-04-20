using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class WingsuitTerrainGenerator : MonoBehaviour
{
    [Header("地形物理尺寸 (翼装飞行需要长条形地图)")]
    public float terrainWidth = 2000f;  // 地形宽度 (X轴)
    public float terrainLength = 6000f; // 地形长度 (Z轴，飞行方向)
    public float terrainHeight = 8000f; // 地形最大高度 (Y轴) - 决定下落的刺激感

    [Header("宏观走势控制")]
    [Range(0f, 1f)] public float plainStartPoint = 0.85f; // 平原开始的比例位置 (0-1)
    [Range(0f, 0.5f)] public float ridgeSpreadMax = 0.25f; // 山脉在终点时分开的宽度
    [Range(0.05f, 0.5f)] public float ridgeThickness = 0.18f; // 山体的厚度 (决定了中间峡谷有多窄/多深)

    [Header("地表崎岖度 (错落感)")]
    public float noiseScale = 15f;     // 噪波缩放（数值越大，细节越密集）
    public float noiseIntensity = 0.2f;// 表面崎岖的剧烈程度
    public int octaves = 5;            // 噪波叠加层数(细节丰富度)
    public float seed = 12345f;        // 随机种子

    [ContextMenu("Generate Terrain (生成峡谷地形)")]
    public void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData terrainData = terrain.terrainData;

        // 设置地形的实际物理尺寸
        terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // u 是横向 (左到右 0~1)， v 是纵向 (起点到终点 0~1)
                float u = (float)x / (resolution - 1);
                float v = (float)z / (resolution - 1);

                // 1. 【高斯山脉叠加生成真实的峡谷】
                // 随着 v 增加，山脊向两侧分开
                float spread = Mathf.Lerp(0.0f, ridgeSpreadMax, v);
                float leftSpine = 0.5f - spread;
                float rightSpine = 0.5f + spread;

                // 使用高斯函数 (e^(-x^2)) 模拟山体的钟形隆起
                // 这样当两条山脊靠近时，它们边缘的厚度会叠加，在中间托起一个天然的峡谷底部
                float leftMountain = Mathf.Exp(-Mathf.Pow((u - leftSpine) / ridgeThickness, 2f));
                float rightMountain = Mathf.Exp(-Mathf.Pow((u - rightSpine) / ridgeThickness, 2f));

                // 将左右两座山叠加（起点时完全重合，形成最高单峰；后来逐渐分开，中间因叠加减少而凹陷成谷）
                float mountainProfile = Mathf.Clamp01(leftMountain + rightMountain);

                // 2. 【坡度与平原过渡】
                // 起点高，终点低，并在平原点归零
                float descentSlope = Mathf.Lerp(1.0f, 0.1f, v);
                float plainFade = Mathf.SmoothStep(1.0f, 0.0f, Mathf.InverseLerp(plainStartPoint - 0.1f, plainStartPoint + 0.05f, v));

                // 3. 【分形噪波生成错落感】
                float noise = 0f;
                float frequency = noiseScale;
                float amplitude = 1f;
                float maxValue = 0f;
                for (int i = 0; i < octaves; i++)
                {
                    // 乘以长宽比例以保持噪波在长条形地图上不变形
                    float nx = u * frequency * (terrainWidth / 1000f) + seed;
                    float nz = v * frequency * (terrainLength / 1000f) + seed;
                    noise += Mathf.PerlinNoise(nx, nz) * amplitude;

                    maxValue += amplitude;
                    amplitude *= 0.5f;
                    frequency *= 2f;
                }
                // 将噪波映射到 -1 到 1，并减弱对谷底的影响（让谷底相对平滑些）
                noise = (noise / maxValue) * 2f - 1f;
                float noiseMask = mountainProfile; // 山越高的地方越崎岖

                // 4. 【最终高度计算】
                float finalHeight = (mountainProfile * descentSlope) + (noise * noiseIntensity * noiseMask);

                // 应用平原衰减
                finalHeight *= plainFade;

                // 给平原加一点极微���的起伏
                float plainNoise = Mathf.PerlinNoise(u * 50f, v * 50f * (terrainLength / terrainWidth)) * 0.005f * (1f - plainFade);

                heights[z, x] = Mathf.Clamp01(finalHeight + plainNoise);
            }
        }

        // 应用高度数据
        terrainData.SetHeights(0, 0, heights);
        Debug.Log($"生成完毕！当前地形尺寸: 宽 {terrainWidth}m, 长 {terrainLength}m, 高 {terrainHeight}m");
    }
}