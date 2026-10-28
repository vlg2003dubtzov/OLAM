#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Olam.Core;

namespace Olam.EditorTools
{
    /// <summary>
    /// Сидер карточек «Живой Узбекистан» из ТЗ (лаконичные VR-тексты).
    /// </summary>
    public static class OlamNatureContentSeeder
    {
        const string Folder = "Assets/OLAM/ScriptableObjects/Nature";

        [MenuItem("OLAM/Seed Nature Content (Biomes + Cards)")]
        public static void Seed()
        {
            EnsureFolder();

            // Пустыня
            var saxaul = Entry("saxaul_black", BiomeId.Desert, SpeciesCategory.TreesShrubs, ConservationStatus.Common,
                "Саксаул чёрный", "Qora saksovul", "Black saxaul", "Haloxylon ammodendron",
                "Дерево без листьев! Чтобы не терять воду при +50°C, дышит зелёными веточками. Корни — до 20 м вглубь.",
                "Barglari yo‘q daraxt! +50°C da suvni saqlash uchun yashil novdalar bilan nafas oladi. Ildizlari 20 m gacha.",
                "A leafless tree! To save water at +50°C it breathes through green twigs. Roots reach 20 m deep.",
                "Корни пьют воду с глубины 5-этажного дома!", "Ildizlari 5 qavatli uy chuqurligidan suv ichadi!", "Roots drink from 5-storey depth!",
                "xray_sand", "Рентген песка", "Qum rentgeni", "Sand X-ray");

            var juzgun = Entry("juzgun", BiomeId.Desert, SpeciesCategory.TreesShrubs, ConservationStatus.Common,
                "Джузгун (кандым)", "Juzg‘un (qandim)", "Calligonum", "Calligonum",
                "Хранитель барханов. Пушистые плоды катятся как перекати-поле и закрепляют песок.",
                "Barxanlar qo‘riqchisi. Mevalari shamolda dumalab qumni ushlab turadi.",
                "Guardian of dunes. Fluffy fruits roll like tumbleweed and fix the sand.",
                "Плоды-шарики сеют новую жизнь на бархане.", "Dumaloq mevalar barxanda yangi hayot ekadi.", "Ball fruits sow life on the dune.",
                "blow_wind", "Подуть ветром", "Shamol esmoq", "Blow wind");

            var cistanche = Entry("cistanche", BiomeId.Desert, SpeciesCategory.HerbsFlowers, ConservationStatus.Common,
                "Цистанхе солончаковая", "Sho‘rxok sistanshe", "Desert cistanche", "Cistanche salsa",
                "Растение-пират! Берёт воду у корней саксаула. Цветёт раз в году яркой свечой.",
                "Qaroqchi o‘simlik! Saksovul ildizidan suv oladi. Yilda bir marta shamdek gullaydi.",
                "A pirate plant! It steals water from saxaul roots. Blooms once a year like a bright torch.",
                "Связь с саксаулом под песком.", "Qum ostida saksovul bilan bog‘lanish.", "Hidden link to saxaul under sand.",
                "show_link", "Показать связь", "Bog‘lanishni ko‘rsat", "Show link");

            var varan = Entry("desert_monitor", BiomeId.Desert, SpeciesCategory.Fauna, ConservationStatus.RedBook,
                "Серый варан", "Boz echkimer", "Desert monitor", "Varanus griseus",
                "«Пустынный крокодил» до 1,5 м! Быстрее человека, охотится на змей и скорпионов.",
                "«Cho‘l timsohi» 1,5 m gacha! Odamdan tezroq, ilon va chayonlarga ov qiladi.",
                "Desert “crocodile” up to 1.5 m! Faster than a human; hunts snakes and scorpions.",
                "Раздвоенный синий язык.", "Ikki shoxli ko‘k til.", "Forked blue tongue.",
                "hiss", "Варан шипит", "Echkimer shivirlaydi", "Monitor hisses");

            var toadhead = Entry("toadhead_agama", BiomeId.Desert, SpeciesCategory.Fauna, ConservationStatus.Common,
                "Ушастая круглоголовка", "Quloqli yumaloqbosh", "Toad-headed agama", "Phrynocephalus mystaceus",
                "Мастер маскировки. Раскрывает «уши» у рта, затем за 2 секунды ныряет в песок!",
                "Yashirinish ustasi. Og‘zidagi «quloqlarni» ochadi, 2 soniyada qumga sho‘ng‘iydi!",
                "Camouflage master. Flashes mouth “ears”, then buries itself in sand in 2 seconds!",
                "Вибрация — и исчезла.", "Tebranadi — va yo‘qoladi.", "Vibrates — and vanishes.",
                "bury", "Спрятаться в песок", "Qumga yashirin", "Hide in sand");

            // Адыры
            var tulipG = Entry("tulip_greigii", BiomeId.Adyr, SpeciesCategory.HerbsFlowers, ConservationStatus.RedBook,
                "Тюльпан Грейга", "Greyg lolasii", "Greig's tulip", "Tulipa greigii",
                "Король тюльпанов! Из предгорий Ташкента. Большой бокал, листья в бордовую крапинку. Прародитель сотен сортов.",
                "Lolalar shohi! Toshkent adirlaridan. Katta kosasi, barglarida to‘q qizil dog‘lar.",
                "King of tulips! From Tashkent foothills. Huge cup, maroon-speckled leaves; ancestor of hundreds of cultivars.",
                "Из семечка цветок растёт 7 лет — срывать нельзя!", "Urug‘dan gul 7 yilda o‘sadi — uzmang!", "Takes 7 years from seed — never pick!",
                "photo_only", "Только фото-скан", "Faqat foto-skan", "Photo-scan only");

            var eremurus = Entry("eremurus", BiomeId.Adyr, SpeciesCategory.HerbsFlowers, ConservationStatus.Common,
                "Эремурус мощный", "Kuchli shiryash", "Foxtail lily", "Eremurus robustus",
                "Горная свеча до 2 м! Цветки раскрываются снизу вверх. Из корней делали природный клей.",
                "Tog‘ shamchirog‘i 2 m gacha! Gullari pastdan yuqoriga ochiladi.",
                "Mountain torch up to 2 m! Flowers open from bottom to top. Roots once made natural glue.",
                "Пчёлы кружат у соцветия.", "Asalarilar atrofida aylanadi.", "Bees circle the spike.",
                "bees", "Позвать пчёл", "Asalarilarni chaqir", "Call bees");

            var tulipUz = Entry("tulip_uzbekistanica", BiomeId.Adyr, SpeciesCategory.HerbsFlowers, ConservationStatus.Endemic,
                "Тюльпан узбекистанский", "O‘zbekiston lolasii", "Uzbekistan tulip", "Tulipa uzbekistanica",
                "Уникальный эндемик! Растёт только в Узбекистане (Сурхандарья, Кашкадарья) — больше нигде на Земле!",
                "Noyob endemik! Faqat O‘zbekistonda o‘sadi — Yerda boshqa joyda yo‘q!",
                "Unique endemic! Grows only in Uzbekistan — nowhere else on Earth!",
                "На карте мира — одна светящаяся точка.", "Dunyo xaritasida bitta yorug‘ nuqta.", "One glowing dot on the world map.",
                "world_map", "Карта мира", "Dunyo xaritasi", "World map");

            // Горы / высокогорья / тугаи
            var juniper = Entry("archa_juniper", BiomeId.Mountains, SpeciesCategory.TreesShrubs, ConservationStatus.Common,
                "Арча зеравшанская", "Zarafshon archasi", "Zeravshan juniper", "Juniperus seravschanica",
                "Страж гор. Живёт до 1000 лет! Фитонциды очищают воздух, корни держат склоны от селей.",
                "Tog‘ qo‘riqchisi. 1000 yilgacha yashaydi! Havo tozalaydi, yonbag‘irlarni ushlab turadi.",
                "Guardian of mountains. Lives up to 1000 years! Cleans air and holds slopes against mudflows.",
                "Годовые кольца долгожителя.", "Uzoq umr yillik halqalari.", "Ancient growth rings.",
                "rings", "Показать кольца", "Halqalarni ko‘rsat", "Show rings");

            var apple = Entry("malus_sieversii", BiomeId.Mountains, SpeciesCategory.TreesShrubs, ConservationStatus.RedBook,
                "Яблоня Сиверса", "Sivers olma daraxti", "Sievers' apple", "Malus sieversii",
                "Прабабушка всех яблок! По ДНК почти все культурные яблоки произошли от дикой яблони Тянь-Шаня.",
                "Barcha olmalarning bobosi! DNK bo‘yicha deyarli barcha madaniy olmalar Tyan-Shandan.",
                "Grandmother of all apples! DNA shows almost all cultivars descend from Tian Shan wild apples.",
                "Яблоки зреют и падают.", "Olmalar pishib tushadi.", "Apples ripen and fall.",
                "drop_apples", "Урожай", "Hosil", "Harvest");

            var leopard = Entry("snow_leopard", BiomeId.Highlands, SpeciesCategory.Fauna, ConservationStatus.RedBook,
                "Снежный барс", "Qor qoploni", "Snow leopard", "Panthera uncia",
                "Властелин вершин (~4000 м). Пушистый хвост — руль в прыжке до 14 м и шарф во сне.",
                "Cho‘qqilar egasi (~4000 m). Dumini rul va sharf sifatida ishlatadi, sakrash 14 m gacha.",
                "Lord of peaks (~4000 m). Fluffy tail steers leaps up to 14 m and wraps like a scarf when sleeping.",
                "Прыжок со скалы на скалу.", "Qoyadan qoyaga sakrash.", "Cliff-to-cliff leap.",
                "leap", "Показать прыжок", "Sakrashni ko‘rsat", "Show leap");

            var markhor = Entry("markhor", BiomeId.Highlands, SpeciesCategory.Fauna, ConservationStatus.RedBook,
                "Винторогий козёл (мархур)", "Markhur", "Markhor", "Capra falconeri",
                "Обитатель отвесных скал. Рога — как штопоры. «Мархур» по-персидски — «поедатель змей».",
                "Tik qoyalarda yashaydi. Shoxlari corkdek. Forscha «ilon yovuvchi».",
                "Lives on sheer cliffs. Corkscrew horns. In Persian, “markhor” means “snake-eater”.",
                "Подъём по почти вертикали.", "Deyarli tik ko‘tarilish.", "Near-vertical climb.",
                "climb", "Взобраться", "Ko‘taril", "Climb");

            var turanga = Entry("turanga", BiomeId.Tugai, SpeciesCategory.TreesShrubs, ConservationStatus.Common,
                "Туранга", "Turang‘il", "Euphrates poplar", "Populus euphratica",
                "Дерево тугайных джунглей у Амударьи. Листья разной формы на одном дереве — «хамелеон» поймы.",
                "Amudaryo tugaylari daraxti. Bitta daraxtida turli shakldagi barglar.",
                "Tree of Amudarya tugai thickets. Different leaf shapes on one tree — a floodplain chameleon.",
                "Шум листвы над водой.", "Suv ustida barglar shovqini.", "Leaves rustle over water.",
                "rustle", "Шум листвы", "Barg shovqini", "Leaf rustle");

            var deer = Entry("bukhara_deer", BiomeId.Tugai, SpeciesCategory.Fauna, ConservationStatus.RedBook,
                "Бухарский олень", "Buxoro bug‘usi", "Bukhara deer", "Cervus elaphus bactrianus",
                "🔴 Красная книга. Хозяин тугаев. Рога ветвятся как речные протоки поймы.",
                "🔴 Qizil kitob. Tugaylar egasi. Shoxlari daryo irmoqlari kabi.",
                "Red List. Lord of the tugai. Antlers branch like floodplain channels.",
                "Рёв над зарослями.", "Chakalakzor ustida baqirish.", "A roar over the thickets.",
                "roar", "Рёв оленя", "Bug‘u baqirishi", "Deer roar");

            Biome("Biome_Desert", BiomeId.Desert, "Пустыни (Чўл)", "Cho‘llar", "Deserts",
                "Кызылкум и Устюрт: барханы, саксаул, варан.", "Qizilqum va Ustyurt.", "Kyzylkum & Ustyurt dunes.",
                "Biome_Desert", new Color(0.9f, 0.72f, 0.35f), saxaul, juzgun, cistanche, varan, toadhead);

            Biome("Biome_Adyr", BiomeId.Adyr, "Предгорья (Адыры)", "Adirlar", "Foothills",
                "Весенний ковер тюльпанов и эремуруса.", "Bahorgi lola gilami.", "Spring tulip carpet.",
                "Biome_Adyr", new Color(0.85f, 0.35f, 0.45f), tulipG, eremurus, tulipUz);

            Biome("Biome_Mountains", BiomeId.Mountains, "Горы (Тау)", "Tog‘lar", "Mountains",
                "Арчовые леса и яблоня Сиверса.", "Archa o‘rmonlari.", "Juniper forests & wild apple.",
                "Biome_Mountains", new Color(0.35f, 0.55f, 0.35f), juniper, apple);

            Biome("Biome_Highlands", BiomeId.Highlands, "Высокогорья (Яйлау)", "Yaylovlar", "Highlands",
                "Снежные пики, барс и мархур. Пилот: ChatkalNightTrail.", "Qor cho‘qqilari.", "Snow peaks; pilot: Chatkal.",
                "ChatkalNightTrail", new Color(0.7f, 0.82f, 0.95f), leopard, markhor);

            Biome("Biome_Tugai", BiomeId.Tugai, "Тугаи", "Tugaylar", "Tugai",
                "Пойма Амударьи: туранга и бухарский олень.", "Amudaryo qayirlari.", "Amudarya floodplain forests.",
                "Biome_Tugai", new Color(0.25f, 0.55f, 0.4f), turanga, deer);

            AssetDatabase.SaveAssets();
            Debug.Log("OLAM: Nature content seeded → " + Folder);
        }

