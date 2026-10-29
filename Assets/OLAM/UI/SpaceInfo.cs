using Olam.Core;
using UnityEngine;

namespace Olam.UI
{
    /// <summary>
    /// Пространство OLAM в меню: сцена, обложка-плашка, название, подзаголовок и вопрос урока.
    /// Обложка показывается в арке хаба и на экране загрузки.
    /// </summary>
    [CreateAssetMenu(menuName = "OLAM/Space Info", fileName = "Space_")]
    public class SpaceInfo : ScriptableObject
    {
        public string sceneName;
        public Texture2D cover;
        [Tooltip("Что держать в кадре при обрезке, доли обложки: x — в узкой арке, y — на широком экране")]
        public Vector2 focus = new(0.5f, 0.5f);
        public LText title;
        public LText kicker;
        public LText question;
    }
}
