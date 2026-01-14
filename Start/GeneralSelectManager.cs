using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 无任何编辑器相关引用，纯运行时脚本
public class GeneralSelectManager : MonoBehaviour
{
    [Header("配置参数")]
    public Transform generalContent; // 武将展示项父容器
    public GameObject generalItemPrefab; // 武将展示项预制体
    public int selectCount = 8; // 随机选择数量

    private List<GameObject> allGeneralPrefabs = new List<GameObject>();
    private List<GameObject> selectedGenerals = new List<GameObject>();

    void Start()
    {
        LoadAllGeneralPrefabs();
        RandomSelectGenerals();
        GenerateGeneralItems();
    }

    /// <summary>
    /// 运行时加载Resources/Generals下的所有武将预制体
    /// </summary>
    void LoadAllGeneralPrefabs()
    {
        allGeneralPrefabs.Clear();
        // 仅用Resources.LoadAll（运行时唯一合法的加载方式）
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Custom/Heros");

        if (prefabs.Length == 0)
        {
            Debug.LogWarning("未加载到武将预制体！请检查：" +
                "\n1. 预制体是否在Assets/Resources/Generals/目录下；" +
                "\n2. 路径区分大小写（如Generals而非generals）；" +
                "\n3. 预制体是GameObject类型（非Mesh/Texture）。");
            return;
        }

        allGeneralPrefabs.AddRange(prefabs);
        foreach (var prefab in allGeneralPrefabs)
        {
            Debug.Log($"加载到武将：{prefab.name}");
        }
    }

    /// <summary>
    /// 随机选8个不重复武将
    /// </summary>
    void RandomSelectGenerals()
    {
        selectedGenerals.Clear();
        if (allGeneralPrefabs.Count < selectCount)
        {
            Debug.LogWarning($"武将数量不足{selectCount}个，仅能选{allGeneralPrefabs.Count}个");
            selectedGenerals = new List<GameObject>(allGeneralPrefabs);
            return;
        }

        List<GameObject> tempList = new List<GameObject>(allGeneralPrefabs);
        for (int i = 0; i < selectCount; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            selectedGenerals.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }
    }

    /// <summary>
    /// 生成武将展示项到选择面板
    /// </summary>
    void GenerateGeneralItems()
    {
        // 清空原有项
        foreach (Transform child in generalContent)
        {
            Destroy(child.gameObject);
        }

        // 生成新项
        foreach (GameObject generalPrefab in selectedGenerals)
        {
            GameObject itemObj = Instantiate(generalItemPrefab, generalContent);
            GeneralItem generalItem = itemObj.GetComponent<GeneralItem>();
            if (generalItem != null)
            {
                generalItem.SetGeneral(generalPrefab);
            }
        }
    }

    // 可选：重新随机按钮调用
    public void ReRandomGenerals()
    {
        RandomSelectGenerals();
        GenerateGeneralItems();
    }
}