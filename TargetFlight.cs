using UnityEngine;

// 🚀 核心变化：不再继承 MonoBehaviour，而是继承 RedThreatBase！
public class TargetFlight : RedThreatBase
{
    // 你会发现，这里不需要再声明 speed, health 和 targetBase 了！
    // 因为父类已经帮它准备好了，它生来就有这些属性。

    [Header("🛸 直线突防专属设置")]
    public bool lockYAxis = false; // 巡航导弹掠海飞行专属
    public float fixedAltitude = 15f;

    // 强制重写 (override) 父类布置的“飞行作业”
    protected override void ExecuteFlightPlan()
    {
        if (targetBase == null) return;

        // 1. 确定目标点 (如果锁定了高度，就强行把目标的 Y 轴改成固定的)
        Vector3 destination = targetBase.position;
        if (lockYAxis)
        {
            destination = new Vector3(destination.x, fixedAltitude, destination.z);
            // 自己也强行维持高度
            transform.position = new Vector3(transform.position.x, fixedAltitude, transform.position.z);
        }

        // 2. 直线突防位移计算
        Vector3 direction = (destination - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 3. 永远把机头对准目标方向
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}