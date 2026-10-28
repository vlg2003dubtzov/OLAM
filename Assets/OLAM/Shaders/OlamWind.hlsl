// Shared wind for OLAM foliage / grass (URP, VR-friendly).
#ifndef OLAM_WIND_INCLUDED
#define OLAM_WIND_INCLUDED

// Мировая фаза — разные кусты не синхронны; h01 = 0 у корня, 1 у верхушки.
float3 OlamApplyWind(float3 positionOS, float3 positionWS, float h01, float strength, float speed)
{
    float t = _Time.y * speed;
    float phase = positionWS.x * 0.35 + positionWS.z * 0.27;
    float bend = h01 * h01; // сильнее у кончиков
    float sx = sin(t + phase) + 0.35 * sin(t * 1.7 + phase * 1.3);
    float sz = cos(t * 0.85 + phase * 0.9) + 0.25 * sin(t * 2.1 + phase);
    positionOS.x += sx * strength * bend * 0.12;
    positionOS.z += sz * strength * bend * 0.09;
    // лёгкое «сжатие» по Y при сильном ветре — меньше клиппинга
    positionOS.y -= abs(sx) * strength * bend * 0.02;
    return positionOS;
}

// Плавное исчезновение на дистанции (для VR: меньше overdraw вдали).
half OlamDistanceFade(float3 positionWS, float start, float end)
{
    float3 cam = GetCameraPositionWS();
    float d = distance(cam, positionWS);
    return (half)saturate((end - d) / max(0.001, end - start));
}

#endif
