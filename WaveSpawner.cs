using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("背景交通配置 (民航)")]
    public GameObject civilianPrefab;

    [Header(" 敌方空军配置 (红军航空兵)")]
    public GameObject bomberPrefab;    // B-2X 隐形轰炸机
    public GameObject fighterPrefab;   // F-22X 战斗机
    public GameObject ewPlanePrefab;   // EW-18 电子战机

    [Header(" 航线参数")]
    public float spawnRadius = 2500f;  // 空军从 2.5 公里外突防 (雷达边缘)
    public float civilianInterval = 12f;     // 每 12 秒一架民航
    public float enemyAirRaidInterval = 15f; // 每 15 秒一波空袭

    private int civilianCounter = 100;
    private int enemyCounter = 300;

    void Start()
    {
        Debug.Log("[联合指挥部] 战场全空域管制系统已启动...");
        StartCoroutine(SpawnCivilianWaves());
        StartCoroutine(SpawnEnemyAirRaids());
    }

    IEnumerator SpawnCivilianWaves()
    {
        while (true)
        {
            yield return new WaitForSeconds(civilianInterval);
            SpawnAircraft(civilianPrefab, false, "CZ");
        }
    }

    //  核心新增：敌方航空兵突防逻辑
    IEnumerator SpawnEnemyAirRaids()
    {
        yield return new WaitForSeconds(10f); // 游戏开始10秒后敌方空军介入战场

        while (true)
        {
            // 战术掷骰子决定派什么机型：50%轰炸机，30%战斗机，20%电子战机
            float rand = Random.value;
            GameObject prefabToSpawn = bomberPrefab;
            string callsignPrefix = "B-2X";

            if (rand < 0.2f)
            {
                prefabToSpawn = ewPlanePrefab;
                callsignPrefix = "EW-18";
            }
            else if (rand < 0.5f)
            {
                prefabToSpawn = fighterPrefab;
                callsignPrefix = "F-22X";
            }

            if (prefabToSpawn != null)
            {
                SpawnAircraft(prefabToSpawn, true, callsignPrefix);
            }

            yield return new WaitForSeconds(enemyAirRaidInterval);
        }
    }

    void SpawnAircraft(GameObject prefab, bool isEnemy, string prefix)
    {
        if (prefab == null) return;

        // 1. 在雷达外围生成点 (敌军飞得高，民航飞得低)
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        float altitude = isEnemy ? Random.Range(600f, 1200f) : Random.Range(200f, 400f);
        Vector3 spawnPos = new Vector3(randomCircle.x, altitude, randomCircle.y);

        // 2. 计算朝向：敌机出生时，机头必须死死锁定防空基地 (Vector3.zero)！
        Vector3 targetDir = (Vector3.zero - spawnPos).normalized;
        // 民航航线随机偏转一下，假装只是路过，不全是对着基地飞
        if (!isEnemy) targetDir = Quaternion.Euler(0, Random.Range(-45f, 45f), 0) * targetDir;

        Quaternion rotation = Quaternion.LookRotation(targetDir);

        // 3. 生成实体
        GameObject aircraft = Instantiate(prefab, spawnPos, rotation);

        // 4. 配置 UDP 遥测系统与敌我识别 (IFF)
        UdpTelemetrySender telemetry = aircraft.GetComponent<UdpTelemetrySender>();
        if (telemetry != null)
        {
            if (!isEnemy)
            {
                civilianCounter++;
                telemetry.flightId = prefix + civilianCounter;
                telemetry.flightType = "Civilian";
                telemetry.iffStatus = "Valid";   // 民航绿点
                telemetry.hasECM = false;
            }
            else
            {
                enemyCounter++;
                telemetry.flightId = prefix + "-" + enemyCounter;
                telemetry.flightType = "Aircraft";
                telemetry.iffStatus = "Unknown"; // 敌军红点

                // 只有电子战机自带高强度电磁干扰
                telemetry.hasECM = (prefix == "EW-18");
            }
            aircraft.name = telemetry.flightId;
        }
    }
}