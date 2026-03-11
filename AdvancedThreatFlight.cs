using UnityEngine;

public enum ThreatType { HypersonicGlide, CruiseMissile, StealthBomber }

// 🚀 核心变化一：不再继承 MonoBehaviour，而是继承 RedThreatBase！
public class AdvancedThreatFlight : RedThreatBase
{
    public ThreatType flightProfile;

    // 🚀 核心变化二：删除了 public float speed，因为父类已经自带了，直接继承使用！

    [Header("轰炸机专用参数")]
    public float dropDistance = 400f; // 距离基地多远投弹
    public GameObject payloadPrefab;  // 投掷的炸弹或分裂的子弹药
    private bool payloadDropped = false;

    private float flightTime = 0f;

    // 🚀 核心变化三：重写父类下达的飞行任务，代替原先的 Update()
    protected override void ExecuteFlightPlan()
    {
        // 如果父类没有找到基地（比如基地被彻底摧毁了），就停止飞行逻辑
        if (targetBase == null) return;

        flightTime += Time.deltaTime;
        Vector3 forwardMove = transform.forward * speed * Time.deltaTime;

        switch (flightProfile)
        {
            case ThreatType.HypersonicGlide:
                // 高超音速“打水漂”：做狂暴的 S 型正弦机动
                float drift = Mathf.Sin(flightTime * 5f) * 100f;
                transform.position += forwardMove + transform.right * drift * Time.deltaTime;
                break;

            case ThreatType.CruiseMissile:
                // 地形匹配巡航：贴地 15 米飞行 (此处用强行锁定高度模拟)
                Vector3 newPos = transform.position + forwardMove;
                newPos.y = 15f;
                transform.position = newPos;
                break;

            case ThreatType.StealthBomber:
                // 轰炸机平飞突防
                transform.position += forwardMove;

                // 🚀 核心变化四（拔刺）：消灭硬编码！用父类找到的 targetBase 代替 Vector3.zero
                float distToBase = Vector3.Distance(transform.position, new Vector3(targetBase.position.x, transform.position.y, targetBase.position.z));

                // 到达投弹点且未投弹
                if (distToBase < dropDistance && !payloadDropped)
                {
                    Debug.LogWarning($"[空袭警报] 隐身轰炸机 {gameObject.name} 正在对主基地投弹！");
                    if (payloadPrefab != null)
                    {
                        Instantiate(payloadPrefab, transform.position - Vector3.up * 5f, Quaternion.identity);
                    }
                    payloadDropped = true;
                    // 投弹后立刻 180 度掉头逃跑
                    transform.Rotate(0, 180, 0);
                }
                break;
        }

        // 🚀 核心变化五：如果逃跑成功（远离目标 3000 米以上），安全撤出战场并销毁
        if (Vector3.Distance(transform.position, targetBase.position) > 3000f)
        {
            Destroy(gameObject);
        }
    }
}