        static EncyclopediaEntry Entry(
            string id, BiomeId biome, SpeciesCategory cat, ConservationStatus status,
            string titleRu, string titleUz, string titleEn, string sci,
            string descRu, string descUz, string descEn,
            string powerRu, string powerUz, string powerEn,
            string actionId, string hintRu, string hintUz, string hintEn)
        {
            string path = $"{Folder}/Entry_{id}.asset";
            var e = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>(path);
            if (e == null)
            {
                e = ScriptableObject.CreateInstance<EncyclopediaEntry>();
                AssetDatabase.CreateAsset(e, path);
            }

            var so = new SerializedObject(e);
            so.FindProperty("id").stringValue = id;
            SetL(so, "title", titleRu, titleUz, titleEn);
            so.FindProperty("scientificName").stringValue = sci;
            SetL(so, "description", descRu, descUz, descEn);
            so.FindProperty("biome").enumValueIndex = (int)biome;
            so.FindProperty("category").enumValueIndex = (int)cat;
            so.FindProperty("status").enumValueIndex = (int)status;
            SetL(so, "powerFact", powerRu, powerUz, powerEn);
            SetL(so, "interactHint", hintRu, hintUz, hintEn);
            so.FindProperty("interactActionId").stringValue = actionId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(e);
            return e;
        }

