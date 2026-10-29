namespace Olam.Kitchen
{
    /// <summary>
    /// Куда можно отпустить предмет кухни: топка, миска, стенка тандыра, подушка, тесто (для чекича), дастархан.
    /// </summary>
    public interface IKitchenTarget
    {
        /// <summary>true — предмет принят и уже переложен; false — пусть ищет дальше по лучу.</summary>
        bool TryAccept(KitchenItem item);
    }
}
