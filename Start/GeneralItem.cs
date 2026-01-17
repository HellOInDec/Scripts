using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeneralItem : MonoBehaviour
{
    [Header("仅需填写武将名（和数据一致）")]
    public string generalName; // 武将名（刘备/曹操等）

    [Header("模型根节点（可选）")]
    public Transform modelRoot; // 模型父节点

    [Header("自动绑定：无需手动拖入")]
    public Button selectButton; // 武将选择按钮
    [Header("卡片背景（需绑定）")]
    public Image cardBackground; // 英雄卡片的背景图片（拖入即可）

    // 新增：阵营文字颜色配置（可在Inspector调整）
    [Header("阵营文字颜色")]
    public Color shuColor = new Color(1f, 0.2f, 0.2f); // 蜀国-红色
    public Color weiColor = new Color(0.2f, 0.2f, 1f); // 魏国-蓝色
    public Color wuColor = new Color(0f, 1f, 0f); // 吴国-浅绿色
    public Color defaultColor = Color.white; // 默认颜色

    // 新增：缩放相关
    private Vector3 originalScale; // 卡片原始缩放比例
    private Vector3 originalModelScale; // 模型实例的原始缩放（关键）
    private float selectScale = 1.15f; // 选中时放大到110%

    // 新增：当前加载的模型实例引用（核心修复）
    private GameObject currentGeneralModel; // 只控制这个模型的缩放

    // 新增：文字组件引用
    private TextMeshProUGUI generalNameText;

    // 选中状态标记（改为公开，让ScoreCalculator能访问）
    public bool isSelected = false;
    // 原始按钮颜色（保留，可选用于按钮高亮）
    private Color originalColor;
    // 新增：防重复绑定标记
    private bool isEventBinded = false;

    private void Awake()
    {
        // 只在Awake绑定一次点击事件，永不重复
        BindClickEventOnce();
    }

    // 新增：单次绑定点击事件的方法
    private void BindClickEventOnce()
    {
        if (isEventBinded) return; // 已绑定过，直接返回

        // 自动查找Button（避免手动绑定出错）
        if (selectButton == null)
        {
            selectButton = GetComponentInChildren<Button>();
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners(); // 清空所有旧事件
            selectButton.onClick.AddListener(OnSelectButtonClick);
            isEventBinded = true; // 标记为已绑定
            Debug.Log($"✅ [{generalName}] 点击事件绑定完成（仅绑定一次）");
        }
        else
        {
            Debug.LogError($"❌ [{generalName}] 未找到Button组件！");
        }
    }

    // 订阅反选事件
    private void OnEnable()
    {
        if (ScoreCalculator.Instance != null)
        {
            ScoreCalculator.Instance.OnGeneralDeselected += OnAutoDeselectGeneral;
        }
    }

    // 取消订阅（防止内存泄漏）
    private void OnDisable()
    {
        if (ScoreCalculator.Instance != null)
        {
            ScoreCalculator.Instance.OnGeneralDeselected -= OnAutoDeselectGeneral;
        }
    }

    /// <summary>
    /// 手动初始化（供GeneralSpawnManager调用）
    /// </summary>
    public void Init()
    {
        // 1. 先初始化文字显示（包含阵营颜色设置）
        InitGeneralNameText();

        // 2. 空值检查：selectButton
        if (selectButton == null)
        {
            Debug.LogError($"❌ GeneralItem（{gameObject.name}）：selectButton未绑定！预制体缺少Button组件", this);
            return;
        }

        // 3. 空值检查：ScoreCalculator实例
        if (ScoreCalculator.Instance == null)
        {
            Debug.LogError($"❌ GeneralItem（{gameObject.name}）：未找到ScoreCalculator实例！请检查是否挂载", this);
            return;
        }

        // 4. 空值检查：generalName
        if (string.IsNullOrEmpty(generalName))
        {
            Debug.LogError($"❌ GeneralItem（{gameObject.name}）：generalName未填写！", this);
            return;
        }

        // 5. 初始化缩放（核心：记录原始缩放）
        originalScale = transform.localScale; // 记录整个卡片的原始缩放
        if (cardBackground != null)
        {
            originalColor = cardBackground.color; // 可选：记录背景原始颜色
        }

        // 👉 关键：初始化模型实例的原始缩放（固定150，仅针对模型本身）
        originalModelScale = new Vector3(150, 150, 150);
        Debug.Log($"✅ 记录[{generalName}]模型实例原始缩放：{originalModelScale}", this);

        // 强制重置状态
        isSelected = false;
        UpdateCardScale();

        // 可选：modelRoot检查
        if (modelRoot == null)
        {
            Debug.LogWarning($"⚠️ GeneralItem（{gameObject.name}）：modelRoot未绑定（无模型可忽略）", this);
        }
    }

    /// <summary>
    /// 初始化武将名称文字显示（新增阵营颜色逻辑）
    /// </summary>
    private void InitGeneralNameText()
    {
        Transform textTrans = transform.Find("TextCanvas/GeneralNameText");
        if (textTrans != null)
        {
            generalNameText = textTrans.GetComponent<TextMeshProUGUI>();
            if (generalNameText != null && !string.IsNullOrEmpty(generalName))
            {
                // 第一步：设置武将名称
                generalNameText.text = generalName;

                // 第二步：获取武将阵营并设置文字颜色（核心新增）
                SetGeneralNameColorByCamp();

                Debug.Log($"✅ 成功给[{generalName}]赋值文字并设置阵营颜色", this);
            }
            else
            {
                Debug.LogError($"❌ GeneralItem（{gameObject.name}）：未找到GeneralNameText的TMP组件！", this);
            }
        }
        else
        {
            Debug.LogError($"❌ GeneralItem（{gameObject.name}）：未找到TextCanvas/GeneralNameText路径！", this);
        }
    }

    /// <summary>
    /// 根据武将阵营设置文字颜色（核心方法）
    /// </summary>
    private void SetGeneralNameColorByCamp()
    {
        // 获取武将数据（从数据管理器中获取阵营信息）
        GeneralData generalData = GeneralDataManager.Instance?.GetGeneralData(generalName);
        if (generalData == null)
        {
            Debug.LogWarning($"⚠️ 未找到[{generalName}]的武将数据，使用默认文字颜色", this);
            generalNameText.color = defaultColor;
            return;
        }

        // 根据阵营设置颜色（匹配GeneralData中的camp字段值，如"Shu"/"Wei"/"Wu"）
        switch (generalData.camp.ToLower()) // 转小写避免大小写问题
        {
            case "shu": // 蜀国
                generalNameText.color = shuColor;
                break;
            case "wei": // 魏国
                generalNameText.color = weiColor;
                break;
            case "wu": // 吴国
                generalNameText.color = wuColor;
                break;
            default: // 未知阵营
                generalNameText.color = defaultColor;
                Debug.LogWarning($"⚠️ [{generalName}]的阵营{generalData.camp}未配置，使用默认颜色", this);
                break;
        }
    }


    private bool isClicking = false;
    /// <summary>
    /// 按钮点击事件（核心修改：只取消当前英雄选中）
    /// </summary>
    public void OnSelectButtonClick()
    {
        // 核心防抖：同一时间只执行一次
        if (isClicking) return;
        isClicking = true;

        // 5. 调用分数计算（加try-catch+日志）
        try
        {
            // 1. 先打印调试日志（关键！看名称是否匹配）
            Debug.Log($"🔍 尝试选中：当前卡片名称=[{generalName}]");
            Debug.Log($"🔍 数据列表总数量=[{GeneralDataManager.Instance.allGeneralData.Count}]");
            // 打印前10个数据名称（方便排查）
            string first10Names = "";
            for (int i = 0; i < Mathf.Min(10, GeneralDataManager.Instance.allGeneralData.Count); i++)
            {
                first10Names += GeneralDataManager.Instance.allGeneralData[i].generalName + "、";
            }
            Debug.Log($"🔍 数据列表前10个名称：{first10Names}");

            // 2. 核心校验
            if (ScoreCalculator.Instance == null)
            {
                Debug.LogWarning("⚠️ ScoreCalculator 实例为空！");
                return;
            }
            if (GeneralDataManager.Instance == null)
            {
                Debug.LogWarning("⚠️ GeneralDataManager 实例为空！");
                return;
            }

            // 3. 精准查找数据（去掉大小写容错，严格匹配，避免问题）
            GeneralData currentData = GeneralDataManager.Instance.GetGeneralData(generalName);
            if (currentData == null)
            {
                Debug.LogError($"❌ 数据列表中无[{generalName}]，无法选中！");
                return;
            }
            Debug.Log($"✅ 找到[{generalName}]的武将数据：阵营={currentData.camp}，基础分={currentData.baseScore}");

            // 4. 切换状态
            isSelected = !isSelected;
            UpdateCardScale();

            if (isSelected)
            {
                Debug.Log($"📝 执行选中逻辑：[{generalName}]");
                ScoreCalculator.Instance.CalculateScore(currentData, this);
            }
            else
            {
                Debug.Log($"📝 执行取消选中逻辑：[{generalName}]");
                ScoreCalculator.Instance.DeselectSingleGeneral(this);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 选中/取消选中出错：{e.Message}\n{e.StackTrace}");
            isSelected = false;
            UpdateCardScale();
        }
        finally
        {
            // 延迟0.1秒重置防抖标记（避免快速点击）
            Invoke(nameof(ResetClickFlag), 0.1f);
        }
    }


    // 新增：重置防抖标记
    private void ResetClickFlag()
    {
        isClicking = false;
    }

    /// <summary>
    /// 更新卡片缩放（核心视觉效果）
    /// </summary>
    public void UpdateCardScale()
    {
        // 1. 缩放整个英雄卡片（仅当前卡片）
        transform.localScale = isSelected ? originalScale * selectScale : originalScale;
        Debug.Log($"[{generalName}]卡片缩放：{transform.localScale}", this);

        // 可选：背景高亮（仅当前卡片）
        if (cardBackground != null)
        {
            cardBackground.color = isSelected ? new Color(0.8f, 1f, 0.8f) : originalColor; // 淡绿色高亮
        }

        // 👉 核心修复：只修改当前模型实例的缩放，不碰modelRoot
        if (currentGeneralModel != null)
        {
            Vector3 targetModelScale = isSelected ? originalModelScale * selectScale : originalModelScale;
            currentGeneralModel.transform.localScale = targetModelScale; // 仅改当前模型本身
            Debug.Log($"[{generalName}]模型实例缩放：{targetModelScale}", this);
        }
    }

    /// <summary>
    /// 监听自动反选事件：仅恢复当前英雄（被挤掉时）
    /// </summary>
    /// <param name="deselectGeneralName">被自动反选的英雄名</param>
    private void OnAutoDeselectGeneral(string deselectGeneralName)
    {
        // 只处理当前英雄的自动反选
        if (generalName == deselectGeneralName)
        {
            isSelected = false;
            UpdateCardScale(); // 仅恢复当前卡片/模型
            Debug.Log($"✅ 自动反选[{generalName}]，仅取消当前英雄选中", this);
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

        // 销毁原有模型（仅当前英雄的模型）
        if (currentGeneralModel != null)
        {
            Destroy(currentGeneralModel);
        }

        // 实例化新模型（仅当前英雄）
        if (generalPrefab != null)
        {
            // 👉 保存模型实例引用（关键）
            currentGeneralModel = Instantiate(generalPrefab, modelRoot);
            currentGeneralModel.transform.localPosition = new Vector3(0, -80, 0);
            currentGeneralModel.transform.localScale = originalModelScale; // 初始缩放（150）
            currentGeneralModel.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Debug.Log($"✅ [{generalName}]模型实例初始化缩放：{originalModelScale}", this);
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
        if (ScoreCalculator.Instance != null)
        {
            ScoreCalculator.Instance.OnGeneralDeselected -= OnAutoDeselectGeneral;
        }

        // 销毁当前英雄的模型实例
        if (currentGeneralModel != null)
        {
            Destroy(currentGeneralModel);
        }
    }
}