        static void Biome(string assetName, BiomeId id, string ru, string uz, string en,
            string blurbRu, string blurbUz, string blurbEn, string scene, Color color,
            params EncyclopediaEntry[] species)
        {
            string path = $"{Folder}/{assetName}.asset";
            var b = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path);
            if (b == null)
            {
                b = ScriptableObject.CreateInstance<BiomeDefinition>();
                AssetDatabase.CreateAsset(b, path);
            }
            var so = new SerializedObject(b);
            so.FindProperty("id").enumValueIndex = (int)id;
            SetL(so, "title", ru, uz, en);
            SetL(so, "blurb", blurbRu, blurbUz, blurbEn);
            so.FindProperty("sceneName").stringValue = scene;
            so.FindProperty("portalColor").colorValue = color;
            var arr = so.FindProperty("featuredSpecies");
            arr.arraySize = species.Length;
            for (int i = 0; i < species.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = species[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(b);
        }

        static void SetL(SerializedObject so, string field, string ru, string uz, string en)
        {
            var p = so.FindProperty(field);
            p.FindPropertyRelative("ru").stringValue = ru;
            p.FindPropertyRelative("uz").stringValue = uz;
            p.FindPropertyRelative("en").stringValue = en;
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/OLAM", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/OLAM/ScriptableObjects", "Nature");
        }
    }
}
#endif
