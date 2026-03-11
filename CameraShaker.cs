using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    // 单例模式：让全地图的炸弹都能随时找到主摄像机
    public static CameraShaker Instance;

    private Vector3 originalPos;
    private float currentShakeDuration = 0f;
    private float currentShakeMagnitude = 0f;
    private float dampingSpeed = 1.0f; // 震动衰减速度

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnEnable()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (currentShakeDuration > 0)
        {
            // 核心物理运算：在一个球形空间内随机取点，乘以震动幅度，制造狂暴的摇晃感
            transform.localPosition = originalPos + Random.insideUnitSphere * currentShakeMagnitude;

            // 随着时间推移，震动慢慢减弱停息
            currentShakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            currentShakeDuration = 0f;
            transform.localPosition = originalPos; // 震动结束，机位归位
        }
    }

    // 暴露给外部的“起爆触发器”接口
    public void Shake(float duration, float magnitude)
    {
        originalPos = transform.localPosition; // 记录当前位置
        currentShakeDuration = duration;
        currentShakeMagnitude = magnitude;
    }
}