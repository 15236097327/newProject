using UnityEngine;

public class THAADController : MonoBehaviour
{
    public RadarNode mainRadar; // 拖入上面的雷达物体
    public GameObject interceptorPrefab;
    public Transform[] launchTubes; // 之前报错的那个接线点

    public float fireRate = 3.0f;
    private float nextFireTime;

    void Update()
    {
        if (mainRadar == null) return;

        // 🚀 拔刺：直接问雷达要最近的目标
        RedThreatBase target = mainRadar.GetNearestThreat();

        if (target != null && Time.time > nextFireTime)
        {
            LaunchInterceptor(target.transform);
            nextFireTime = Time.time + fireRate;
        }
    }

    void LaunchInterceptor(Transform targetTransform)
    {
        // 从轮询的发射管发射
        Transform tube = launchTubes[Random.Range(0, launchTubes.Length)];
        GameObject missile = Instantiate(interceptorPrefab, tube.position, tube.rotation);

        // 获取导弹脚本并传参
        MissileBehavior logic = missile.GetComponent<MissileBehavior>();
        if (logic != null)
        {
            // 🚀 这里直接传物体的 Name，因为我们的 MissileBehavior 是靠 Name 寻找目标的
            logic.SetTarget(targetTransform.name);
        }
    }
}