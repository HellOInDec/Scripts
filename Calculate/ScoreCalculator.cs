using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;

// 仅保留ScoreCalculator核心逻辑，所有规则类复用ScoreRuleData.cs中的定义
public class ScoreCalculator : MonoBehaviour
{
    // 事件定义（保持不变）
    public delegate void OnGeneralDeselectedDelegate(string generalName);

    // 保留原有事件（用于自动反选）
    public event System.Action<string> OnGeneralDeselected;

    public static ScoreCalculator Instance;

    [Header("UI文本绑定（对应你的ScorePanel）")]
    public TextMeshProUGUI baseScoreText;       // 绑定BaseScore文本
    public TextMeshProUGUI magnificationScoreText; // 绑定倍率文本
    public TextMeshProUGUI finalScoreText;      // 最终分文本
    public TextMeshProUGUI tipText;             // 提示/规则描述文本
    public TextMeshProUGUI ruleDescText;        // 新增：显示匹配规则的description

    [Header("选英雄限制")]
    public int maxSelectCount = 5; // 最多选5个

    // 得分数据
    private float currentBaseScore = 0;       // 基础分（改为float适配小数）
    private float currentMagnification = 1;   // 倍率（改为float适配小数）
    private float currentFinalScore = 0;      // 最终分 = 基础分 × 倍率

    // 选中的武将列表（支持多选）
    private List<GeneralData> selectedGenerals = new List<GeneralData>();
    // 新增：同步记录选中的GeneralItem（用于精准反选单个英雄）
    private List<GeneralItem> selectedGeneralItems = new List<GeneralItem>();

