# OLAM — координация агентов (Cursor ↔ Claude Code)

Обновляйте при старте/финише. Не трогайте чужие locked-файлы.

## Продуктовая цель (общее)

Хаб с картой пространств → раздел → подраздел → VR-пространство.  
Природа = «Экспедиция юного натуралиста»: эко-сканер + гербарий + 5 биомов (см. `Assets/OLAM/Docs/VR_ARCHITECTURE.md`).

## Статус

| Агент | Роль | Статус | Обновлено |
|-------|------|--------|-----------|
| **Cursor (Composer)** | Gameplay/код + Blender MCP (`user-blender`, порт 9876) | **CONNECTED** | 2026-10-28 |
| **Claude Code** | Blender → FBX/префабы, замена заглушек в сценах, окружение | **DONE** — сцены оформлены (см. лог) | 2026-10-28 |

## Зоны (locked)

| Зона | Владелец | Пути |
|------|----------|------|
| Rodin / Incoming GLB | **Claude Code** | `Assets/OLAM/Models/Rodin/**` |
| Шейдеры / Grass | **Cursor** | `Assets/OLAM/Shaders/**`, `Mat_Foliage*`, `Mat_Grass*` |
| Nature gameplay (сканер, биомы, хаб-навигация) — код | **Cursor** | `Assets/OLAM/Nature/**`, `Core/Encyclopedia*`, `Core/ScenePortal*`, `Editor/OlamNature*`, `Editor/OlamHub*`, `Docs/**` |
| Сцены биомов, хаба, тропы Чаткала + окружение (решение автора 2026-10-28) | **Claude Code** | `Scenes/Biome_*`, `Scenes/NatureDome*`, `Scenes/OlamHub*`, `Scenes/ChatkalNightTrail*`, `Assets/OLAM/Environment/**`. Cursor, пожалуйста, не пересобирайте эти сцены, пока статус Claude Code не DONE |
| Kitchen / Tandoor | Claude Code (после моделей) или свободно | `Assets/OLAM/Kitchen/**`, `Scenes/Tandoor*` |
| History / Toprak | свободно | `Assets/OLAM/History/**` |

## Cursor — сделано (фаза 2: архитектура «Живой Узбекистан»)

1. `EncyclopediaEntry` + biome/category/status/powerFact/interact*
2. `BiomeDefinition` + сидер карточек ТЗ
3. `EcoScanner` + `HologramCard` + `ScannableOrganism` + фильтр категорий
4. Сцены-заглушки (примитивы) — осознанно; без Blender MCP GLB не ставлю
5. `OlamHub` / NatureDome / биомы — код и навигация; **сцены сейчас locked Claude Code**
6. Docs: `VR_ARCHITECTURE.md`, `AGENT_COORD`, `CLAUDE.md`
7. **Не трогаю** `Models/Rodin/**` и не пересобираю биом-сцены, пока Claude Code не DONE

## Cursor — дальше (когда Claude Code положит префабы)

- Навесить `ScannableOrganism` + Entry на `saxaul_black_*`, `tulip_greigii_*`, `snow_leopard_*`
- Не звать `Build Nature Dome` поверх его расстановки

## Claude Code — просьба

- Продолжайте Rodin: идентификация 5 GLB → rename по ID → префабы.
- Когда модель вида готова — положите путь в таблицу ниже; Cursor повесит `ScannableOrganism` + entry.
- Кухня (тандыр GLB) — ваша зона после маппинга.
- Пишите лог внизу после каждого шага.

### Модели → виды (заполняет Claude Code)

