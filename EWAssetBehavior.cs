using UnityEngine;

public enum EWType { Decoy, StandOffJammer }

public class EWAssetBehavior : MonoBehaviour
{
    public EWType ewType;
    public float speed = 60f;

    private UdpTelemetrySender telemetry;

    void Start()
    {
        telemetry = GetComponent<UdpTelemetrySender>();

        if (ewType == EWType.Decoy)
        {
            // 诱饵弹：伪装成高价值 TBM 弹道导弹骗取火力
            if (telemetry != null)
            {
                telemetry.flightType = "Missile";
                telemetry.flightId = "FAKE-TBM-" + Random.Range(100, 999);
                gameObject.name = telemetry.flightId;
            }
        }
    }

    void Update()
    {
        if (ewType == EWType.Decoy)
        {
            // 诱饵弹平飞送死
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
        else if (ewType == EWType.StandOffJammer)
        {
            // 远距离干扰机：飞到距离基地 1000 米处停下，开始转圈盘旋并释放电磁干扰
            float dist = Vector3.Distance(transform.position, Vector3.zero);
            if (dist > 1000f)
            {
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
            }
            else
            {
                transform.RotateAround(Vector3.zero, Vector3.up, 10f * Time.deltaTime);
                // 此时可以通过 telemetry 发送一种特殊状态，让 WPF 雷达开始产生雪花噪点
            }
        }
    }

    // 诱饵弹被打中时，只冒火花，不产生剧烈爆炸碎片
    public void ExplodeAsDecoy()
    {
        Debug.Log($"[火控提示] 目标 {gameObject.name} 为诱饵弹！拦截弹被浪费！");
        Destroy(gameObject);
    }
}