using UnityEngine;
using UnityEngine.UI;

public class GeneralItem : MonoBehaviour
{
    [Header("仅需填写武将名（和数据一致）")]
    public string generalName; // 武将名（刘备/曹操等）

    [Header("模型根节点（可选）")]
    public Transform modelRoot; // 模型父节点

    [Header("自动绑定：无需手动拖入")]
    public Button selectButton; // 武将选择按钮

    // 选中状态标记
    private bool isSelected = false;
    // 原始按钮颜色
    private Color originalColor;

    // ========== 修复：访问ScoreCalculator类的静态事件 ==========
    // 订阅反选事件
    private void OnEnable()
    {
        ScoreCalculator.OnGeneralDeselected += OnAutoDeselectGeneral;
    }

    // 取消订阅（防止内存泄漏）
    private void OnDisable()
    {
        ScoreCalculator.OnGeneralDeselected -= OnAutoDeselectGeneral;
    }

    /// <summary>
    /// 手动初始化（供GeneralSpawnManager调用）
    /// </summary>
    public void Init()
    {
        // 空值检查：selectButton
        if (selectButton == null)
        {
            Debug.LogError($"❌ GeneralItem（{gameObject.name}）：selectButton未绑定！预制体缺少Button组件", this);
            return;
        }

        // 空值检查：ScoreCalculator实例
        if (ScoreCalculator.Instance == null)
        {
            Debug.LogError($"❌ GeneralItem（{gameObject.name}）：未找到ScoreCalculator实例！请检查是否挂载", this);
            return;
        }

        // 空值检查：generalName
        if (string.IsNullOrEmpty(generalName))
        {
            Debug.LogError($"❌ GeneralItem（{gameObject.name}）：generalName未填写！", this);
            return;
        }

        // 记录原始按钮颜色
        originalColor = selectButton.image.color;

        // 绑定按钮点击事件（得分计算的核心）
        selectButton.onClick.AddListener(OnSelectButtonClick);

        // 可选：modelRoot检查（无模型可忽略）
        if (modelRoot == null)
        {
            Debug.LogWarning($"⚠️ GeneralItem（{gameObject.name}）：modelRoot未绑定（无模型可忽略）", this);
        }
    }

    /// <summary>
    /// 按钮点击事件（触发得分计算）
    /// </summary>
    private void OnSelectButtonClick()
    {
        isSelected = !isSelected;

        // 按钮视觉反馈
        selectButton.image.color = isSelected ? Color.green : originalColor;

        if (isSelected)
        {
            // 获取武将数据
            GeneralData generalData = GeneralDataManager.Instance?.GetGeneralData(generalName);
            if (generalData != null)
            {
                // 调用得分计算
                ScoreCalculator.Instance.CalculateScore(generalData);
                Debug.Log($"✅ 选中[{generalName}]，基础分：{generalData.baseScore}");
            }
            else
            {
                Debug.LogError($"❌ 未找到[{generalName}]的武将数据！");
                isSelected = false;
                selectButton.image.color = originalColor;
            }
        }
        else
        {
            // 重置得分
            ScoreCalculator.Instance.ResetScore();
            Debug.Log($"✅ 取消选中[{generalName}]，重置得分");
        }
    }

    /// <summary>
    /// 监听自动反选事件：恢复按钮颜色
    /// </summary>
    /// <param name="deselectGeneralName">被自动反选的英雄名</param>
    private void OnAutoDeselectGeneral(string deselectGeneralName)
    {
        // 匹配当前英雄名，才更新状态
        if (generalName == deselectGeneralName)
        {
            isSelected = false;
            selectButton.image.color = originalColor;
            Debug.Log($"✅ 自动反选[{generalName}]，按钮已恢复原色");
        }
    }

    /// <summary>
    /// 加载武将模型（外部调用）
    /// </summary>
    /// <param name="generalPrefab">模型预制体</param>
    public void SetGeneral(GameObject generalPrefab)
    {
        if (modelRoot == null)
        {
            Debug.LogWarning($"⚠️ GeneralItem（{gameObject.name}）：modelRoot为null，无法加载模型", this);
            return;
        }

        // 销毁原有模型
        if (modelRoot.childCount > 0)
        {
            Destroy(modelRoot.GetChild(0).gameObject);
        }

        // 实例化新模型
        if (generalPrefab != null)
        {
            GameObject model = Instantiate(generalPrefab, modelRoot);
            model.transform.localPosition = new Vector3(0, -80, 0);
            model.transform.localScale = new Vector3(150f, 150f, 150f);
            // 统一朝向屏幕（避免朝左问题）
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            Debug.LogWarning($"⚠️ GeneralItem（{gameObject.name}）：模型预制体为null", this);
        }
    }

    // 销毁时移除事件监听，防止内存泄漏
    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectButtonClick);
        }
        // ========== 修复：同样加类名前缀 ==========
        ScoreCalculator.OnGeneralDeselected -= OnAutoDeselectGeneral;
    }
}