using UnityEngine;

public class CIWSController : MonoBehaviour
{
    [Header("全域雷达接入")]
    public RadarNode mainRadar;           // 🚀 核心新增：接入统一雷达指挥网

    [Header("火控雷达参数")]
    public float engageRange = 150f;      // 绝对防御圈半径 (米)
    public float fireRate = 0.05f;        // 判定伤害的周期 (秒)，密集阵通常极快
    public float damagePerBurst = 20f;    // 每一小轮射击造成的伤害量

    [Header("机械部件")]
    public Transform turretHead;          // 旋转的炮管/炮塔头部
    public ParticleSystem tracerBullets;  // 曳光弹粒子特效
    public AudioSource gunAudio;

    private RedThreatBase currentTarget;  // 🚀 核心重构：使用多态基类代替 Transform
    private float fireTimer = 0f;

    void Update()
    {
        // 如果雷达断开连接，系统进入瘫痪状态
        if (mainRadar == null) return;

        // 1. 🚀 拔刺：不再全图 Find，直接从雷达网索取最近威胁
        currentTarget = mainRadar.GetNearestThreat();

        if (currentTarget != null)
        {
            // 2. 距离判断 (使用多态基类的 transform)
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (distance <= engageRange)
            {
                // 3. 炮塔锁定目标 (雷达伺服系统)
                turretHead.LookAt(currentTarget.transform.position);
                EngageTarget();
            }
            else
            {
                CeaseFire();
            }
        }
        else
        {
            CeaseFire();
        }
    }

    void EngageTarget()
    {
        // 视觉与音频反馈
        if (!tracerBullets.isPlaying) tracerBullets.Play();
        if (gunAudio != null && !gunAudio.isPlaying) gunAudio.Play();

        // 4. 🚀 核心重构：持续火力照射，向目标递交伤害
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0)
        {
            // 调用红军基类的统一接口，不再暴力 Destroy
            currentTarget.TakeDamage(damagePerBurst);

            Debug.Log($"[近防炮] 金属风暴正在撕裂目标 {currentTarget.name}！其剩余生命值：{currentTarget.health}");

            fireTimer = fireRate; // 重置判定冷却
        }
    }

    void CeaseFire()
    {
        if (tracerBullets.isPlaying) tracerBullets.Stop();
        if (gunAudio != null && gunAudio.isPlaying) gunAudio.Stop();

        // 停止开火时重置计时器，确保下次目标进入瞬间即能判定伤害
        fireTimer = 0f;
    }

    // 在 Unity 编辑器里画出防御圈的红线
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engageRange);
    }
}