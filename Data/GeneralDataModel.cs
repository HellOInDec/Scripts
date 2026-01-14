using System.Collections.Generic;
using UnityEngine;

// 武将基础数据结构（全局唯一，无重复）
[System.Serializable]
public class GeneralData
{
    public string generalName; // 武将名称（如刘备、曹操）
    public string englishName; // 新增：英文名称（如：LiuBei）
    public string camp;        // 阵营（Wei/Shu/Wu）
    public string role;        // 角色类型（Monarch/General/CivilOfficer/Soldier）
    public int baseScore;      // 基础分（君主10、武将8、文臣7、士兵2）
}

// 用于解析武将数据JSON的容器类
[System.Serializable]
public class GeneralDataList
{
    public List<GeneralData> generalList;
}