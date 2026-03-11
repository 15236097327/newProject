using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DamageControlUnit : MonoBehaviour
{
    public NavMeshAgent agent;
    public ParticleSystem waterOrWeldingEffect;
    public int healAmount = 1000;

    // 🚀 修复漏洞：使用可配置的坐标节点，替代 (100,0,100) 硬编码
    [Header("待命车库")]
    public Transform garageStation;

    private WeaponController baseCmd;
    private bool isWorking = false;

    void Start()
    {
        baseCmd = FindObjectOfType<WeaponController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    public void DispatchToCrater(Vector3 craterPosition)
    {
        if (!isWorking)
        {
            Debug.Log("[后勤管制] 消防/工程组出动，正在前往受损区域！");
            agent.SetDestination(craterPosition);
            StartCoroutine(CheckArrivalAndHeal());
        }
    }

    IEnumerator CheckArrivalAndHeal()
    {
        isWorking = true;

        // 🚀 修复漏洞：防死锁机制
        while (agent.pathPending || agent.remainingDistance > 5f)
        {
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogError("[后勤管制] 道路失效，抢修车无法抵达弹坑！");
                isWorking = false;
                yield break;
            }
            yield return null;
        }

        agent.isStopped = true;
        if (waterOrWeldingEffect != null) waterOrWeldingEffect.Play();

        yield return new WaitForSeconds(5f);

        if (waterOrWeldingEffect != null) waterOrWeldingEffect.Stop();

        if (baseCmd != null)
        {
            baseCmd.TakeDamage(-healAmount);
            Debug.Log("[后勤管制] 跑道抢修完毕，系统已恢复部分耐久！");
        }

        agent.isStopped = false;

        // 🚀 修复漏洞：动态返回车库
        if (garageStation != null)
        {
            agent.SetDestination(garageStation.position);
        }
        else
        {
            agent.SetDestination(transform.position); // 若没设车库，原地待命
        }

        isWorking = false;
    }
}