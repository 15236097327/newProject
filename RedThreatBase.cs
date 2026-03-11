using UnityEngine;

// 抽象类 (abstract)：它本身不能被直接挂在物体上，只能被继承
[RequireComponent(typeof(Rigidbody))]
public abstract class RedThreatBase : MonoBehaviour
{
    [Header("🔴 基础作战参数 (父类统管)")]
    public float speed = 50f;
    public float health = 100f;
    public int impactDamage = 1000; // 撞击基地造成的伤害

    [Header("💥 销毁特效")]
    public GameObject explosionEffectPrefab;

    protected Transform targetBase; // protected: 只有自己和儿子能访问

    protected virtual void Start()
    {
        // 🚀 拔刺：消灭硬编码！自动在全图寻找蓝军防空中枢，不需要手动拖拽了
        WeaponController blueBase = FindObjectOfType<WeaponController>();
        if (blueBase != null)
        {
            targetBase = blueBase.transform;
        }
        else
        {
            Debug.LogWarning($"[导航失效] {gameObject.name} 找不到蓝军主基地！");
        }
    }

    protected virtual void Update()
    {
        // 核心运转中枢：具体怎么飞，交给儿子们自己去实现
        ExecuteFlightPlan();
    }

    // 抽象方法：强制所有继承它的子类，必须默写出自己的飞行路线！
    protected abstract void ExecuteFlightPlan();

    // 🚀 拔刺：统一的全军伤害结算系统
    public virtual void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // 1. 生成爆炸火球
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. 🚀 修复报错：直接调用静态类发送 UDP 战报！不需要去场景里 Find！
        CombatReporter.SendKillReport(gameObject.name);

        // 3. 彻底从内存抹除
        Destroy(gameObject);
    }
}