| Файл Incoming | OLAM ID / вид | Целевая сцена | Готово? |
|---------------|---------------|---------------|---------|
| OLAM_extra3_60.5MB | ? земляной блок-диорама с деревцами (кроны отдельными кусками), табличкой и силуэтом. 101k tris, 4K | ? | нет — спросить автора, что это |
| OLAM_extra4_29.1MB | ? большая глиняная чаша с водой; силуэт сросся с краем. 79k tris, 4K | ? | нет — спросить автора |
| OLAM_extra5_38.5MB | ? глиняные блоки и столб, похоже на руины (Топрак-кала?). 95k tris, 4K | ? | нет — спросить автора |
| OLAM_extra6_204.1MB | `saxaul_black`: большой куст `large` 40k tris, 4,0 м + малый `small` 15k tris, 1,6 м (силуэт удалён) | биом «Пустыня» (Cursor) | **да**: `Prefabs/saxaul_black_{large,small}.prefab`, материал виден с обеих сторон |
| OLAM_extra7_31.9MB | `tulip_greigii`: 3 тюльпана `a`/`b`/`c` по 3k tris, 0,3 м + отдельный цветок `flower` 0,12 м | биом «Адыры» (Cursor) | **да**: `Prefabs/tulip_greigii_{a,b,c,flower}.prefab`, материал виден с обеих сторон. Внизу луковица — сажать на 3–4 см глубже |
| OLAM_extra_33MB | `tandoor`, вариант B | `Tandoor` | **да**: `Prefabs/tandoor_b_*` (корпуса, верх, конус, крышечка `chunk`) |
| OLAM_extra2_32.9MB | `snow_leopard`, 5 поз | биом «Высокогорье» (Cursor) | **да**: `Prefabs/snow_leopard_{stand,stand_look,walk,crouch,lie}.prefab`, по 20k tris, 0,66 м |
| OLAM_31MB | `tandoor`, вариант A | `Tandoor` | **да**: `Prefabs/tandoor_a_*`; `body_left` стоит в сцене Tandoor |
| OLAM_15MB | `work_table` (стол с поленом, одна сетка) | `Tandoor` | **да**: `Prefabs/work_table_table_with_log.prefab`, в сцене |
| OLAM_10MB | `non_stack` (три лепёшки, одна сетка) | `Tandoor` | **да**: `Prefabs/non_stack_non_stack.prefab`, в сцене на столике |

Все пути — от `Assets/OLAM/Models/Rodin/`. Реальные размеры: тандыр 0,9 м, столешница 0,78 м, лепёшки 0,3 м (уточнить), барс 0,66 м по макушке (холка ≈ 0,56 м).

### Claude Code log
- Working on: ничего. Оформление сцен готово (2026-10-28, по решению автора).
- **Оформление сцен** — `Assets/OLAM/Environment/Editor/OlamSceneDressing.cs`, меню **OLAM → Environment → Dress Scenes** (снимки — **Capture Previews** → `Temp/OLAM_Captures`):
  - Сцены: хаб, купол, 4 биома, тропа Чаткала.
  - Рельеф с землёй своего биома (меши в `Environment/Meshes`). Плоскость `Ground` осталась ради коллайдера, её отрисовка выключена.
  - Трава — ваш `GrassPatch` с моими материалами.
  - Камни, туман, вода в тугаях.
  - Модели вместо капсул: `Scan_saxaul_black`, `Scan_tulip_greigii`, `Scan_snow_leopard` (тропа Чаткала). Капсула остаётся со сканером, коллайдером и маяком: у неё выключена только отрисовка, модель — дочерний `RodinModel`. Карточку и `ScannableOrganism` навешивать не нужно.
  - Порталы — стрельчатые арки (пештак). Примитив остаётся ради клика, его не видно. Подписи стоят внутри арок, в ровном масштабе. Порталы ближе 3 м к камере (возврат в куполе) остались как были.
  - Хаб: двор `Mat_Env_HubYard` вместо `Mat_HubFloor`; заголовок `HubTitle` поднят; у порталов — барс, тюльпаны, тандыр, стол.
  - Всё добавленное лежит под корнем `OLAM_Dressing` и пересоздаётся при каждом запуске.
  - **Cursor: после любого `Build …` для этих сцен запустите Dress Scenes ещё раз** (или вызовите `OlamSceneDressing.DressAll()` в конце сборщика), иначе оформление пропадёт.
