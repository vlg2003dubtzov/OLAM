using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Поленница: сгоревшее полено через пару секунд сменяется новым на том же месте — печь всегда можно подтопить.
    /// </summary>
    public class FirewoodSupply : MonoBehaviour
    {
        [SerializeField] GameObject template;   // выключенный образец полена
        [SerializeField] GameObject[] logs;     // поленья на своих местах
        [SerializeField] float delay = 2f;

        Pose[] slots;
        float[] timers;

        void Awake()
        {
            slots = new Pose[logs.Length];
            timers = new float[logs.Length];
            for (int i = 0; i < logs.Length; i++)
                slots[i] = logs[i] != null ? new Pose(logs[i].transform.position, logs[i].transform.rotation) : default;
        }

        void Update()
        {
            if (template == null) return;
            for (int i = 0; i < logs.Length; i++)
            {
                if (logs[i] != null) continue;
                timers[i] += Time.deltaTime;
                if (timers[i] < delay) continue;
                timers[i] = 0f;
                logs[i] = Instantiate(template, slots[i].position, slots[i].rotation, transform);
                logs[i].SetActive(true);
            }
        }
    }
}
