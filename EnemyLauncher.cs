using UnityEngine;
using System.Collections;

public class EnemyLauncher : MonoBehaviour
{
    [Header("敌方武库配置")]
    public GameObject ballisticMissilePrefab; // 战术弹道导弹 (TBM)
    public GameObject suicideDronePrefab;     // 自杀式无人机 (UAV)
    public Transform launchPoint;             // 发射井/发射架的具体位置

    [Header("战术发射计划")]
    public float firstStrikeDelay = 5f;       // 开局多久后发动第一波打击
    public float launchInterval = 8f;         // 每波打击的间隔

    private int threatCounter = 200;          // 威胁目标编号流水线

   




    void Start()
    {
        Debug.LogWarning($"[军事情报] 侦测到敌方发射阵地活动！坐标: {transform.position}");
        
        StartCoroutine(ExecuteLaunchSequence());
    }

    IEnumerator ExecuteLaunchSequence()
    {
        yield return new WaitForSeconds(firstStrikeDelay);

        while (true)
        {
            // 战术掷骰子：30% 概率发射弹道导弹，70% 概率发射无人机群
            bool isBallisticAttack = Random.value < 0.3f;
            GameObject weaponPrefab = isBallisticAttack ? ballisticMissilePrefab : suicideDronePrefab;

            if (weaponPrefab != null && launchPoint != null)
            {
                // 在发射架位置生成武器实体
                GameObject threat = Instantiate(weaponPrefab, launchPoint.position, launchPoint.rotation);
                threatCounter++;

                // 1. 挂载数字孪生遥测标识 (让 WPF 雷达能识别它)
                UdpTelemetrySender telemetry = threat.GetComponent<UdpTelemetrySender>();
                if (telemetry != null)
                {
                    // 弹道导弹叫 TBM-xxx，无人机叫 UAV-xxx
                    telemetry.flightId = (isBallisticAttack ? "TBM-" : "UAV-") + threatCounter;
                    telemetry.flightType = isBallisticAttack ? "Missile" : "UAV";
                    telemetry.iffStatus = "Unknown"; // 雷达全盘爆红
                    threat.name = telemetry.flightId;
                    // ====== 【核心新增：启用电子战模块】 ======
                    telemetry.hasECM = true;
                }

                // 2. 核心物理引擎：根据武器类型给予不同的初速度/动力
                if (isBallisticAttack)
                {
                    // 【弹道导弹逻辑】：依靠刚体物理抛射
                    // 【弹道导弹逻辑】：无视质量的绝对抛射
                    Rigidbody rb = threat.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 targetDir = (Vector3.zero - launchPoint.position).normalized;
                        targetDir.y = 0;

                        // 赋予绝对的初速度：向前 120m/s，向上 180m/s (形成完美的抛物线)
                        Vector3 launchVelocity = targetDir * 30f + Vector3.up * 40f;

                        // 【关键修复】：使用 VelocityChange 无视 mass 参数
                        rb.AddForce(launchVelocity, ForceMode.VelocityChange);

                        Debug.Log($"[警报] 敌方发射了战术弹道导弹 {telemetry.flightId}！");
                    }
                }
                else
                {
                    // 【无人机逻辑】：依靠自带引擎平飞 (你之前写的 TargetFlight 脚本)
                    TargetFlight flightEngine = threat.GetComponent<TargetFlight>();
                    if (flightEngine != null)
                    {
                        flightEngine.enabled = true; // 激活它的平飞引擎
                    }
                }
            }

            yield return new WaitForSeconds(launchInterval);
        }
    }
}