    // 分数规则配置（复用ScoreRuleData.cs中的TotalScoreRules）
    private TotalScoreRules scoreRules;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 改用Unity内置API加载Resources中的JSON（团结引擎兼容）
        StartCoroutine(LoadScoreRulesFromResources());
        ResetScore(); // 初始化UI
    }

    /// <summary>
    /// 加载分数规则配置文件（团结引擎适配，复用外部类定义）
    /// </summary>
    private IEnumerator LoadScoreRulesFromResources()
    {
        // 读取Resources目录下的JSON文件（路径：Assets/Resources/Custom/Data/ScoreRules.json）
        string jsonPath = "Custom/Data/ScoreRules"; // Resources路径无需.json后缀
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);

        if (jsonFile == null)
        {
            Debug.LogError($"❌ 未找到分数规则文件：{jsonPath}.json，请检查Resources路径");
            yield break;
        }

        try
        {
            // 用Unity内置JsonUtility解析，复用ScoreRuleData.cs中的TotalScoreRules
            scoreRules = JsonUtility.FromJson<TotalScoreRules>(jsonFile.text);
            if (scoreRules == null)
            {
                Debug.LogError("❌ 分数规则JSON解析失败，请检查JSON格式是否匹配TotalScoreRules结构");
                yield break;
            }

            Debug.Log($"✅ 成功加载分数规则：baseRules={scoreRules.baseRules.Count}条 | shuRules={scoreRules.shuRules.Count}条");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JSON解析异常：{e.Message}\n请检查ScoreRuleData.cs与JSON结构是否一致");
        }
    }

    /// <summary>
    /// 选择武将时调用：计算得分并更新UI（加数量限制）
    /// 适配新增的GeneralItem参数，保留原有所有逻辑
    /// </summary>
    public void CalculateScore(GeneralData generalData, GeneralItem currentItem)
    {
        if (generalData == null || scoreRules == null) return;

        // 1. 检查是否已选中该英雄（避免重复添加：同时校验Data和Item）
        if (selectedGenerals.Contains(generalData) || selectedGeneralItems.Contains(currentItem)) return;

        // 2. 数量限制：超过5个时，先移除第一个选中的英雄
        if (selectedGenerals.Count >= maxSelectCount)
        {
            GeneralData firstGeneral = selectedGenerals[0];
            GeneralItem firstGeneralItem = selectedGeneralItems[0];

            // 从列表移除（同步移除Data和Item）
            selectedGenerals.RemoveAt(0);
            selectedGeneralItems.RemoveAt(0);

            // 触发反选事件（通知该英雄取消选中）
            OnGeneralDeselected?.Invoke(firstGeneral.generalName);
            // 强制取消该英雄的选中状态（兜底）
            firstGeneralItem.isSelected = false;
            firstGeneralItem.UpdateCardScale();

            Debug.Log($"⚠️ 已选满{maxSelectCount}个英雄，自动取消选中：{firstGeneral.generalName}");
        }

        // 3. 添加当前选中的英雄（同步添加Data和Item）
        selectedGenerals.Add(generalData);
        selectedGeneralItems.Add(currentItem);

        // 4. 计算基础分（所有选中武将的基础分之和）
        currentBaseScore = selectedGenerals.Sum(d => (float)d.baseScore);

        // 5. 计算倍率（根据JSON规则）
        string matchedRuleDesc = ""; // 记录匹配规则的描述
        currentMagnification = CalculateMagnificationByRules(out matchedRuleDesc);

        // 6. 计算最终分
        currentFinalScore = currentBaseScore * currentMagnification;

        // 7. 更新UI（包含规则描述）
        UpdateScoreUI(matchedRuleDesc);
    }

    /// <summary>
    /// 兼容旧版调用（无GeneralItem参数），避免报错
    /// </summary>
    public void CalculateScore(GeneralData generalData)
    {
        CalculateScore(generalData, null);
    }

    /// <summary>
    /// 根据JSON配置的规则计算倍率（核心逻辑，复用外部类定义）
    /// </summary>
    private float CalculateMagnificationByRules(out string matchedRuleDesc)
    {
        float totalMagnification = 1f; // 基础倍率为1
        matchedRuleDesc = "无匹配规则"; // 默认描述
        List<string> matchedRuleNames = new List<string>();

        // 1. 先匹配基础规则（baseRules）
        foreach (var rule in scoreRules.baseRules)
        {
            if (IsRuleMatched(rule))
            {
                totalMagnification = rule.baseMagnification;
                totalMagnification = ApplyBonus(rule, totalMagnification);
                matchedRuleNames.Add(rule.ruleName);
                matchedRuleDesc = rule.description; // 记录匹配的描述
            }
        }

        // 2. 匹配蜀阵营规则（shuRules）
        if (selectedGenerals.Any(g => g.camp == "Shu") && scoreRules.shuRules != null)
        {
            foreach (var rule in scoreRules.shuRules)
            {
                if (IsRuleMatched(rule))
                {
                    totalMagnification = rule.baseMagnification;
                    totalMagnification = ApplyBonus(rule, totalMagnification);
                    matchedRuleNames.Add(rule.ruleName);
                    matchedRuleDesc = rule.description;
                }
            }
        }

        // 3. 匹配魏阵营规则（weiRules）
        if (selectedGenerals.Any(g => g.camp == "Wei") && scoreRules.weiRules != null)
        {
            foreach (var rule in scoreRules.weiRules)
            {
                if (IsRuleMatched(rule))
                {
                    totalMagnification = rule.baseMagnification;
                    totalMagnification = ApplyBonus(rule, totalMagnification);
                    matchedRuleNames.Add(rule.ruleName);
                    matchedRuleDesc = rule.description;
                }
            }
        }

        // 4. 匹配吴阵营规则（wuRules）
        if (selectedGenerals.Any(g => g.camp == "Wu") && scoreRules.wuRules != null)
        {
            foreach (var rule in scoreRules.wuRules)
            {
                if (IsRuleMatched(rule))
                {
                    totalMagnification = rule.baseMagnification;
                    totalMagnification = ApplyBonus(rule, totalMagnification);
                    matchedRuleNames.Add(rule.ruleName);
                    matchedRuleDesc = rule.description;
                }
            }
        }

        // 5. 匹配混合规则（mixRules）
        if (scoreRules.mixRules != null)
        {
            foreach (var rule in scoreRules.mixRules)
            {
                if (IsRuleMatched(rule))
                {
                    totalMagnification = rule.baseMagnification;
                    totalMagnification = ApplyBonus(rule, totalMagnification);
                    matchedRuleNames.Add(rule.ruleName);
                    matchedRuleDesc = rule.description;
                }
            }
        }

        // 输出匹配的规则日志
        if (matchedRuleNames.Count > 0)
        {
            Debug.Log($"🔍 匹配规则：{string.Join("、", matchedRuleNames)} | 最终倍率：{totalMagnification}");
        }

        return totalMagnification;
    }

    /// <summary>
    /// 检查单个规则是否匹配选中的武将列表（复用RuleCondition类）
    /// </summary>
    private bool IsRuleMatched(ScoreRule rule)
    {
        if (rule.condition == null) return false;
        var condition = rule.condition;
        int selectedCount = selectedGenerals.Count;

        // 1. 基础数量校验
        if (condition.minCount > 0 && selectedCount < condition.minCount) return false;
        if (condition.maxCount > 0 && selectedCount > condition.maxCount) return false;

        // 2. 排除角色类型校验（roleType → role）
        if (condition.excludeRoles != null && condition.excludeRoles.Count > 0)
        {
            if (selectedGenerals.Any(g => condition.excludeRoles.Contains(g.role))) return false;
        }

        // 3. 角色类型校验（roleType → role）
        if (condition.roleTypes != null && condition.roleTypes.Count > 0)
        {
            if (!selectedGenerals.All(g => condition.roleTypes.Contains(g.role))) return false;
        }

        // 4. 同角色类型要求（roleType → role）
        if (condition.sameRoleRequired)
        {
            if (selectedGenerals.Select(g => g.role).Distinct().Count() > 1) return false;
        }

        // 5. 阵营校验（保留）
        if (!string.IsNullOrEmpty(condition.camp))
        {
            if (!selectedGenerals.All(g => g.camp == condition.camp)) return false;
        }

        // 6. 连续分值要求（保留）
        if (condition.continuousScoreRequired)
        {
            var sortedScores = selectedGenerals.Select(g => g.baseScore).OrderBy(s => s).ToList();
            for (int i = 1; i < sortedScores.Count; i++)
            {
                if (sortedScores[i] - sortedScores[i - 1] != 1) return false;
            }
        }

        // 7. 角色配置组合校验（roleType → role）
        if (condition.roleConfigs != null && condition.roleConfigs.Count > 0)
        {
            foreach (var config in condition.roleConfigs)
            {
                // 注意：这里还要把 config.roleType 改成 config.role（如果你的RoleConfig类里是role）
                int count = selectedGenerals.Count(g => g.role == config.role);
                if (count != config.count) return false;
            }
        }

        // 8. 阵营-角色配置组合校验（roleType → role）
        if (condition.campConfigs != null && condition.campConfigs.Count > 0)
        {
            foreach (var config in condition.campConfigs)
            {
                // 同样：config.roleType → config.role（如果CampRoleConfig类里是role）
                int count = selectedGenerals.Count(g => g.camp == config.camp && g.role == config.role);
                if (count != config.count) return false;
            }
        }

        // 9. 指定武将组合校验（保留）
        if (condition.specificGenerals != null && condition.specificGenerals.Count > 0)
        {
            var selectedNames = selectedGenerals.Select(g => g.generalName).ToList();
            if (!condition.specificGenerals.All(name => selectedNames.Contains(name))) return false;
        }

        return true;
    }

    /// <summary>
    /// 应用规则的奖励计算
    /// </summary>
    private float ApplyBonus(ScoreRule rule, float currentMagnification)
    {
        switch (rule.bonusType)
        {
            case "None":
                return currentMagnification;

            case "BaseScore_Multiply":
                // 基础分乘以奖励值
                currentBaseScore *= rule.bonusValue;
                return currentMagnification;

            case "Magnification_Add":
                // 倍率增加奖励值
                currentMagnification += rule.bonusValue;
                return currentMagnification;

            case "BaseScore_Add_PerCard":
                // 每张牌基础分增加奖励值
                currentBaseScore += selectedGenerals.Count * rule.bonusValue;
                return currentMagnification;

            default:
                Debug.LogWarning($"⚠️ 未处理的奖励类型：{rule.bonusType}");
                return currentMagnification;
        }
    }

    /// <summary>
    /// 重置得分（取消所有选中时调用，保留原有逻辑）
    /// </summary>
    public void ResetScore()
    {
        // 触发事件：通知所有选中的英雄反选
        foreach (var general in selectedGenerals)
        {
            OnGeneralDeselected?.Invoke(general.generalName);
        }

        selectedGenerals.Clear();
        selectedGeneralItems.Clear(); // 同步清空Item列表
        currentBaseScore = 0;
        currentMagnification = 1;
        currentFinalScore = 0;

        UpdateScoreUI("请选择英雄（最多5个）");
    }

    /// <summary>
    /// 取消单个英雄选中（核心修改：只取消当前英雄，不影响其他）
    /// </summary>
    public void DeselectSingleGeneral(GeneralItem generalItem)
    {
        if (generalItem == null)
        {
            Debug.LogWarning("⚠️ 传入的GeneralItem为空，无需取消");
            return;
        }

        // 核心修改：先按名称找，再按Item找（兼容交换后的名称变更）
        GeneralData targetData = selectedGenerals.FirstOrDefault(d =>
            d != null && d.generalName == generalItem.generalName
        );

        // 容错：找不到数据时，直接清空该Item的选中状态，不报错
        if (targetData == null || !selectedGeneralItems.Contains(generalItem))
        {
            // 兜底：强制取消UI选中状态（避免卡片卡住选中样式）
            generalItem.isSelected = false;
            generalItem.UpdateCardScale();
            Debug.LogWarning($"⚠️ 未找到[{generalItem.generalName}]的选中记录，已强制取消UI选中状态");
            return;
        }

        // 从列表移除（同步移除Data和Item）
        selectedGenerals.Remove(targetData);
        selectedGeneralItems.Remove(generalItem);

        // 取消该英雄的选中状态
        generalItem.isSelected = false;
        generalItem.UpdateCardScale();

        // 重新计算所有分数（和选中时逻辑一致）
        currentBaseScore = selectedGenerals.Sum(d => (float)d.baseScore);
        string matchedRuleDesc = "";
        currentMagnification = CalculateMagnificationByRules(out matchedRuleDesc);
        currentFinalScore = currentBaseScore * currentMagnification;

        // 更新UI
        UpdateScoreUI(matchedRuleDesc);

        Debug.Log($"❌ 取消选中[{generalItem.generalName}] | 基础分：{currentBaseScore} | 倍率：{currentMagnification} | 最终分：{currentFinalScore}");
    }

    public void ClearGeneralSelectRecord(string generalName)
    {
        if (string.IsNullOrEmpty(generalName)) return;

        // 移除该名称对应的所有数据和Item
        var targetDatas = selectedGenerals.Where(d => d != null && d.generalName == generalName).ToList();
        var targetItems = selectedGeneralItems.Where(item => item != null && item.generalName == generalName).ToList();

        foreach (var data in targetDatas)
        {
            selectedGenerals.Remove(data);
        }
        foreach (var item in targetItems)
        {
            selectedGeneralItems.Remove(item);
            // 强制取消UI选中状态
            item.isSelected = false;
            item.UpdateCardScale();
        }

        if (targetDatas.Count > 0)
        {
            Debug.Log($"✅ 清空[{generalName}]的选中记录，共移除{targetDatas.Count}条");
            // 重新计算分数
            currentBaseScore = selectedGenerals.Sum(d => (float)d.baseScore);
            string matchedRuleDesc = "";
            currentMagnification = CalculateMagnificationByRules(out matchedRuleDesc);
            currentFinalScore = currentBaseScore * currentMagnification;
            UpdateScoreUI(matchedRuleDesc);
        }
    }

    public void ResetAllSelectState()
    {
        // 强制取消所有Item的选中状态
        foreach (var item in selectedGeneralItems)
        {
            if (item != null)
            {
                item.isSelected = false;
                item.UpdateCardScale();
            }
        }
        selectedGenerals.Clear();
        selectedGeneralItems.Clear();
        ResetScore();
    }

    /// <summary>
    /// 更新所有得分UI（包含规则描述）
    /// </summary>
    private void UpdateScoreUI(string ruleDescription)
    {
        // 基础分（保留1位小数）
        if (baseScoreText != null) baseScoreText.text = currentBaseScore.ToString("0.0");
        // 倍率（保留1位小数）
        if (magnificationScoreText != null) magnificationScoreText.text = currentMagnification.ToString("0.0");
        // 最终分（保留1位小数）
        if (finalScoreText != null) finalScoreText.text = currentFinalScore.ToString("0.0");

        // 提示文本（显示已选数量）
        if (tipText != null)
        {
            string selectedNames = selectedGenerals.Count > 0
                ? string.Join("、", selectedGenerals.Select(d => d.generalName))
                : "无";

            tipText.text = $"已选{selectedGenerals.Count}/{maxSelectCount}个：{selectedNames}";
        }

        // 规则描述文本（核心需求：显示ScoreRules.json的description）
        if (ruleDescText != null)
        {
            ruleDescText.text = ruleDescription;
        }
    }
}