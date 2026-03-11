using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallisticMissile : MonoBehaviour
{
    [Header("🎯 弹道打击参数")]
    [Tooltip("目标位置，默认打向世界原点(0,0,0)防空阵地")]
    public Vector3 targetPosition = Vector3.zero;

    [Tooltip("预计到达目标的时间(秒)。时间越短速度越快，抛物线越低")]
    public float flightTime = 12f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 1. 强制开启重力，这是抛物线的灵魂！
        rb.useGravity = true;

        // 2. 战术结算：根据运动学公式，计算出精准命中目标所需的初速度向量
        Vector3 initialVelocity = CalculateLaunchVelocity(transform.position, targetPosition, flightTime);

        // 3. 点火起飞！瞬间赋予导弹这个初速度
        rb.velocity = initialVelocity;
    }

    void Update()
    {
        // 4. 视觉优化：让导弹的机头（Z轴）始终顺着它的速度方向（抛物线切线）
        // 这样导弹在上升时机头朝天，下坠时机头朝地，极具压迫感！
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }

        // 5. 越界销毁兜底
        if (transform.position.y < -100f) Destroy(gameObject);
    }

    // 📐 核心物理引擎：通过位移反推初速度
    Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 end, float time)
    {
        Vector3 displacement = end - start;
        Vector3 gravity = Physics.gravity; // Unity 默认重力是 (0, -9.81, 0)

        // 物理公式：S = V0*t + 0.5*g*t^2  =>  V0 = (S - 0.5*g*t^2) / t
        return (displacement - 0.5f * gravity * time * time) / time;
    }
}