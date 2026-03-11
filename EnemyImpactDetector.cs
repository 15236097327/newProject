using UnityEngine;

public class EnemyImpactDetector : MonoBehaviour
{
    [Header("爆炸与损毁")]
    public GameObject impactExplosionPrefab; // 砸地时的爆炸特效

    private bool hasExploded = false; // 终极保险：防止一颗炸弹引发两次爆炸扣两次血

    void Update()
    {
        // 🚀 核心新增：绝对高度兜底机制！
        // 如果物理引擎穿模了，只要炸弹 Y 轴高度小于等于 0，立刻强制起爆！
        if (transform.position.y <= 0f && !hasExploded)
        {
            Debug.LogWarning($"[雷达警告] {gameObject.name} 触发近地引信，强制起爆！");
            // 强行把爆炸点设在地面(y=0)上
            Explode(new Vector3(transform.position.x, 0f, transform.position.z));
        }
    }

    // 物理引擎的碰撞触发（应对打在建筑或高于 0 的地形上）
    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        // 🚀 优化：放宽碰撞条件。不管是跑到(Runway)、地面(Ground)还是未命名(Untagged)物体，砸上去统统爆炸
        if (collision.gameObject.CompareTag("Runway") ||
            collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Untagged"))
        {
            ContactPoint contact = collision.contacts[0];
            Explode(contact.point);
        }
    }

    // 💥 独立的引爆函数
    void Explode(Vector3 explosionPoint)
    {
        hasExploded = true; // 锁定状态，引信烧毁

        // 1. 在撞击点生成巨大的砸地火球
        if (impactExplosionPrefab != null)
        {
            Instantiate(impactExplosionPrefab, explosionPoint, Quaternion.identity);
        }

        // 2. 寻找阵地大脑并扣血
        WeaponController baseSystem = FindObjectOfType<WeaponController>();
        if (baseSystem != null)
        {
            bool isEMP = gameObject.name.Contains("EMP");
            int damage = gameObject.name.Contains("TBM") ? 3000 : 1000;
            if (isEMP) damage = 500;

            baseSystem.TakeDamage(damage, isEMP);
        }

        // 3. 触发空间震荡
        if (CameraShaker.Instance != null)
        {
            float shakeIntensity = gameObject.name.Contains("TBM") ? 2.5f : 0.8f;
            float shakeTime = gameObject.name.Contains("TBM") ? 1.5f : 0.5f;

            CameraShaker.Instance.Shake(shakeTime, shakeIntensity);
        }

        // 4. 彻底摧毁弹体
        Destroy(gameObject);
    }
}