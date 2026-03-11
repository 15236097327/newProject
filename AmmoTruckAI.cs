using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class AmmoTruckAI : MonoBehaviour
{
    [Header("后勤节点")]
    public Transform ammoBunker;

    [Header("补给参数")]
    public float transferTime = 5f;

    private NavMeshAgent agent;
    private bool isDeployed = false;
    private WeaponController targetLauncher;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (ammoBunker != null)
        {
            agent.Warp(ammoBunker.position);
        }
    }

    public void DispatchToReload(WeaponController launcher)
    {
        if (isDeployed) return;

        Debug.LogWarning("[后勤管制] 收到补给请求！HEMTT 弹药运输车已出动！");
        targetLauncher = launcher;
        isDeployed = true;

        agent.SetDestination(launcher.transform.position);
        StartCoroutine(SupplyProcess());
    }

    IEnumerator SupplyProcess()
    {
        // 🚀 修复漏洞：加入寻路状态判定，防止道路被毁时卡死在死循环
        while (agent.pathPending || agent.remainingDistance > 8f)
        {
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogError("[致命错误] 补给道路被彻底摧毁！弹药车无法抵达阵地！");
                isDeployed = false;
                yield break; // 立刻终止协程
            }
            yield return null;
        }

        agent.isStopped = true;
        Debug.Log("[后勤管制] 弹药车已就位，正在进行机械吊装作业...");
        yield return new WaitForSeconds(transferTime);

        if (targetLauncher != null)
        {
            targetLauncher.RefillAmmo();
            Debug.Log("[后勤管制] 补给完成！防空阵地重新上线！");
        }

        agent.isStopped = false;
        if (ammoBunker != null)
        {
            agent.SetDestination(ammoBunker.position);
        }
        isDeployed = false;
    }

    void OnDestroy()
    {
        Debug.LogError("!!! 灾难性打击 !!! 弹药运输车被摧毁，阵地后勤链彻底断裂！");
    }
}