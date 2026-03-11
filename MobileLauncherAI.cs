using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 强制要求挂载该脚本的物体必须有 NavMeshAgent 组件
[RequireComponent(typeof(NavMeshAgent))]
public class MobileLauncherAI : MonoBehaviour
{
    [Header(" 行驶与导航设定")]
    [Tooltip("敌车要在哪里停下发射？(拖入一个空物体)")]
    public Transform launchPosition;
    private NavMeshAgent agent;

    [Header(" 武器与打击目标")]
    [Tooltip("拖入 TBM-90 陨石预制体")]
    public GameObject missilePrefab;
    [Tooltip("导弹从车上的哪个位置发射出去？(拖入车身上的一个空物体)")]
    public Transform missileSpawnPoint;
    [Tooltip("导弹要砸向哪里？(拖入你的主防空阵地)")]
    public Transform playerBaseTarget;

    [Header(" 战术时间轴")]
    public float setupTime = 5.0f; // 停车、展开液压支撑、起竖导弹所需时间
    public float reloadTime = 10.0f; // 再次发射的装填时间 (如果只有一发，可以改写逻辑)

    // 定义 AI 的三大战术状态 (有限状态机)
    private enum LauncherState { Moving, SettingUp, Firing }
    private LauncherState currentState;

    void Start()
    {
        // 1. 获取寻路大脑
        agent = GetComponent<NavMeshAgent>();

        // 2. 挂挡起步，向发射阵地进发！
        if (launchPosition != null)
        {
            agent.SetDestination(launchPosition.position);
            currentState = LauncherState.Moving; // 初始状态设为：移动中
            Debug.Log("敌方发射车已出动，正在前往预设阵地...");
        }
    }

    void Update()
    {
        // 状态机中枢：根据当前状态执行不同逻辑
        switch (currentState)
        {
            case LauncherState.Moving:
                // 判断是否到达目的地 (用 0.5f 作为容差阈值，防止极小偏差导致永远不停)
                if (!agent.pathPending && agent.remainingDistance <= 0.5f)
                {
                    agent.isStopped = true; // 拉起手刹！
                    currentState = LauncherState.SettingUp; // 切换状态：开始布置阵地

                    // 启动异步任务：起竖与发射流程
                    StartCoroutine(SetupAndFireSequence());
                }
                break;

            case LauncherState.SettingUp:
                // 正在布置阵地，Update 不需要做任何事，交给协程等待
                break;

            case LauncherState.Firing:
                // 正在持续开火，交由协程循环处理
                break;
        }
    }

    // 异步协同程序：处理需要“死等”的时间轴
    IEnumerator SetupAndFireSequence()
    {
        Debug.Log("⚠️ 警报：敌方发射车已停稳，正在起竖导弹！");

        // 模拟起竖发射架的时间
        yield return new WaitForSeconds(setupTime);

        currentState = LauncherState.Firing;

        // 进入火力打击循环 (你可以设定一个 maxAmmo 变量来控制发射数量)
        while (true)
        {
            FireMissile();
            Debug.Log("💥 敌方弹道导弹已升空！准备下一发装填...");

            // 等待装填时间后再次发射
            yield return new WaitForSeconds(reloadTime);
        }
    }

    // 点火执行函数
    void FireMissile()
    {
        if (missilePrefab != null && missileSpawnPoint != null)
        {
            // 1. 在发射口生成导弹的克隆体
            GameObject spawnedMissile = Instantiate(missilePrefab, missileSpawnPoint.position, missileSpawnPoint.rotation);

            // 2. 将防空阵地(目标)的坐标，强行注入给导弹的飞行代码
            // 【注意】这里假设你的导弹是用 TargetFlight 脚本控制的，如果不是，请改成你实际的脚本名！
            /* TargetFlight flightLogic = spawnedMissile.GetComponent<TargetFlight>();
            if (flightLogic != null) 
            { 
                flightLogic.target = playerBaseTarget; 
            }
            */
        }
    }
}