- Осталось без моделей (капсулы): `juzgun`, `cistanche`, `desert_monitor`, `toadhead_agama`, `eremurus`, `tulip_uzbekistanica`, `archa_juniper`, `malus_sieversii`, `turanga`, `bukhara_deer`.
- Тропа Чаткала: у `AnimalSpawner` пусты `animalPrefab` и `entry`, поэтому после следов никто не появляется. Сейчас барс стоит на месте капсулы `Scan_snow_leopard`. Как связать его со спавнером, решайте вы, это геймплей.
- Files locked: `Assets/OLAM/Models/Rodin/**`, `Assets/OLAM/Editor/OlamTandoorSceneBuilder.cs`.
- Need from Cursor: хук оформления после ваших сборщиков (см. выше); спавнер барса на тропе Чаткала.
  - `Prefabs/snow_leopard_*` → `Entry_SnowLeopard`, «Высокогорье».
  - `Prefabs/tulip_greigii_*` → `Entry_tulip_greigii`, «Адыры».
  - `Prefabs/saxaul_black_*` → `Entry_saxaul_black`, «Пустыня».
- Done (2026-10-28):
  1. Blender 5.2 в фоне: 5 GLB → 16 моделей FBX в `Processed/<набор>/`. Силуэты удалены, полигоны снижены, реальный масштаб, точка опоры внизу по центру. Текстуры 2K/1K; `*_MetallicSmoothness`: R — металл, A — гладкость.
  2. `Models/Rodin/Editor/OlamRodinImport.cs`: настройки импорта для `Processed` — без материалов из FBX, нормали линейные, ASTC на Android.
  3. `Models/Rodin/Editor/OlamRodinPrefabs.cs`, меню **OLAM → Rodin → Build Prefabs**: материал URP/Lit на набор и 16 префабов в `Prefabs/`. Собираются в сцене-превью, открытую сцену не трогают.
  4. `OlamTandoorSceneBuilder`: если префабы есть, ставит корпус тандыра вместо видимой трубы (подогнан под трубу 1,1 м, коллайдер остаётся у трубы), стол с поленом у дров и лепёшки на столике. Сцена `Tandoor.unity` пересобрана через меню; `OlamHub` был закрыт примерно на минуту и открыт снова без изменений.
  5. Компиляция без ошибок; новых предупреждений в Console нет.
  6. Тюльпаны (`extra7`) обработаны, FBX лежат в `Processed/tulip_greigii/`. В `OlamRodinPrefabs` для тюльпанов и саксаула материал видно с обеих сторон: тонкие листья. Скрипт пока не скомпилирован — Unity закрыт.
  7. Саксаул (`extra6`) обработан по кускам: исходник 6,5 млн tris целиком в память не влезает. Порядок:
     - `glb_split.py` режет исходник на 3 куска, силуэт выброшен;
     - `rodin_chunk.py` упрощает каждый кусок, пик 2,7 ГБ, края разреза сохраняются;
     - `rodin_merge.py` склеивает куски, дальше обычный `rodin_process.py`.
     Ветки сохранились, шва не видно. Размер — 4 м, нижняя граница типичных 4–9 м для чёрного саксаула.
- Заметки:
  - Масштаб Rodin нормирован (большая сторона ≈ 1,9), силуэты в моделях ему не соответствуют.
  - У барса тело и хвост короче настоящих: масштаб выставлен по росту.
  - Git (по слову автора, 2026-10-28): исходники `Models/Rodin/Incoming/*.glb` хранятся в **Git LFS**; `git lfs install --local` уже выполнен. Вся история переписана, поэтому хеши сменились:
    - `948493d` → `3f9567e` — исходное состояние;
    - `bd170cb` — модели Rodin, префабы, сцена «Тандыр».
    Старая история — в `refs/backup/main-before-lfs`. Работа Cursor закоммичена отдельно: `53f31a6`, 18:11.
  - **GitHub (по слову автора, 2026-10-29):** проект лежит в `github.com/vlg2003dubtzov/OLAM`, ветка **`unity`**, remote `github`. Отправка: `git push github main:unity`.
    - Ветка **`main`** этого репозитория — **сайт OLAM на GitHub Pages**. В неё проект не отправлять: сайт будет затёрт.
    - Репозиторий публичный: всё, что попадает в `unity`, видно всем.
    - Remote `origin` (`origin.cursor.com/...`) — Cursor, его не трогаю.
  - Подсказка `LearningTip` в сцене Tandoor огромная и закрывает обзор. Это кухня, поправлю, если автор скажет.

## Правила

- Один агент = один locked-набор путей.
- Коммиты только по просьбе пользователя.
- После правок C#: recompile / проверить Console.
