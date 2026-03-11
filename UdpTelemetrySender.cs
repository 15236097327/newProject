using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[Serializable]
public class FlightData
{
    public string id;
    public string type;
    public float x;
    public float y;
    public float z;
    public float speed;
    public string iff_status;
}

public class UdpTelemetrySender : MonoBehaviour
{
    [Header("网络配置")]
    public string targetIP = "127.0.0.1";
    public int targetPort = 8080;
    public float updateRate = 0.1f;

    [Header("飞行器属性")]
    public string flightId = "EK380";
    public string flightType = "Civilian";
    public string iffStatus = "Valid";

    // ====== 【核心新增：电子战/隐身干扰系统 (ECM)】 ======
    [Header("电子战配置")]
    public bool hasECM = false;          // 是否携带电子干扰吊舱 (默认不带，由生成器决定)
    private bool isJamming = false;      // 当前是否处于雷达静默状态
    private float jammingTimer = 0f;     // 干扰持续时间计时器
    private float visibleTimer = 3f;     // 开局先在雷达上暴露的时间
    // ===================================================

    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private Vector3 lastPosition;

    void Start()
    {
        udpClient = new UdpClient();
        endPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
        lastPosition = transform.position;

        InvokeRepeating(nameof(SendTelemetry), 0f, updateRate);
        Debug.Log($"[系统] {flightId} 已启动 UDP 遥测广播，目标: {targetIP}:{targetPort}");
    }

    // ====== 【核心新增：ECM 轮询控制】 ======
    void Update()
    {
        if (hasECM)
        {
            if (isJamming)
            {
                // 干扰中：倒计时，准备重新暴露
                jammingTimer -= Time.deltaTime;
                if (jammingTimer <= 0)
                {
                    isJamming = false;
                    visibleTimer = UnityEngine.Random.Range(3f, 6f); // 随机暴露 3 到 6 秒
                    Debug.Log($"[ECM] {flightId} 干扰机过热，雷达信号重新暴露！");
                }
            }
            else
            {
                // 暴露中：倒计时，准备开启干扰
                visibleTimer -= Time.deltaTime;
                if (visibleTimer <= 0)
                {
                    isJamming = true;
                    jammingTimer = UnityEngine.Random.Range(1.5f, 3f); // 随机静默 1.5 到 3 秒
                    Debug.LogWarning($"[ECM] {flightId} 开启电子干扰，从敌方雷达上消失！");
                }
            }
        }
    }
    // =====================================

    void SendTelemetry()
    {
        // ====== 【核心修改：干扰期间直接切断信号】 ======
        if (isJamming) return; // 变成幽灵！停止向 WPF 发送坐标
        // ==============================================
        // ====== 【核心新增：地形掩蔽与雷达组网检测】 ======
        bool isVisibleToRadarNetwork = false;

        // 遍历全图所有的雷达节点
        foreach (RadarNode radar in RadarNode.AllNodes)
        {
            if (radar.CanSee(transform.position))
            {
                isVisibleToRadarNetwork = true;
                break; // 只要有一个雷达能看见，目标就会暴露！
            }
        }

        // 如果没有任何一个雷达能看见它（被山挡住了），停止发送坐标！
        if (!isVisibleToRadarNetwork)
        {
            // WPF 雷达上该目标将停止更新，残影会在 0.5 秒后消失！
            return;
        }
        float currentSpeed = Vector3.Distance(transform.position, lastPosition) / updateRate;
        lastPosition = transform.position;

        FlightData data = new FlightData
        {
            id = flightId,
            type = flightType,
            x = transform.position.x,
            y = transform.position.y,
            z = transform.position.z,
            speed = currentSpeed,
            iff_status = iffStatus
        };

        string jsonMessage = JsonUtility.ToJson(data);
        byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);

        try
        {
            udpClient.Send(bytes, bytes.Length, endPoint);
        }
        catch (Exception e)
        {
            Debug.LogError($"[网络异常] 数据发送失败: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null) udpClient.Close();
    }
}