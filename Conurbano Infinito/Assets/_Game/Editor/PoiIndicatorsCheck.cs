using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PoiIndicatorsCheck
{
    private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;

    [MenuItem("Tools/Conurbano Infinito/Comprobar indicadores de POIs")]
    public static void Run()
    {
        Vector2[] sizes = { new Vector2(1280, 720), new Vector2(1280, 960), new Vector2(1920, 720) };
        Vector3[] directions =
        {
            new Vector3(-1, 0.5f, 1), new Vector3(2, 0.5f, 1),
            new Vector3(0.5f, 2, 1), new Vector3(0.5f, -1, 1),
            new Vector3(-1, 2, 1), new Vector3(2, 2, 1),
            new Vector3(-1, -1, 1), new Vector3(2, -1, 1)
        };
        foreach (Vector2 size in sizes)
        {
            // Incluye el espacio de un equipo lleno con varias pasivas.
            Rect area = Rect.MinMaxRect(12, 300, size.x - 12, size.y - 140);
            foreach (Vector3 direction in directions)
            {
                if (!GetPanel(direction, size, area, out Rect panel) || !Inside(panel, area))
                    throw new Exception("Un indicador salió de su área o no apareció.");
                if ((direction.x < 0 && panel.center.x >= size.x / 2) ||
                    (direction.x > 1 && panel.center.x <= size.x / 2) ||
                    (direction.y > 1 && panel.center.y >= size.y / 2) ||
                    (direction.y < 0 && panel.center.y <= size.y / 2))
                    throw new Exception("Un indicador quedó en el lado opuesto al POI.");

                Rect separated = (Rect)typeof(GameHud).GetMethod("SeparatePoiPanels", Flags)
                    .Invoke(null, new object[] { panel, panel, area });
                if (panel.Overlaps(separated) || !Inside(separated, area))
                    throw new Exception("Dos POIs en el mismo borde se superponen o salen del área.");
            }
            Vector3[] visible = { new Vector3(0.5f, 0.5f, 1), new Vector3(0, 0, 1),
                new Vector3(1, 1, 1), new Vector3(2, 0.5f, -1) };
            foreach (Vector3 position in visible)
                if (GetPanel(position, size, area, out _))
                    throw new Exception("Un POI visible o detrás de cámara generó un indicador.");
        }

        if (Application.isPlaying)
        {
            RiskZone[] zones = UnityEngine.Object.FindObjectsByType<RiskZone>(FindObjectsSortMode.None);
            GameHud hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
            if (zones.Length != 2 || hud == null)
                throw new Exception("Main debe tener HUD, Kiosco y Parrilla.");
            foreach (RiskZone zone in zones)
            {
                Camera camera = new SerializedObject(zone).FindProperty("gameCamera")
                    .objectReferenceValue as Camera;
                Vector3 original = zone.transform.position;
                try
                {
                    if (camera == null || string.IsNullOrEmpty(zone.DisplayName))
                        throw new Exception("El POI no tiene cámara o nombre.");
                    float depth = camera.WorldToViewportPoint(original).z;
                    Rect area = Rect.MinMaxRect(12, 190, 1268, 580);
                    foreach (float x in new[] { -1f, 0.5f, 2f })
                    {
                        zone.transform.position = camera.ViewportToWorldPoint(new Vector3(x, 0.5f, depth));
                        bool shown = GetPanel(zone.ViewportPosition, sizes[0], area, out _);
                        if (shown != (x != 0.5f))
                            throw new Exception("Mover un POI no actualizó la visibilidad del indicador.");
                    }
                }
                finally
                {
                    zone.transform.position = original;
                }
            }
        }

        Debug.Log("POIs: ocho direcciones, tres formatos, límites, separación y visibilidad correctos.");
    }

    private static bool GetPanel(Vector3 viewport, Vector2 size, Rect area, out Rect panel)
    {
        object[] arguments = { viewport, size, area, null };
        bool shown = (bool)typeof(GameHud).GetMethod("TryGetPoiPanel", Flags).Invoke(null, arguments);
        panel = (Rect)arguments[3];
        return shown;
    }

    private static bool Inside(Rect panel, Rect area) =>
        panel.xMin >= area.xMin - 0.01f && panel.xMax <= area.xMax + 0.01f &&
        panel.yMin >= area.yMin - 0.01f && panel.yMax <= area.yMax + 0.01f;
}
