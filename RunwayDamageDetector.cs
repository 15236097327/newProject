using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class RunwayDamageDetector : MonoBehaviour
{
    [Header("指挥中心网络配置")]
    public string wpfIP = "127.0.0.1";
    public int wpfPort = 8080; // WPF 正在监听的那个接收端口

    private UdpClient udpClient;
    private IPEndPoint endPoint;

    void Start()
    {
        udpClient = new UdpClient();
        endPoint = new IPEndPoint(IPAddress.Parse(wpfIP), wpfPort);
    }

    // 核心：Unity 的物理引擎回调。任何刚体砸到跑道都会触发这里
    void OnCollisionEnter(Collision collision)
    {
        // 检查砸中跑道的是不是爆炸产生的碎片
        if (collision.gameObject.CompareTag("Debris"))
        {
            Debug.LogWarning("[系统警报] 碎片击穿跑道表面！正在向 WPF 指挥中心发送高危警报...");

            // 为了不修改 WPF 端的数据结构，我们巧妙地借用 FlightData 格式
            // 伪装成一个极其特殊的“飞行物”数据发过去
            FlightData alertData = new FlightData
            {
                id = "RUNWAY_ALERT",
                type = "CRITICAL_DAMAGE",
                x = collision.contacts[0].point.x, // 提取碎片落点的精确物理坐标
                y = 0,
                z = collision.contacts[0].point.z,
                speed = 0,
                iff_status = "DANGER"
            };

            // 序列化并发送
            string jsonMessage = JsonUtility.ToJson(alertData);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);

            try
            {
                udpClient.Send(bytes, bytes.Length, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogError("警报发送失败: " + e.Message);
            }

            // 销毁这块碎片，防止它在地上弹跳造成重复报警
            Destroy(collision.gameObject);
        }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null) udpClient.Close();
    }
}