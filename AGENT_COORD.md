# OLAM — координация агентов (Cursor ↔ Claude Code)

Обновляйте этот файл при старте/финише задачи. Не трогайте чужие locked-файлы.

## Статус сейчас

| Агент | Роль | Статус | Обновлено |
|-------|------|--------|-----------|
| **Cursor (Composer)** | Шейдеры природы, трава, VR-оптимизация | **DONE** (фаза 1) | 2026-10-28 |
| **Claude Code** | *(заполните свой блок ниже)* | | |

## Зоны (чтобы не конфликтовать)

| Зона | Владелец | Файлы |
|------|----------|--------|
| Шейдеры / материалы природы | **Cursor** | `Assets/OLAM/Shaders/**`, `Assets/OLAM/Materials/Mat_Foliage*`, `Mat_Grass*`, `Mat_*Sand*`, `Mat_ChatkalSoil*` |
| Incoming GLB / Rodin | **оба, по очереди** | `Assets/OLAM/Models/Rodin/Incoming/**` — только копировать, не переименовывать без маппинга ID |
| Kitchen / Tandoor механика | Claude Code (если берёте) | `Assets/OLAM/Kitchen/**`, `Scenes/Tandoor*` |
| History / Toprak | свободно | `Assets/OLAM/History/**` |
| Core / локализация | осторожно | `Assets/OLAM/Core/**` — мелкие правки ок, большие — запишите здесь |

## Cursor — сделано (фаза 1)

1. `OlamWind.hlsl` — общий wind + distance fade
2. `OLAM/Foliage Wind` — instancing, DepthOnly, fade, двусторонний lit
3. `OLAM/Grass` — новый шейдер tip-tint + wind
4. `OLAM/Sand Ground` / `Clay Stylized` — fog + instancing
5. `GrassPatch.cs` + 4 патча в ChatkalNightTrail
6. Меню **OLAM → Nature Shaders + VR Perf** (renderScale 0.7, shadows off)
7. Материалы: `Mat_GrassChatkal`, обновлены Foliage/Sand/Clay
8. Incoming GLB / Kitchen — **не трогал** (свободно для Claude Code)

## Cursor — дальше (если свободно)

- Альбедо-текстура травы (alpha cutout) вместо white
- LOD / меньше count ночью
- Rock material на Clay/Sand variant

## Claude Code — пожалуйста напишите

```
### Claude Code log
- Working on:
- Files locked:
- Need from Cursor:
- Done:
```

## Правила

- Один агент = один «locked» набор путей за раз.
- После компиляции: проверьте Console на розовый/missing shader.
- Коммиты только по просьбе пользователя.
- Unity CLI self-update (beta.13→14) — ждать подтверждения пользователя.
