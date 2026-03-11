using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum PersonnelClass { Loader, Firefighter, Medic, Driver }
public enum PersonState { Idle, MovingToJob, Working, TakingCover, Injured, BeingHealed } // 🚀 新增被救治状态

[RequireComponent(typeof(NavMeshAgent))]
public class SpecializedPersonnelAI : MonoBehaviour
{
    [Header("人员身份卡")]
    public PersonnelClass jobClass;
    public Animator animator;
    public Transform standbyStation;

    [Header("通用战术参数")]
    public float dangerRadius = 80f;
    public PersonState currentState = PersonState.Idle;

    private NavMeshAgent agent;
    private WeaponController weaponSystem;

    [Header("医护专属")]
    public float healTime = 3f;

    // 🚀 修复漏洞：扫描降频
    private float scanTimer = 0f;
    private float scanInterval = 0.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        weaponSystem = FindObjectOfType<WeaponController>();

        if (standbyStation != null) agent.SetDestination(standbyStation.position);
    }

    void Update()
    {
        if (currentState == PersonState.Injured || currentState == PersonState.BeingHealed) return;

        // 🚀 降频扫描
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            CheckForDanger();
            scanTimer = 0f;
        }

        if (currentState == PersonState.TakingCover) return;

        switch (jobClass)
        {
            case PersonnelClass.Loader:
                HandleLoaderLogic(); break;
            case PersonnelClass.Firefighter:
                HandleFirefighterLogic(); break;
            case PersonnelClass.Medic:
                HandleMedicLogic(); break;
            case PersonnelClass.Driver:
                HandleDriverLogic(); break;
        }

        UpdateAnimations();
    }

    void HandleLoaderLogic()
    {
        if (weaponSystem != null && weaponSystem.currentAmmo <= 0)
        {
            float dist = Vector3.Distance(transform.position, weaponSystem.transform.position);
            if (dist > 5f && currentState != PersonState.MovingToJob)
            {
                currentState = PersonState.MovingToJob;
                agent.SetDestination(weaponSystem.transform.position);
            }
            else if (dist <= 5f)
            {
                currentState = PersonState.Working;
                agent.isStopped = true;
            }
        }
        else ReturnToStandby();
    }

    void HandleFirefighterLogic()
    {
        GameObject[] fires = GameObject.FindGameObjectsWithTag("Fire");
        if (fires.Length > 0)
        {
            Transform targetFire = fires[0].transform;
            float dist = Vector3.Distance(transform.position, targetFire.position);

            if (dist > 8f && currentState != PersonState.MovingToJob)
            {
                currentState = PersonState.MovingToJob;
                agent.SetDestination(targetFire.position);
            }
            else if (dist <= 8f)
            {
                currentState = PersonState.Working;
                transform.LookAt(targetFire);
                agent.isStopped = true;
            }
        }
        else ReturnToStandby();
    }

    void HandleMedicLogic()
    {
        if (currentState == PersonState.Working) return;

        // 仅在执行扫描的这一帧去寻找伤员
        if (scanTimer == 0f)
        {
            SpecializedPersonnelAI[] allPersonnel = FindObjectsOfType<SpecializedPersonnelAI>();
            SpecializedPersonnelAI patient = null;

            foreach (var p in allPersonnel)
            {
                // 🚀 修复漏洞：避免多个医疗兵去抢救同一个伤员
                if (p.currentState == PersonState.Injured && p != this)
                {
                    patient = p;
                    patient.currentState = PersonState.BeingHealed; // 锁定目标，别人就不会来了
                    break;
                }
            }

            if (patient != null)
            {
                float dist = Vector3.Distance(transform.position, patient.transform.position);
                if (dist > 3f)
                {
                    currentState = PersonState.MovingToJob;
                    agent.SetDestination(patient.transform.position);
                }
                else
                {
                    currentState = PersonState.Working;
                    StartCoroutine(PerformCPR(patient));
                }
            }
            else ReturnToStandby();
        }
    }

    IEnumerator PerformCPR(SpecializedPersonnelAI patient)
    {
        agent.isStopped = true;
        transform.LookAt(patient.transform);

        Debug.Log("[医疗组] 正在进行前线创伤抢救...");
        yield return new WaitForSeconds(healTime);

        if (patient != null) patient.currentState = PersonState.Idle;
        currentState = PersonState.Idle;
        agent.isStopped = false;
    }

    void HandleDriverLogic()
    {
        if (agent != null) agent.enabled = false;
        NavMeshAgent vehicleAgent = GetComponentInParent<NavMeshAgent>();
        if (vehicleAgent != null)
        {
            currentState = vehicleAgent.velocity.magnitude > 0.1f ? PersonState.Working : PersonState.Idle;
        }
    }

    void ReturnToStandby()
    {
        if (standbyStation != null && Vector3.Distance(transform.position, standbyStation.position) > 2f)
        {
            currentState = PersonState.Idle;
            agent.isStopped = false;
            agent.SetDestination(standbyStation.position);
        }
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

        if (inDanger && currentState != PersonState.TakingCover)
        {
            if (Random.value > 0.7f)
            {
                currentState = PersonState.Injured;
                Debug.LogWarning($"[战损] 人员被爆炸波及受伤倒地，呼叫医护人员！");
            }
            else currentState = PersonState.TakingCover;
            if (agent.isActiveAndEnabled) agent.isStopped = true;
        }
        else if (!inDanger && currentState == PersonState.TakingCover)
        {
            currentState = PersonState.Idle;
            if (agent.isActiveAndEnabled) agent.isStopped = false;
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", agent.isActiveAndEnabled ? agent.velocity.magnitude : 0f);
        animator.SetBool("IsWorking", currentState == PersonState.Working);
        animator.SetBool("IsTakingCover", currentState == PersonState.TakingCover);
        animator.SetBool("IsInjured", currentState == PersonState.Injured || currentState == PersonState.BeingHealed);
    }
}