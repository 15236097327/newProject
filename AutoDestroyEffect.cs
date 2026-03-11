using UnityEngine;
public class AutoDestroyEffect : MonoBehaviour
{
    public float duration = 2.0f; // 特效持续多久后清理自己
    void Start() { Destroy(gameObject, duration); }
}