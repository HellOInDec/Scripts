using System.Collections.Generic;
using UnityEngine;

// 总得分规则（对应ScoreRules.json根节点）
[System.Serializable]
public class TotalScoreRules
{
    public List<ScoreRule> baseRules;
    public List<ScoreRule> shuRules;
    public List<ScoreRule> weiRules;
    public List<ScoreRule> wuRules;
    public List<ScoreRule> mixRules;
}

// 单条得分规则
[System.Serializable]
public class ScoreRule
{
    public string ruleName;
    public RuleCondition condition;
    public float baseMagnification;
    public string bonusType; // None/BaseScore_Multiply/Magnification_Add/BaseScore_Add_PerCard
    public float bonusValue;
    public string description;
    public string example;
}

// 规则条件
[System.Serializable]
public class RuleCondition
{
    public List<string> roleTypes;
    public int minCount;
    public int maxCount;
    public bool sameRoleRequired;
    public bool continuousScoreRequired;
    public List<string> excludeRoles;
    public string camp;
    public List<RoleConfig> roleConfigs;
    public List<string> specificGenerals;
    public List<CampRoleConfig> campConfigs;
}

// 角色配置（如1君主+2文臣）
[System.Serializable]
public class RoleConfig
{
    public string roleType;
    public int count;
}

// 阵营-角色配置（跨阵营组合）
[System.Serializable]
public class CampRoleConfig
{
    public string camp;
    public string roleType;
    public int count;
}