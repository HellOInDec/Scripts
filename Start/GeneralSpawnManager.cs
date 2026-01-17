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

    // 配置字段（新增SwapButton绑定）
    [Header("通用武将预制体")]
    public GameObject generalItemPrefab;
    [Header("武将项父容器")]
    public Transform heroPanel;
    [Header("武将项间距")]
    public float spacing = 20f;
    [Header("交换按钮")] // 新增：绑定你的SwapButton
    public Button swapButton;

    // 新增：单例（确保只有1个实例）
    public static GeneralSpawnManager Instance;
    // 新增：标记位（防止SpawnAllGenerals重复执行）
    private bool hasSpawned = false;

    private List<GameObject> spawnedGenerals = new List<GameObject>();
    // 新增：维护两个列表，区分「已随机显示」和「未随机」的武将数据
    private List<GeneralData> selectedGeneralDatas = new List<GeneralData>(); // 当前显示的8个
    private List<GeneralData> unusedGeneralDatas = new List<GeneralData>();   // 未被随机到的武将池

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
        // 绑定交换按钮点击事件
        if (swapButton != null)
        {
            swapButton.onClick.AddListener(OnSwapButtonClick);
        }
        else
        {
            Debug.LogError("❌ 请在Inspector中绑定SwapButton！");
        }

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
        selectedGeneralDatas.Clear();
        unusedGeneralDatas.Clear();

        List<GeneralData> allGenerals = GeneralDataManager.Instance.allGeneralData;
        if (allGenerals == null || allGenerals.Count == 0)
        {
            Debug.LogError("❌ 没有武将数据！");
            return;
        }

        // 关键修改：随机挑选8个不重复的武将数据，并划分已选/未选池
        int targetCount = 8;
        // 容错：如果武将总数不足8个，就取全部
        int actualCount = Mathf.Min(targetCount, allGenerals.Count);

        if (actualCount < targetCount)
        {
            Debug.LogWarning($"⚠️ 武将数据不足8个，仅能生成{actualCount}个武将项");
        }

        // 先复制所有武将数据到临时列表
        List<GeneralData> tempAllGenerals = new List<GeneralData>(allGenerals);
        // 生成随机索引（不重复），填充已选列表
        List<int> randomIndices = new List<int>();
        while (randomIndices.Count < actualCount)
        {
            int randomIndex = Random.Range(0, tempAllGenerals.Count);
            // 确保索引不重复
            if (!randomIndices.Contains(randomIndex))
            {
                randomIndices.Add(randomIndex);
                selectedGeneralDatas.Add(tempAllGenerals[randomIndex]);
                tempAllGenerals.RemoveAt(randomIndex);
            }
        }
        // 剩下的就是未被随机到的武将池
        unusedGeneralDatas = tempAllGenerals;

        // 遍历随机选中的索引，生成武将项
        for (int i = 0; i < selectedGeneralDatas.Count; i++)
        {
            GeneralData data = selectedGeneralDatas[i];
            SpawnSingleGeneral(data, i); // 这里的i是生成顺序，不影响随机结果
        }

        Debug.Log($"✅ 成功生成{selectedGeneralDatas.Count}个随机武将项（无重复），剩余未随机武将：{unusedGeneralDatas.Count}个");
    }

    public void SpawnSingleGeneral(GeneralData data, int i)
    {
        // 1. 生成武将项
        GameObject generalItem = Instantiate(generalItemPrefab, heroPanel);

        // 2. 排版
        RectTransform rt = generalItem.GetComponent<RectTransform>();
        float itemWidth = 200f;
        float posX = i * (itemWidth + spacing);
        rt.anchoredPosition = new Vector2(posX, 0);
        rt.sizeDelta = new Vector2(200, 300);

        // 3. 配置GeneralItem组件
        GeneralItem generalComp = generalItem.GetComponent<GeneralItem>();
        if (generalComp != null)
        {
            generalComp.generalName = data.generalName;
            generalComp.modelRoot = generalItem.transform.Find("ModelRoot");
            generalComp.isSelected = false;
            generalComp.Init(); // 只初始化状态，不绑定事件

            // 加载模型
            GameObject modelPrefab = LoadGeneralModelByPath(data.camp, data.generalName);
            if (modelPrefab != null)
            {
                generalComp.SetGeneral(modelPrefab);
            }

            // 关键：删除所有绑定Button事件的代码！
            // （事件绑定交给GeneralItem的Awake处理）
        }

        // 替换列表中的旧卡片
        if (i < spawnedGenerals.Count)
        {
            spawnedGenerals[i] = generalItem;
        }
        else
        {
            spawnedGenerals.Add(generalItem);
        }
    }

    // 新增：SwapButton点击事件核心逻辑（支持多选）
    // SwapButton点击事件（支持多选，新增同步ScoreCalculator逻辑）
    private void OnSwapButtonClick()
    {
        // 1. 收集所有选中的武将项
        List<(GeneralItem item, int index)> selectedItems = new List<(GeneralItem, int)>();
        for (int i = 0; i < spawnedGenerals.Count; i++)
        {
            GeneralItem item = spawnedGenerals[i].GetComponent<GeneralItem>();
            if (item != null && item.isSelected)
            {
                selectedItems.Add((item, i));
            }
        }

        // 2. 未选中任何武将时提示
        if (selectedItems.Count == 0)
        {
            Debug.LogWarning("⚠️ 请先选中至少一个英雄再点击交换！");
            return;
        }

        // 3. 检查未随机池是否有足够的武将进行交换
        if (unusedGeneralDatas.Count < selectedItems.Count)
        {
            Debug.LogWarning($"⚠️ 未随机武将不足（仅剩余{unusedGeneralDatas.Count}个），无法交换{selectedItems.Count}个选中的英雄！");
            return;
        }

        // ========== 核心新增：交换前先清空选中记录 ==========
        if (ScoreCalculator.Instance != null)
        {
            // 清空所有选中的旧武将记录
            foreach (var (item, _) in selectedItems)
            {
                ScoreCalculator.Instance.ClearGeneralSelectRecord(item.generalName);
            }
        }

        // 4. 批量交换逻辑
        foreach (var (selectedItem, selectedIndex) in selectedItems)
        {
            int randomUnusedIndex = -1;
            GeneralData newGeneralData = null;
            // 循环找有数据的武将（避免选到无数据的空项）
            for (int j = 0; j < unusedGeneralDatas.Count; j++)
            {
                GeneralData tempData = unusedGeneralDatas[j];
                if (tempData != null && !string.IsNullOrEmpty(tempData.generalName)
                    && GeneralDataManager.Instance.GetGeneralData(tempData.generalName) != null)
                {
                    randomUnusedIndex = j;
                    newGeneralData = tempData;
                    break;
                }
            }
            // 容错：没找到有数据的武将
            if (newGeneralData == null)
            {
                Debug.LogWarning($"⚠️ 未随机池里没有可用的武将数据，跳过本次交换");
                continue;
            }

            // 找到被替换的旧武将数据
            GeneralData oldGeneralData = selectedGeneralDatas[selectedIndex];

            // 交换数据池（旧的进未随机池，新的进已显示池）
            selectedGeneralDatas[selectedIndex] = newGeneralData;
            unusedGeneralDatas.RemoveAt(randomUnusedIndex);
            unusedGeneralDatas.Add(oldGeneralData);

            // ========== 核心新增：交换后强制取消选中状态 ==========
            selectedItem.isSelected = false;
            selectedItem.UpdateCardScale();

            // 更新选中武将项的显示（替换名称、模型、初始化）
            selectedItem.generalName = newGeneralData.generalName;
            selectedItem.Init(); // 重新初始化阵营颜色、文字等
                                 // 加载新武将模型
            GameObject newModelPrefab = LoadGeneralModelByPath(newGeneralData.camp, newGeneralData.generalName);
            if (newModelPrefab != null)
            {
                selectedItem.SetGeneral(newModelPrefab);
            }

            Debug.Log($"✅ 交换成功：[{oldGeneralData.generalName}] → [{newGeneralData.generalName}]");
        }

        Debug.Log($"✅ 批量交换完成，共交换{selectedItems.Count}个英雄");
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
        selectedGeneralDatas.Clear();
        unusedGeneralDatas.Clear();
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