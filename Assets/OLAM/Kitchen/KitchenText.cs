using Olam.Core;

namespace Olam.Kitchen
{
    /// <summary>
    /// Тексты миссии «Первая лепёшка»: шаги, подсказки к ним, карточки урока.
    /// Узбекские строки есть только у шагов и ждут проверки носителем; где узбекского нет, показывается русский.
    /// </summary>
    public static class KitchenText
    {
        public static readonly LText MissionTitle = new("Первая лепёшка", "Birinchi non", "The first flatbread");
        public static readonly LText MissionQuestion = new(
            "Почему лепёшка держится на стенке тандыра и не падает в огонь?",
            "Nega non tandir devorida ushlanib turadi va olovga tushmaydi?",
            "Why does the flatbread stay on the tandoor wall instead of falling into the fire?");

        /// <summary>Шаги в порядке урока: id цели QuestManager, сколько раз, название, подсказка.</summary>
        public static readonly (string id, int required, LText title, LText hint)[] Steps =
        {
            ("fuel", 3, new LText("Положи дрова в топку", "Tandirga o‘tin sol", "Put firewood into the tandoor"),
                new LText("Перетащи полено к тандыру и отпусти над ним", "", "Drag a log to the tandoor and let go over it")),
            ("heat", 1, new LText("Разогрей тандыр", "Tandirni qizdir", "Heat up the tandoor"),
                new LText("Дрова прогорают, глина нагревается — следи за градусником", "",
                    "The wood burns down and the clay heats up — watch the thermometer")),
            ("stamp", 1, new LText("Наколи середину чекичем", "O‘rtasiga chekich bos", "Prick the centre with the chekich"),
                new LText("Возьми чекич и отпусти его над тестом", "", "Pick up the chekich and let go over the dough")),
            ("wet", 1, new LText("Смочи тесто водой", "Xamirni suv bilan namla", "Moisten the dough"),
                new LText("Перетащи тесто к миске с водой", "", "Drag the dough to the bowl of water")),
            ("stick", 1, new LText("Прилепи лепёшку к стенке", "Nonni devorga yopishtir", "Stick the bread to the wall"),
                new LText("Положи тесто на подушку, а подушку отпусти над тандыром", "",
                    "Put the dough on the cushion, then let go of the cushion over the tandoor")),
            ("bake_bread", 1, new LText("Дождись румяной корочки", "Qizarguncha kut", "Wait for a golden crust"),
                new LText("Нажми на тандыр, чтобы заглянуть внутрь. Не передержи!", "",
                    "Click the tandoor to look inside. Don't overbake it!")),
            ("take_out", 1, new LText("Сними лепёшку вовремя", "Nonni vaqtida uz", "Take the bread out in time"),
                new LText("Нажми на румяную лепёшку и тащи её", "", "Click the golden bread and drag it")),
            ("serve", 1, new LText("Положи лепёшку на дастархан", "Nonni dasturxonga qo‘y", "Put the bread on the dastarkhan"),
                new LText("Отпусти лепёшку над дастарханом справа", "", "Let go of the bread over the dastarkhan on the right"))
        };

        // ---------- карточки урока ----------

        public static readonly LText ClayTitle = new("Глина копит тепло", "", "Clay stores heat");
        public static readonly LText ClayBody = new(
            "Толстые глиняные стенки греются медленно и долго держат жар. Поэтому лепёшки пекут не на огне, а на горячих стенках.", "",
            "Thick clay walls heat up slowly and hold the heat for a long time. That's why bread bakes on the hot walls, not over the flames.");

        public static readonly LText ReadyTitle = new("Тандыр готов", "", "The tandoor is ready");
        public static readonly LText ReadyBody = new(
            "Стенки прогрелись. Дрова прогорают, а жар остаётся в глине — можно лепить лепёшки.", "",
            "The walls are hot. The wood burns down, but the heat stays in the clay — time to bake.");

        public static readonly LText StampTitle = new("Зачем чекич", "", "Why the chekich");
        public static readonly LText StampBody = new(
            "Проколы выпускают пар: середина остаётся тонкой и ровной, а края поднимаются пышным бортиком.", "",
            "The pricks let the steam out: the centre stays thin and flat while the edges rise into a soft rim.");

        public static readonly LText HotTitle = new("Горячо!", "", "Hot!");
        public static readonly LText HotBody = new(
            "Стенки раскалены. Пекари кладут тесто на подушку и прижимают его к стенке подушкой, чтобы не обжечь руку.", "",
            "The walls are scorching. Bakers put the dough on a cushion and press it to the wall with it so as not to burn their hand.");

        public static readonly LText DryTitle = new("Тесто сухое", "", "The dough is dry");
        public static readonly LText DryBody = new(
            "Сухое тесто не пристанет к стенке. Смочи его водой.", "",
            "Dry dough won't stick to the wall. Moisten it with water.");

        public static readonly LText ColdTitle = new("Стенки остыли", "", "The walls have cooled");
        public static readonly LText ColdBody = new(
            "Тесто держится, но не печётся: жара мало. Подбрось дров в топку.", "",
            "The dough holds on but doesn't bake: there isn't enough heat. Add more wood.");

        public static readonly LText GoldenTitle = new("Корочка зарумянилась", "", "Golden crust");
        public static readonly LText GoldenBody = new(
            "Пора снимать! Нажми на лепёшку и перенеси её на дастархан.", "",
            "Time to take it out! Click the bread and carry it to the dastarkhan.");

        public static readonly LText BurntTitle = new("Подгорела", "", "Burnt");
        public static readonly LText BurntBody = new(
            "Лепёшку снимают, как только она зарумянится. На столе новое тесто — попробуй ещё раз.", "",
            "Bread comes out as soon as it turns golden. There's fresh dough on the table — try again.");

        public static readonly LText RawServeTitle = new("Сначала испеки", "", "Bake it first");
        public static readonly LText RawServeBody = new(
            "Сырое тесто на дастархан не кладут — сначала испеки лепёшку.", "",
            "Raw dough doesn't go on the dastarkhan — bake the bread first.");

        public static readonly LText ServedTitle = new("Хлеб — на дастархан", "", "Bread on the dastarkhan");
        public static readonly LText ServedBody = new(
            "Лепёшку кладут лицом вверх и никогда — на землю: к хлебу в Узбекистане относятся бережно.", "",
            "Bread is laid face up and never on the ground: in Uzbekistan bread is treated with care.");

        public static readonly LText DoneTitle = new("Лепёшка готова!", "", "Your bread is ready!");
        public static readonly LText DoneBody = new(
            "Ты разжёг тандыр, наколол тесто чекичем и испёк лепёшку. Влажная сторона теста пристаёт к горячей глине, и лепёшка печётся прямо на стенке — от жара, который накопила глина.", "",
            "You lit the tandoor, pricked the dough with the chekich and baked the bread. The moist side of the dough sticks to the hot clay, and the bread bakes right on the wall — from the heat the clay has stored.");
    }
}
