using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Owned by the menu or terminal screen; opening records never changes the run.
public sealed class RecordsView
{
    private readonly LocalRecords records;
    private readonly RunConfiguration[] modes;
    private int modeIndex, versionIndex, page, focus, firstLine;
    private string[] versions, lines;
    private StoredRun[] table;
    private StoredRun detail;
    public bool IsOpen { get; private set; } = true;

    public RecordsView(LocalRecords records, string modeId, string version)
    {
        this.records = records;
        modes = Array.ConvertAll(RunRules.All, r => r.CreateConfiguration());
        modeIndex = Array.FindIndex(modes, m => m.StableId == modeId);
        if (modeIndex < 0) modeIndex = 0;
        RefreshVersions(version);
    }

    private void RefreshVersions(string preferred)
    {
        versions = records.Versions(modes[modeIndex].StableId, modes[modeIndex].RulesetVersion);
        versionIndex = Array.IndexOf(versions, preferred);
        if (versionIndex < 0) versionIndex = Array.IndexOf(versions, modes[modeIndex].RulesetVersion);
        RefreshTable();
    }

    private void RefreshTable()
    {
        table = records.Table(modes[modeIndex].StableId, versions[versionIndex]);
        page = focus = 0;
    }

    public void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame) { Back(); return; }
        if (detail != null)
        {
            if (keyboard.pageUpKey.wasPressedThisFrame) firstLine = Math.Max(0, firstLine - 5);
            if (keyboard.pageDownKey.wasPressedThisFrame) firstLine = Math.Min(lines.Length - 1, firstLine + 5);
            if (GameUi.Confirm) Back();
            return;
        }
        focus = GameUi.Navigate(focus, 6 + VisibleRows);
        if (GameUi.Confirm) Activate(focus);
    }

    private int VisibleRows => Math.Min(5, Math.Max(0, table.Length - page * 5));
    private void Back()
    {
        if (detail != null) { detail = null; lines = null; }
        else IsOpen = false;
    }

    private void Show(StoredRun run)
    {
        if (run == null) return;
        detail = run; lines = run.SummaryLines(); firstLine = 0;
    }

    private void Activate(int action)
    {
        if (action == 0) { modeIndex = (modeIndex + 1) % modes.Length; RefreshVersions(modes[modeIndex].RulesetVersion); }
        else if (action == 1) { versionIndex = (versionIndex + 1) % versions.Length; RefreshTable(); }
        else if (action < 2 + VisibleRows) Show(table[page * 5 + action - 2]);
        else if (action == 2 + VisibleRows) { page = Math.Max(0, page - 1); focus = 0; }
        else if (action == 3 + VisibleRows) { page = Math.Min(Math.Max(0, (table.Length - 1) / 5), page + 1); focus = 0; }
        else if (action == 4 + VisibleRows) Show(records.LastResult);
        else Back();
    }

    public void Draw()
    {
        var panel = new Rect(GameUi.SafeArea.center.x - 508, GameUi.SafeArea.center.y - 316, 1016, 632);
        GameUi.Sign(panel, GameUi.Gold);
        GameUi.Text(new Rect(panel.x + 24, panel.y + 14, 968, 44), detail == null ? "Récords de este equipo" : $"Build · {detail.Points} puntos", 34, GameUi.Ink);
        GameUi.Text(new Rect(panel.x + 24, panel.y + 62, 968, 28),
            "Tabla local · Flechas / Tab + Enter · Mouse · Esc: volver", 18, GameUi.Petrol);
        if (detail != null)
        {
            GameUi.Surface(new Rect(panel.x + 20, panel.y + 100, 976, 452), "enamel");
            DrawLines(new Rect(panel.x + 36, panel.y + 112, 944, 428), lines, ref firstLine);
            if (GameUi.Button(new Rect(panel.x + 648, panel.y + 570, 344, 42), "Volver a la tabla", "ESC", true)) Back();
            return;
        }
        DrawButton(new Rect(panel.x + 24, panel.y + 108, 288, 46), modes[modeIndex].IsEndless ? "Infinito" : "Clásico", 0);
        DrawButton(new Rect(panel.x + 324, panel.y + 108, 668, 46), "Reglas: " + versions[versionIndex], 1);
        GameUi.Text(new Rect(panel.x + 24, panel.y + 164, 968, 28),
            "Puntos ↓ · Tiempo ↓ · Run ID ordinal ↑    |    Elegí una marca para ver su build", 18, GameUi.Petrol);
        if (table.Length == 0) GameUi.Text(new Rect(panel.x + 24, panel.y + 230, 968, 60),
            "Todavía no hay marcas en este modo y versión.\nPráctica y abandono sólo figuran como último resultado.", 22, GameUi.Ink, TextAnchor.UpperLeft, true);
        for (int i = 0; i < VisibleRows; i++)
        {
            StoredRun run = table[page * 5 + i];
            string character = run.characterId;
            foreach (CharacterData data in CharacterData.All) if (data.StableId == character) { character = data.DisplayName; break; }
            var row = new Rect(panel.x + 24, panel.y + 204 + i * 58, 968, 50);
            if (GameUi.Button(row, $"{page * 5 + i + 1}.  {run.Points} pt · {run.activeSeconds:0.0} s · {character}", null, focus == i + 2))
            { focus = i + 2; Activate(focus); }
        }
        GameUi.Text(new Rect(panel.x + 24, panel.y + 498, 968, 28), records.Notice ??
            $"{table.Length} de 20 marcas · Página {page + 1}/{Math.Max(1, (table.Length + 4) / 5)} · Historial separado por reglas", 18, GameUi.Petrol);
        DrawButton(new Rect(panel.x + 24, panel.y + 538, 222, 46), "Anterior", 2 + VisibleRows);
        DrawButton(new Rect(panel.x + 260, panel.y + 538, 222, 46), "Siguiente", 3 + VisibleRows);
        DrawButton(new Rect(panel.x + 496, panel.y + 538, 496, 46), "Último resultado", 4 + VisibleRows);
        DrawButton(new Rect(panel.x + 648, panel.y + 588, 344, 32), "Volver", 5 + VisibleRows);
    }

    private void DrawButton(Rect rect, string label, int action)
    {
        if (!GameUi.Button(rect, label, null, focus == action)) return;
        focus = action; Activate(action);
        // Avoid a second IMGUI click reaching controls revealed by this action.
        GUIUtility.ExitGUI();
    }

    public static void DrawLines(Rect area, string[] text, ref int first)
    {
        if (text == null || text.Length == 0) return;
        if (Event.current.type == EventType.ScrollWheel && area.Contains(Event.current.mousePosition))
        { first += (int)Mathf.Sign(Event.current.delta.y); Event.current.Use(); }
        first = Mathf.Clamp(first, 0, text.Length - 1);
        float y = area.y;
        int last = first;
        for (int i = first; i < text.Length; i++)
        {
            float height = string.IsNullOrEmpty(text[i]) ? 12 : GameUi.ResultTextHeight(text[i], area.width);
            if (y + height > area.yMax - 34) break;
            GameUi.Text(new Rect(area.x, y, area.width, height), text[i], 18, GameUi.Cream, TextAnchor.UpperLeft, true);
            y += height + 3; last = i;
        }
        GameUi.Text(new Rect(area.x, area.yMax - 28, area.width, 28),
            $"{first + 1}–{last + 1} de {text.Length} · RePág / AvPág", 16, GameUi.Muted);
    }
}
