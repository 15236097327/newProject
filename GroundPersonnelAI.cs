using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

public enum PersonnelRole { RadarOperator, Mechanic, Guard, Commander }
public enum PersonnelState { Idle, Working, TakingCover }

public class GroundPersonnelAI : MonoBehaviour
{
    [Header("人员编制与分配")]
    public PersonnelRole role;
    public Transform assignedStation;

    [Header("战术状态与动画")]
    public Animator animator;
    public float dangerRadius = 80f;
    private PersonnelState currentState = PersonnelState.Idle;

    [Header("实时动作捕捉接口 (UDP)")]
    public bool enableMocap = false;
    public int mocapListenPort = 8082;
    private UdpClient mocapReceiver;
    private bool isMocapActive = false;

    // 🚀 修复漏洞：扫描间隔，避免 FindGameObjectsWithTag 卡死主线程
    private float scanTimer = 0f;
    private float scanInterval = 0.5f;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (assignedStation != null && !enableMocap)
        {
            transform.position = assignedStation.position;
            transform.rotation = assignedStation.rotation;
        }

        if (enableMocap)
        {
            // 假设你有 StartMocapListener 方法，请保留原样
        }
    }

    void Update()
    {
        if (enableMocap && isMocapActive)
        {
            // ApplyMocapDataToBones();
            return;
        }

        // 🚀 修复漏洞：降频扫描
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            CheckForDanger();
            scanTimer = 0f;
        }

        // UpdateAnimationState(); // 请保留你的原代码
    }

    void CheckForDanger()
    {
        bool inDanger = false;
        GameObject[] threats = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var threat in threats)
        {
            if (Vector3.Distance(transform.position, threat.transform.position) < dangerRadius)
            {
                inDanger = true;
                break;
            }
        }

        if (inDanger) currentState = PersonnelState.TakingCover;
        else currentState = PersonnelState.Idle;
    }

    // 🚀 修复漏洞：彻底释放动捕 UDP 端口，防止二次启动崩溃
    void OnDestroy()
    {
        if (mocapReceiver != null)
        {
            mocapReceiver.Close();
        }
    }
}