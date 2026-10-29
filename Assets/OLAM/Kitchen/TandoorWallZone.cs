using Olam.UI;
using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Стенка тандыра: подушку с влажным тестом отпускают над тандыром — лепёшка прилипает в позе attachAnchor.
    /// Голое тесто — «горячо!», сухое — «не пристанет»: тесто возвращается на стол.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TandoorWallZone : MonoBehaviour, IKitchenTarget
    {
        [SerializeField] TandoorThermal thermal;
        [SerializeField] Transform attachAnchor;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public bool TryAccept(KitchenItem item)
        {
            if (item.ItemKind == KitchenItem.Kind.Dough)
            {
                if (item.TryGetComponent<Dough>(out var bare) && bare.State == Dough.DoughState.Free && bare.BakeProgress <= 0f)
                    OlamMissionPanel.ShowCard(KitchenText.HotTitle, KitchenText.HotBody);
                return false;
            }
            if (item.ItemKind != KitchenItem.Kind.Cushion || !item.TryGetComponent<BakerCushion>(out var cushion) ||
                cushion.Dough == null)
                return false;
            if (!cushion.Dough.IsWet)
            {
                OlamMissionPanel.ShowCard(KitchenText.DryTitle, KitchenText.DryBody);
                return false;
            }
            cushion.Dough.StickToWall(attachAnchor != null ? attachAnchor : transform, thermal);
            item.ReturnHome();
            return true;
        }
    }
}
