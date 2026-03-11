using UnityEngine;

public class LaserWeaponHELMD : MonoBehaviour
{
    [Header("激光武器参数")]
    public float engageRange = 250f;     // 射程
    public float timeToMelt = 1.5f;      // 熔毁一个目标所需时间
    public float maxHeat = 100f;         // 最大热容
    public float heatPerSecond = 30f;    // 每秒发热量
    public float coolingRate = 15f;      // 每秒散热量

    [Header("组件")]
    public Transform turretHead;         // 炮塔头部
    public Transform firePoint;          // 激光发射点
    public LineRenderer laserBeam;       // 激光射线特效

    private Transform currentTarget;
    private float currentMeltTime = 0f;
    private float currentHeat = 0f;
    private bool isOverheated = false;

    void Update()
    {
        // 散热逻辑
        if (currentHeat > 0 && currentTarget == null)
            currentHeat -= coolingRate * Time.deltaTime;

        if (currentHeat >= maxHeat) isOverheated = true;
        if (isOverheated && currentHeat <= 0) isOverheated = false; // 完全冷却后重启

        FindTarget();

        if (currentTarget != null && !isOverheated)
        {
            turretHead.LookAt(currentTarget);
            FireLaser();
        }
        else
        {
            laserBeam.enabled = false;
            currentMeltTime = 0f;
        }
    }

    void FindTarget()
    {
        GameObject[] threats = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDist = engageRange;
        Transform bestTarget = null;
        foreach (var threat in threats)
        {
            float dist = Vector3.Distance(transform.position, threat.transform.position);
            // 优先打近处的
            if (dist < shortestDist)
            {
                shortestDist = dist;
                bestTarget = threat.transform;
            }
        }
        currentTarget = bestTarget;
    }

    void FireLaser()
    {
        laserBeam.enabled = true;
        laserBeam.SetPosition(0, firePoint.position);
        laserBeam.SetPosition(1, currentTarget.position);

        currentHeat += heatPerSecond * Time.deltaTime;
        currentMeltTime += Time.deltaTime;

        if (currentMeltTime >= timeToMelt)
        {
            Debug.Log($"[激光防空] 目标 {currentTarget.name} 已被高能激光熔毁！");
            Destroy(currentTarget.gameObject);
            currentTarget = null;
            currentMeltTime = 0f;
        }
    }
}