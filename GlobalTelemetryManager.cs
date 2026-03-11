using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Generic;

[System.Serializable]
public class BattlefieldData
{
    public float baseHealth;
    public int baseAmmo;
    public List<TargetData> targets = new List<TargetData>();
}

[System.Serializable]
public class TargetData
{
    public string id;
    public string type;
    public Vector3 position;
    public float health;
}

public class GlobalTelemetryManager : MonoBehaviour
{
    [Header("接线分配")]
    public RadarNode radar;             // 接入雷达中枢
    public WeaponController baseCore;   // 接入基地核心

    [Header("网络设置")]
    public string wpfIP = "127.0.0.1";
    public int wpfPort = 8080;
    public float updateInterval = 0.1f; // 10Hz 刷新率

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

    void Start()
    {
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(wpfIP), wpfPort);
        InvokeRepeating(nameof(SendDataToWPF), 0, updateInterval);
    }

    void SendDataToWPF()
    {
        if (radar == null || baseCore == null) return;

        // 1. 构建战术数据集
        BattlefieldData data = new BattlefieldData();
        data.baseHealth = baseCore.currentHealth;
        data.baseAmmo = baseCore.currentAmmo;

        // 2. 提取雷达扫描到的所有实时目标
        foreach (var threat in radar.GetAllThreats())
        {
            if (threat == null) continue;
            data.targets.Add(new TargetData
            {
                id = threat.gameObject.name,
                type = threat.GetType().Name, // 自动获取是轰炸机还是无人机
                position = threat.transform.position,
                health = threat.health
            });
        }

        // 3. 序列化为 JSON 并通过 UDP 发送
        string json = JsonUtility.ToJson(data);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        udpClient.Send(bytes, bytes.Length, remoteEndPoint);
    }

    void OnDestroy()
    {
        if (udpClient != null) udpClient.Close();
    }
}