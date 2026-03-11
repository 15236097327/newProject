using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[Serializable]
public class CommandData
{
    public string cmd;
    public string target_id;
}

public class WeaponController : MonoBehaviour
{
    [Header("网络配置")]
    public int listenPort = 8081;
    private UdpClient udpServer;
    private ConcurrentQueue<CommandData> commandQueue = new ConcurrentQueue<CommandData>();

    [Header("防空阵地武库配置")]
    public GameObject missilePrefab;
    public Transform[] firePoints;
    private int currentSiloIndex = 0;

    [Header("波次与弹药系统")]
    public float launchInterval = 0.5f;
    private float fireTimer = 0f;
    private Queue<string> fireQueue = new Queue<string>();

    public int maxAmmo = 4;
    public int currentAmmo;
    public float reloadTime = 10f;
    private float reloadTimer = 0f;
    private bool isReloading = false;

    [Header("阵地状态广播")]
    public string wpfIP = "127.0.0.1";
    public int wpfPort = 8080;
    private UdpClient statusSender;

    [Header("基地装甲系统")]
    public int maxBaseHP = 10000;   // 基地总装甲值
    public int currentBaseHP;       // 当前健康度


    [Header("电子战受创状态")]
    public bool isEMPBlinded = false;
    private float empTimer = 0f;

    [Header("后勤补给系统")]
    public AmmoTruckAI assignedAmmoTruck; // 专属弹药车
    private float estimatedReloadTime = 0f; // 仅用于 WPF UI 显示的预估时间


    [Header("蓝军主动电子战系统")]
    public float ewJammingRadius = 600f; // 电磁波覆盖半径
    public ParticleSystem empWaveEffect; // 如果你有环形冲击波特效可以拖进来
    void Start()
    {
        udpServer = new UdpClient(listenPort);
        udpServer.BeginReceive(ReceiveCallback, null);
        Debug.Log($"[武器系统] C-RAM 阵地已上线，正在监听火控端口: {listenPort}");
        currentBaseHP = maxBaseHP; // 开局满血
        currentAmmo = maxAmmo;
        statusSender = new UdpClient();
        InvokeRepeating("SendBaseStatusToWPF", 0f, 0.5f);
    }

    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedBytes = udpServer.EndReceive(ar, ref remoteEndPoint);
            string jsonString = Encoding.UTF8.GetString(receivedBytes);

