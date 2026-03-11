using UnityEngine;
using System.Collections.Generic;

public class RadarNode : MonoBehaviour
{
    // 🚀 核心新增：全局静态列表，保存战场上所有在线的雷达节点
    public static List<RadarNode> AllNodes = new List<RadarNode>();

    [Header("探测参数")]
    public float scanRadius = 2500f; // 探测半径
    public bool checkLineOfSight = true; // 是否受地形阻挡（可选）
    public LayerMask obstructionMask; // 地形层（用于遮挡判定）

    // 🚀 静态注册：当雷达激活时，自动加入全网
    private void OnEnable()
    {
        if (!AllNodes.Contains(this))
        {
            AllNodes.Add(this);
            Debug.Log($"[雷达网] 节点 {gameObject.name} 已上线，全网当前节点数：{AllNodes.Count}");
        }
    }

    // 🚀 静态注销：当雷达被摧毁或关闭时，自动从全网移除
    private void OnDisable()
    {
        if (AllNodes.Contains(this))
        {
            AllNodes.Remove(this);
            Debug.Log($"[雷达网] 节点 {gameObject.name} 已离线");
        }
    }

    // 🚀 核心新增：CanSee 探测算法
    public bool CanSee(Vector3 targetPos)
    {
        float dist = Vector3.Distance(transform.position, targetPos);

        // 1. 距离判定
        if (dist > scanRadius) return false;

        // 2. 地形遮挡判定 (射线检测)
        if (checkLineOfSight)
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            if (Physics.Raycast(transform.position, direction, dist, obstructionMask))
            {
                return false; // 被山体或建筑挡住了
            }
        }

        return true; // 目标暴露在雷达波内！
    }
}