using UnityEngine;

public class MissileBehavior : MonoBehaviour
{
    [Header("导弹机动性能")]
    public float speed = 150f;
    public float turnSpeed = 10f;
    public float explosionRadius = 15f;

    // 🚀 核心新增：战斗部破坏力
    [Header("战斗部与特效")]
    public float damage = 100f; // 默认造成100点伤害
    public GameObject explosionPrefab;
    public GameObject debrisPrefab;

    [Header("击杀视角")]
    public Camera killCam;

    private Transform target;
    private Vector3 lastTargetPos;

    public void SetTarget(string targetId)
    {
        GameObject t = GameObject.Find(targetId);
        if (t != null)
        {
            target = t.transform;
            lastTargetPos = target.position;
        }
        else
        {
            Debug.LogWarning($"[制导系统] 目标 {targetId} 已丢失或不存在，导弹自毁！");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        MissileBehavior[] allMissiles = FindObjectsOfType<MissileBehavior>();
        foreach (var m in allMissiles)
        {
            if (m != this && m.killCam != null) m.killCam.enabled = false;
        }
        if (killCam != null) killCam.enabled = true;
    }

    void Update()
    {
        // 1. 目标丢失兜底
        if (target == null)
        {
            Explode(false); // 失去目标，仅自爆，不触发伤害结算
            return;
        }

        // 2. 空间前置预判制导算法 (Lead Collision)
        Vector3 targetVelocity = Vector3.zero;
        if (Time.deltaTime > 0)
        {
            targetVelocity = (target.position - lastTargetPos) / Time.deltaTime;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float timeToIntercept = distanceToTarget / speed;
        Vector3 predictedPosition = target.position + targetVelocity * timeToIntercept;

        Vector3 direction = (predictedPosition - transform.position).normalized;
        if (direction != Vector3.zero) // 防止除零警告
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        lastTargetPos = target.position;

        // 3. 近炸引信 (Proximity Fuze)
        if (distanceToTarget < explosionRadius)
        {
            Explode(true); // 命中目标！触发起爆与伤害结算
        }
    }

    // 💥 引信引爆与伤害递交核心
    void Explode(bool hitTarget)
    {
        // 1. 悬停观瞄脱离
        if (killCam != null)
        {
            killCam.transform.SetParent(null);
            killCam.enabled = true;
            Destroy(killCam.gameObject, 2.0f);
        }

        // 2. 生成自身爆炸特效
        if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, transform.rotation);

        // 3. 漫天火雨碎片
        if (debrisPrefab != null)
        {
            for (int i = 0; i < 6; i++)
            {
                GameObject debris = Instantiate(debrisPrefab, transform.position, Random.rotation);
                Rigidbody rb = debris.GetComponent<Rigidbody>();
                if (rb != null) rb.AddExplosionForce(1000f, transform.position, 20f);
                Destroy(debris, 4f);
            }
        }

        // 4. 🚀 核心多态融合：向红军基类递交伤害，而不是直接 Destroy 目标！
        if (hitTarget && target != null)
        {
            RedThreatBase enemy = target.GetComponent<RedThreatBase>();
            if (enemy != null)
            {
                // 触发红军的统一扣血接口！如果血量清零，红军会自己调用 Die() 去销毁和发战报。
                enemy.TakeDamage(damage);
                Debug.Log($"[防空阵地] 导弹破片击中目标，造成 {damage} 点伤害！");
            }
            else
            {
                // 兜底：如果打中的是还没来得及改造的老模型，直接强制销毁
                Destroy(target.gameObject);
            }
        }

        // 注意：这里彻底删除了 CombatReporter.SendKillReport！发战报的职责已经移交给敌机自己了！

        // 5. 导弹自身殉爆销毁
        Destroy(gameObject);
    }
}