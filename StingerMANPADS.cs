using UnityEngine;

public class StingerMANPADS : MonoBehaviour
{
    public float engageRange = 250f;
    public GameObject smallMissilePrefab;
    public Transform shoulderLaunchPoint;
    public float reloadTime = 5f; // 士兵重新装填需要 5 秒

    private float timer = 0f;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            GameObject target = FindNearestDrone();
            if (target != null)
            {
                FireStinger(target);
                timer = reloadTime;
            }
        }
    }

    GameObject FindNearestDrone()
    {
        GameObject[] threats = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var t in threats)
        {
            if (Vector3.Distance(transform.position, t.transform.position) < engageRange)
            {
                // 只打低空的无人机或巡航导弹
                if (t.transform.position.y < 150f) return t;
            }
        }
        return null;
    }

    void FireStinger(GameObject target)
    {
        Debug.Log($"[防空步兵] 发射毒刺导弹！目标锁定：{target.name}");
        GameObject missile = Instantiate(smallMissilePrefab, shoulderLaunchPoint.position, transform.rotation);
        MissileBehavior tracker = missile.GetComponent<MissileBehavior>();
        if (tracker != null) tracker.SetTarget(target.name);
    }
}