            CommandData cmdData = JsonUtility.FromJson<CommandData>(jsonString);
            if (cmdData != null && cmdData.cmd == "FIRE")
            {
                commandQueue.Enqueue(cmdData);
            }
            udpServer.BeginReceive(ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"指令接收异常: {e.Message}");
        }
    }

    void Update()
    {
        // 1. 把网络线程收到的开火指令，塞进基地的待发射队列
        while (commandQueue.TryDequeue(out CommandData cmd))
        {
            // ====== 【指令路由中心】 ======
            if (cmd.cmd == "FIRE")
            {
                EngageTarget(cmd.target_id);
            }
            else if (cmd.cmd == "EW_JAM")
            {
                ExecuteEWJamming(); // 触发广域电磁干扰
            }
        }

        // 2. 核心物理：装填与发射逻辑
        if (isReloading)
        {
            // 在途装填中：计算车子距离发射架的时间，反馈给 WPF UI
            if (assignedAmmoTruck != null && assignedAmmoTruck.gameObject.activeInHierarchy)
            {
                // 粗略预估剩余时间 = (距离 / 车速) + 吊装时间
                float dist = Vector3.Distance(assignedAmmoTruck.transform.position, transform.position);
                estimatedReloadTime = (dist / 10f) + 5f;
            }
            else
            {
                // 车没了（被炸了），时间直接卡死在 999
                estimatedReloadTime = 999f;
            }
        }
        else
        {
            // A. 检查弹药是否耗尽
            if (currentAmmo <= 0)
            {
                Debug.LogWarning("!!! [阵地警报] 弹药耗尽！呼叫后勤车队 !!!");
                fireQueue.Clear();
                isReloading = true;

                // 呼叫物理弹药车
                if (assignedAmmoTruck != null)
                {
                    assignedAmmoTruck.DispatchToReload(this);
                }
                else
                {
                    Debug.LogError("[致命错误] 未分配弹药车或弹药车已损毁，无法补给！");
                }
                return;
            }

            // B. 处理 EMP 电子战状态
            if (empTimer > 0)
            {
                empTimer -= Time.deltaTime;
                isEMPBlinded = true;
            }
            else
            {
                isEMPBlinded = false;
            }

            // C. ====== 【你刚才不小心删掉的开火逻辑，我补回来了】 ======
            if (fireTimer > 0)
            {
                fireTimer -= Time.deltaTime;
            }

            // 如果冷却完毕，且队列里有目标，就物理发射导弹！
            if (fireTimer <= 0 && fireQueue.Count > 0)
            {
                string targetId = fireQueue.Dequeue();
                ExecutePhysicalLaunch(targetId);
                fireTimer = launchInterval; // 重置发射间隔
            }
            // ==========================================================
        }
    }
    private void ExecuteEWJamming()
    {
        Debug.LogWarning("!!! [防空阵地] 启动广域电磁压制 (EMP) !!!");
        if (empWaveEffect != null) empWaveEffect.Play();

        // 获取全图所有敌人
        GameObject[] threats = GameObject.FindGameObjectsWithTag("Enemy");
        int jamCount = 0;

        foreach (var threat in threats)
        {
            // 检查是否在电磁覆盖半径内
            if (Vector3.Distance(transform.position, threat.transform.position) <= ewJammingRadius)
            {
                // 实战逻辑：EMP 对重装甲的战术弹道导弹 (TBM) 无效，只对无人机 (UAV) 或诱饵弹有效
                if (threat.name.Contains("UAV") || threat.name.Contains("FAKE"))
                {
                    // A. 物理断电：切断平飞引擎
                    TargetFlight flightEng = threat.GetComponent<TargetFlight>();
                    if (flightEng != null) flightEng.enabled = false;

                    // B. 赋予重力：让其像砖头一样砸向地面 (软杀伤)
                    Rigidbody rb = threat.GetComponent<Rigidbody>();
                    if (rb == null) rb = threat.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.mass = 1000f;

                    // C. 摧毁信号：让其从 WPF 雷达上瞬间消失！
                    UdpTelemetrySender telemetry = threat.GetComponent<UdpTelemetrySender>();
                    if (telemetry != null) telemetry.enabled = false;

                    jamCount++;
                    Debug.Log($"[电子战] 目标 {threat.name} 导航模块被烧毁，正在坠落！");
                }
            }
        }
        Debug.Log($"[电子战战报] 成功瘫痪了 {jamCount} 架敌方无人机！");
    }
    private void EngageTarget(string targetId)
    {
        if (isReloading) return;
        if (!fireQueue.Contains(targetId)) fireQueue.Enqueue(targetId);
    }

    private void ExecutePhysicalLaunch(string targetId)
    {
        GameObject targetObj = GameObject.Find(targetId);
        if (targetObj == null) return;

        if (missilePrefab != null && firePoints != null && firePoints.Length > 0)
        {
            Transform activeSilo = firePoints[currentSiloIndex];
            GameObject missile = Instantiate(missilePrefab, activeSilo.position, Quaternion.LookRotation(Vector3.up));
            MissileBehavior tracker = missile.GetComponent<MissileBehavior>();
            if (tracker != null) tracker.SetTarget(targetId);

            currentSiloIndex = (currentSiloIndex + 1) % firePoints.Length;
            currentAmmo--;
        }
    }

    void SendBaseStatusToWPF()
    {
        // 在原来的 JSON 里加上 HP 和 MaxHP
        //string json = $"{{\"Type\":\"BaseStatus\",\"Ammo\":{currentAmmo},\"MaxAmmo\":{maxAmmo},\"IsReloading\":{isReloading.ToString().ToLower()},\"ReloadTimeLeft\":{reloadTimer},\"HP\":{currentBaseHP},\"MaxHP\":{maxBaseHP}}}";
        //string json = $"{{\"Type\":\"BaseStatus\",\"Ammo\":{currentAmmo},\"MaxAmmo\":{maxAmmo},\"IsReloading\":{isReloading.ToString().ToLower()},\"ReloadTimeLeft\":{reloadTimer},\"HP\":{currentBaseHP},\"MaxHP\":{maxBaseHP},\"IsEMP\":{isEMPBlinded.ToString().ToLower()}}}";

        // 把 ReloadTimeLeft 的值改成 estimatedReloadTime
        string json = $"{{\"Type\":\"BaseStatus\",\"Ammo\":{currentAmmo},\"MaxAmmo\":{maxAmmo},\"IsReloading\":{isReloading.ToString().ToLower()},\"ReloadTimeLeft\":{estimatedReloadTime},\"HP\":{currentBaseHP},\"MaxHP\":{maxBaseHP},\"IsEMP\":{isEMPBlinded.ToString().ToLower()}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        try { statusSender.Send(bytes, bytes.Length, wpfIP, wpfPort); }
        catch { }
    }
    public void TakeDamage(int damage,bool isEMP=false)
    {
        currentBaseHP -= damage;
        if (currentBaseHP < 0) currentBaseHP = 0;

        Debug.LogWarning($"[基地受损] 装甲受创！当前耐久度: {currentBaseHP} / {maxBaseHP}");
        if (isEMP)
        {
            empTimer = 5f; // 雷达系统强制离线 5 秒！
            Debug.LogError("!!! 遭遇 EMP 电磁脉冲打击！雷达系统严重故障 !!!");
        }
        else
        {
            Debug.LogWarning($"[基地受损] 装甲受创！当前耐久度: {currentBaseHP} / {maxBaseHP}");
        }
        if (currentBaseHP == 0)
        {
            Debug.LogError("!!! 基地装甲彻底击穿，设施完全瘫痪 !!!");
            // 这里以后可以加引发全图大爆炸的逻辑
        }
    }
    void OnApplicationQuit()
    {
        if (udpServer != null) udpServer.Close();
        if (statusSender != null) statusSender.Close();
    }

    // 由弹药车在物理接触后调用
    public void RefillAmmo()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
        estimatedReloadTime = 0f;
    }
}