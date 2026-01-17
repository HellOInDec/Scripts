// 必须添加这行！StringComparison 属于 System 命名空间
using System;
using System.Collections.Generic;
using UnityEngine;

public class GeneralDataManager : MonoBehaviour
{
    // 单例实例
    public static GeneralDataManager Instance;

    // 所有武将数据
    public List<GeneralData> allGeneralData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 加载武将数据
        LoadExampleGeneralData();
    }

    // 加载示例武将数据（你之前补充的45个武将）
    private void LoadExampleGeneralData()
    {
        allGeneralData = new List<GeneralData>()
        {
            // 君主牌（6张）
            new GeneralData(){ generalName = "曹操", camp = "Wei", role = "Monarch", baseScore = 10 },
            new GeneralData(){ generalName = "曹丕", camp = "Wei", role = "Monarch", baseScore = 10 },
            new GeneralData(){ generalName = "刘备", camp = "Shu", role = "Monarch", baseScore = 10 },
            new GeneralData(){ generalName = "刘禅", camp = "Shu", role = "Monarch", baseScore = 10 },
            new GeneralData(){ generalName = "孙权", camp = "Wu", role = "Monarch", baseScore = 10 },
            new GeneralData(){ generalName = "孙策", camp = "Wu", role = "Monarch", baseScore = 10 },

            // 武将牌（15张）
            new GeneralData(){ generalName = "典韦", camp = "Wei", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "许褚", camp = "Wei", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "张辽", camp = "Wei", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "夏侯惇", camp = "Wei", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "张郃", camp = "Wei", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "关羽", camp = "Shu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "张飞", camp = "Shu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "赵云", camp = "Shu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "马超", camp = "Shu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "黄忠", camp = "Shu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "周瑜", camp = "Wu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "吕蒙", camp = "Wu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "甘宁", camp = "Wu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "太史慈", camp = "Wu", role = "General", baseScore = 8 },
            new GeneralData(){ generalName = "周泰", camp = "Wu", role = "General", baseScore = 8 },

            // 文臣牌（15张）
            new GeneralData(){ generalName = "司马懿", camp = "Wei", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "郭嘉", camp = "Wei", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "荀彧", camp = "Wei", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "贾诩", camp = "Wei", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "程昱", camp = "Wei", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "诸葛亮", camp = "Shu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "庞统", camp = "Shu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "法正", camp = "Shu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "简雍", camp = "Shu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "糜竺", camp = "Shu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "陆逊", camp = "Wu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "鲁肃", camp = "Wu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "张昭", camp = "Wu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "张纮", camp = "Wu", role = "CivilOfficer", baseScore = 7 },
            new GeneralData(){ generalName = "顾雍", camp = "Wu", role = "CivilOfficer", baseScore = 7 },

            // 士兵牌（15张）
            new GeneralData(){ generalName = "魏兵1", camp = "Wei", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "魏兵2", camp = "Wei", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "魏兵3", camp = "Wei", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "魏兵4", camp = "Wei", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "魏兵5", camp = "Wei", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "蜀兵1", camp = "Shu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "蜀兵2", camp = "Shu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "蜀兵3", camp = "Shu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "蜀兵4", camp = "Shu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "蜀兵5", camp = "Shu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "吴兵1", camp = "Wu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "吴兵2", camp = "Wu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "吴兵3", camp = "Wu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "吴兵4", camp = "Wu", role = "Soldier", baseScore = 2 },
            new GeneralData(){ generalName = "吴兵5", camp = "Wu", role = "Soldier", baseScore = 2 }
        };
    }

    // 通过武将名查找数据（修复后）
    // 通过武将名查找数据（严格匹配版）
    public GeneralData GetGeneralData(string generalName)
    {
        if (string.IsNullOrEmpty(generalName))
        {
            Debug.LogError("❌ 武将名为空，无法查找数据！");
            return null;
        }

        // 严格匹配（去掉IgnoreCase，避免「荀彧」和「荀或」等错误匹配）
        foreach (var data in allGeneralData)
        {
            if (data == null) continue; // 跳过空数据
            if (data.generalName == generalName)
            {
                return data;
            }
        }

        // 打印更详细的错误日志（方便排查）
        Debug.LogError($"❌ 未找到[{generalName}]对应的武将数据！");
        Debug.Log($"❌ 数据列表中所有名称：");
        foreach (var data in allGeneralData)
        {
            if (data != null) Debug.Log($"   - {data.generalName}");
        }
        return null;
    }
}