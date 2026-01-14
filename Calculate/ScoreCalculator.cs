using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ScoreCalculator : MonoBehaviour
{
    // ========== 修复：把委托和事件移到类内部 ==========
    // 定义事件：当英雄被自动移除时触发（用于更新UI）
    public delegate void OnGeneralDeselectedDelegate(string generalName);
    public static event OnGeneralDeselectedDelegate OnGeneralDeselected;

    public static ScoreCalculator Instance;

    [Header("UI文本绑定（对应你的ScorePanel）")]
    public TextMeshProUGUI baseScoreText;       // 绑定BaseScore文本
    public TextMeshProUGUI magnificationScoreText; // 绑定MagnificationScore文本
    public TextMeshProUGUI finalScoreText;      // 最终分文本
    public TextMeshProUGUI tipText;             // 可选：提示文本

    [Header("选英雄限制")]
    public int maxSelectCount = 5; // 最多选5个

    // 得分数据
    private int currentBaseScore = 0;       // 基础分
    private int currentMagnification = 1;   // 倍率
    private int currentFinalScore = 0;      // 最终分 = 基础分 × 倍率

    // 选中的武将列表（支持多选）
    private List<GeneralData> selectedGenerals = new List<GeneralData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ResetScore(); // 初始化UI
    }

    /// <summary>
    /// 选择武将时调用：计算得分并更新UI（加数量限制）
    /// </summary>
    public void CalculateScore(GeneralData generalData)
    {
        if (generalData == null) return;

        // 1. 检查是否已选中该英雄（避免重复添加）
        if (selectedGenerals.Contains(generalData)) return;

        // 2. 数量限制：超过5个时，先移除第一个选中的英雄
        if (selectedGenerals.Count >= maxSelectCount)
        {
            // 获取第一个要移除的英雄
            GeneralData firstGeneral = selectedGenerals[0];
            // 从列表移除
            selectedGenerals.RemoveAt(0);
            // 触发事件：通知该英雄的UI反选（按钮恢复原色）
            OnGeneralDeselected?.Invoke(firstGeneral.generalName);
            // 提示日志
            Debug.Log($"⚠️ 已选满{maxSelectCount}个英雄，自动取消选中：{firstGeneral.generalName}");
        }

        // 3. 添加当前选中的英雄
        selectedGenerals.Add(generalData);

        // 4. 计算基础分（所有选中武将的基础分之和）
        currentBaseScore = selectedGenerals.Sum(d => d.baseScore);

        // 5. 计算倍率（示例：3个同阵营武将，倍率+1）
        currentMagnification = CalculateMagnification();

        // 6. 计算最终分
        currentFinalScore = currentBaseScore * currentMagnification;

        // 7. 更新UI文本
        UpdateScoreUI();
    }

    /// <summary>
    /// 计算倍率（可根据需求扩展）
    /// </summary>
    private int CalculateMagnification()
    {
        int magnification = 1;

        // 示例规则：3个同阵营武将，倍率+1
        Dictionary<string, int> campCount = new Dictionary<string, int>();
        foreach (var data in selectedGenerals)
        {
            if (campCount.ContainsKey(data.camp)) campCount[data.camp]++;
            else campCount[data.camp] = 1;
        }
        foreach (var count in campCount.Values)
        {
            if (count >= 3) magnification++;
        }

        return magnification;
    }

    /// <summary>
    /// 重置得分（取消选中时调用）
    /// </summary>
    public void ResetScore()
    {
        // 触发事件：通知所有选中的英雄反选
        foreach (var general in selectedGenerals)
        {
            OnGeneralDeselected?.Invoke(general.generalName);
        }

        selectedGenerals.Clear();
        currentBaseScore = 0;
        currentMagnification = 1;
        currentFinalScore = 0;

        UpdateScoreUI();
    }

    /// <summary>
    /// 更新所有得分UI
    /// </summary>
    private void UpdateScoreUI()
    {
        // 基础分
        if (baseScoreText != null) baseScoreText.text = currentBaseScore.ToString();
        // 倍率
        if (magnificationScoreText != null) magnificationScoreText.text = currentMagnification.ToString();
        // 最终分
        if (finalScoreText != null) finalScoreText.text = currentFinalScore.ToString();
        // 提示文本（显示已选数量）
        if (tipText != null)
        {
            tipText.text = selectedGenerals.Count > 0
                ? $"已选{selectedGenerals.Count}/{maxSelectCount}个：{string.Join("、", selectedGenerals.Select(d => d.generalName))}"
                : $"请选择英雄（最多{maxSelectCount}个）";
        }
    }
}