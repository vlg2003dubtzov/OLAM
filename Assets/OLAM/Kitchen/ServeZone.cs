using Olam.UI;
using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Дастархан: румяную лепёшку отпускают над ним — она ложится на стопку лицом вверх.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ServeZone : MonoBehaviour, IKitchenTarget
    {
        [SerializeField] Transform seat;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public bool TryAccept(KitchenItem item)
        {
            if (item.ItemKind != KitchenItem.Kind.Dough || !item.TryGetComponent<Dough>(out var bread)) return false;
            if (!bread.Golden || bread.State != Dough.DoughState.Free)
            {
                if (bread.BakeProgress <= 0f) OlamMissionPanel.ShowCard(KitchenText.RawServeTitle, KitchenText.RawServeBody);
                return false;
            }
            bread.Serve(seat != null ? seat : transform);
            OlamMissionPanel.ShowCard(KitchenText.ServedTitle, KitchenText.ServedBody);
            return true;
        }
    }
}
