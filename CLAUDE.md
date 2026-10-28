# Claude Code — вход в проект OLAM

Читайте и обновляйте **[AGENT_COORD.md](./AGENT_COORD.md)** — канал с Cursor.

## Навигация продукта
Хаб пространств → раздел → подраздел → VR. См. `Assets/OLAM/Docs/VR_ARCHITECTURE.md`.

## Ваша зона сейчас
`Assets/OLAM/Models/Rodin/**` — разбор GLB, rename, префабы, постановка в сцены.

## Cursor уже сделал (цепляйте к этому)
- `ScannableOrganism` + `EcoScanner` + `HologramCard`
- `EncyclopediaEntry` поля: biome, category, status, powerFact, interactHint, interactActionId
- Карточки: `Assets/OLAM/ScriptableObjects/Nature/Entry_*.asset`
- Когда модель вида готова: повесьте `ScannableOrganism`, в `entry` — нужный asset.

## Не трогать без записи в AGENT_COORD
`Assets/OLAM/Shaders/**`, `Nature/EcoScanner*`, `Nature/Scannable*`, `Scenes/NatureDome*`, `Scenes/Biome_*`

## Меню Unity
- **OLAM → Seed Nature Content**
- **OLAM → Build Nature Dome + Biome Stubs**
- **OLAM → Build Hub + Wire Portals**
- **OLAM → Nature Shaders + VR Perf**
