using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralSpawnManager : MonoBehaviour
{
    // 中文→英文映射表（不动）
    private Dictionary<string, string> generalNameToEnglish = new Dictionary<string, string>()
    {
        // 君主
        {"曹操", "CaoCao"}, {"曹丕", "CaoPi"},
        {"刘备", "LiuBei"}, {"刘禅", "LiuShan"},
        {"孙权", "SunQuan"}, {"孙策", "SunCe"},
        // 武将（魏）
        {"典韦", "DianWei"}, {"许褚", "XuChu"}, {"张辽", "ZhangLiao"}, {"夏侯惇", "XiaHouDun"}, {"张郃", "ZhangHe"},
        // 武将（蜀）
        {"关羽", "GuanYu"}, {"张飞", "ZhangFei"}, {"赵云", "ZhaoYun"}, {"马超", "MaChao"}, {"黄忠", "HuangZhong"},
        // 武将（吴）
        {"周瑜", "ZhouYu"}, {"吕蒙", "LvMeng"}, {"甘宁", "GanNing"}, {"太史慈", "TaiShiCi"}, {"周泰", "ZhouTai"},
        // 文臣（魏）
        {"司马懿", "SiMaYi"}, {"郭嘉", "GuoJia"}, {"荀彧", "XunYu"}, {"贾诩", "JiaXu"}, {"程昱", "ChengYu"},
        // 文臣（蜀）
        {"诸葛亮", "ZhuGeLiang"}, {"庞统", "PangTong"}, {"法正", "FaZheng"}, {"简雍", "JianYong"}, {"糜竺", "MiZhu"},
        // 文臣（吴）
        {"陆逊", "LuXun"}, {"鲁肃", "LuSu"}, {"张昭", "ZhangZhao"}, {"张纮", "ZhangHong"}, {"顾雍", "GuYong"},
        // 士兵
        {"魏兵1", "WeiBing"}, {"魏兵2", "WeiBing"}, {"魏兵3", "WeiBing"}, {"魏兵4", "WeiBing"}, {"魏兵5", "WeiBing"},
        {"蜀兵1", "ShuBing"}, {"蜀兵2", "ShuBing"}, {"蜀兵3", "ShuBing"}, {"蜀兵4", "ShuBing"}, {"蜀兵5", "ShuBing"},
        {"吴兵1", "WuBing"}, {"吴兵2", "WuBing"}, {"吴兵3", "WuBing"}, {"吴兵4", "WuBing"}, {"吴兵5", "WuBing"}
    };

    // 配置字段（不动）
    [Header("通用武将预制体")]
    public GameObject generalItemPrefab;
    [Header("武将项父容器")]
    public Transform heroPanel;
    [Header("武将项间距")]
    public float spacing = 20f;

    // 新增：单例（确保只有1个实例）
    public static GeneralSpawnManager Instance;
    // 新增：标记位（防止SpawnAllGenerals重复执行）
    private bool hasSpawned = false;

    private List<GameObject> spawnedGenerals = new List<GameObject>();

    // 新增：初始化单例
    private void Awake()
    {
        // 确保只有1个GeneralSpawnManager实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：跨场景不销毁
        }
        else
        {
            Destroy(gameObject); // 销毁重复的实例
        }
    }

    private void Start()
    {
        // 只有没生成过的情况下，才执行生成
        if (!hasSpawned)
        {
            SpawnAllGenerals();
            hasSpawned = true; // 标记为已生成
        }
    }

    public void SpawnAllGenerals()
    {
        // 第一步：强制清空父容器（不管列表里有没有，先清场景）
        ClearHeroPanelCompletely();
        // 第二步：清空列表（兜底）
        spawnedGenerals.Clear();

        List<GeneralData> allGenerals = GeneralDataManager.Instance.allGeneralData;
        if (allGenerals == null || allGenerals.Count == 0)
        {
            Debug.LogError("❌ 没有武将数据！");
            return;
        }

        // 关键修改：随机挑选8个不重复的武将数据
        int targetCount = 8;
        // 容错：如果武将总数不足8个，就取全部
        int actualCount = Mathf.Min(targetCount, allGenerals.Count);

        if (actualCount < targetCount)
        {
            Debug.LogWarning($"⚠️ 武将数据不足8个，仅能生成{actualCount}个武将项");
        }

        // 生成随机索引（不重复）
        List<int> randomIndices = new List<int>();
        while (randomIndices.Count < actualCount)
        {
            int randomIndex = Random.Range(0, allGenerals.Count);
            // 确保索引不重复
            if (!randomIndices.Contains(randomIndex))
            {
                randomIndices.Add(randomIndex);
            }
        }

        // 遍历随机选中的索引，生成武将项
        for (int i = 0; i < randomIndices.Count; i++)
        {
            int index = randomIndices[i];
            GeneralData data = allGenerals[index];
            SpawnSingleGeneral(data, i); // 这里的i是生成顺序，不影响随机结果
        }

        Debug.Log($"✅ 成功生成{randomIndices.Count}个随机武将项（无重复）");
    }

    public void SpawnSingleGeneral(GeneralData data, int i)
    {
        // 1. 生成武将项
        GameObject generalItem = Instantiate(generalItemPrefab, heroPanel);

        // 2. 排版
        RectTransform rt = generalItem.GetComponent<RectTransform>();
        float itemWidth = rt.sizeDelta.x;
        float posX = i * (itemWidth + spacing);
        rt.anchoredPosition = new Vector2(posX, 0);
        rt.sizeDelta = new Vector2(200, 300);

        // 3. 配置GeneralItem组件
        GeneralItem generalComp = generalItem.GetComponent<GeneralItem>();
        if (generalComp != null)
        {
            generalComp.generalName = data.generalName;

            // ========== 核心修改：遍历所有子对象找Button ==========
            // 替换原来的 generalComp.selectButton = generalItem.GetComponent<Button>();
            generalComp.selectButton = generalItem.GetComponentInChildren<Button>();

            // 加日志：验证是否找到Button
            if (generalComp.selectButton == null)
            {
                Debug.LogError($"❌ 生成[{data.generalName}]时，未在预制体/子对象中找到Button组件！", generalItem);
            }
            else
            {
                Debug.Log($"✅ 生成[{data.generalName}]时，成功绑定Button组件", generalItem);
            }

            generalComp.modelRoot = generalItem.transform.Find("ModelRoot");
            generalComp.Init(); // 初始化

            // 加载模型（可选）
            GameObject modelPrefab = LoadGeneralModelByPath(data.camp, data.generalName);
            if (modelPrefab != null)
            {
                generalComp.SetGeneral(modelPrefab);
            }
        }

        spawnedGenerals.Add(generalItem);
    }

    // 新增：彻底清空父容器（不管有没有存到列表里）
    private void ClearHeroPanelCompletely()
    {
        if (heroPanel == null) return;

        // 遍历父容器下所有子对象，强制销毁
        for (int i = heroPanel.childCount - 1; i >= 0; i--)
        {
            Transform child = heroPanel.GetChild(i);
            DestroyImmediate(child.gameObject); // 立即销毁（编辑模式/运行模式都生效）
        }
    }

    // 优化：清空列表+场景对象（兜底）
    public void ClearAllGenerals()
    {
        // 1. 销毁列表里的对象
        foreach (var item in spawnedGenerals)
        {
            if (item != null) Destroy(item);
        }
        // 2. 清空列表
        spawnedGenerals.Clear();
        // 3. 额外清空父容器（双重保障）
        ClearHeroPanelCompletely();

        // 重置标记位（如需重新生成）
        hasSpawned = false;
    }

    private GameObject LoadGeneralModelByPath(string camp, string generalName)
    {
        if (!generalNameToEnglish.ContainsKey(generalName))
        {
            Debug.LogError($"❌ 映射表未配置{generalName}的英文名称！");
            return null;
        }
        string englishName = generalNameToEnglish[generalName];
        string modelPath = $"Custom/Heros/{camp}/{englishName}/{englishName}";
        return Resources.Load<GameObject>(modelPath);
    }
}