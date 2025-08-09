using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using DoTheRitualsYourselves.Lister.Rituals;
using DoTheRitualsYourselves.Extra;

namespace DoTheRitualsYourselves.Windows
{
    public class Dialog_AssignRole : Window
    {
        private const float WindowH = 700f;
        private const float PoolPanelW = 260f;
        private const float RoleColW = 220f;
        private const float Pad = 8f;
        private const float LRGap = 10f;
        private const float RowH = 34f;
        private const float RowGap = 3f;
        private const float HeaderH = 22f;
        private const float Portrait = 28f;

        private static readonly Color PrisonerColor = new Color(1f, 0.6f, 0.2f);
        private static readonly Color SlaveColor = new Color(1f, 0.9f, 0.3f);

        private readonly List<RitualRole> roles = new List<RitualRole>();
        private readonly Dictionary<string, Vector2> roleScrolls = new Dictionary<string, Vector2>();
        private readonly List<Pawn> poolAll = new List<Pawn>();
        private readonly List<Pawn> pool = new List<Pawn>();

        private Vector2 _poolScrollState = Vector2.zero;
        private Ritual ritual;
        private RitualExtraData extra;

        private static bool _dragging;
        private static Pawn _dragPawn;
        private static string _fromRoleId;
        private static int _fromIndex;
        private static Vector2 _dragOffset;

        public Dictionary<string, List<Pawn>> RoleLists => extra.roles;

        public override Vector2 InitialSize
        {
            get
            {
                int n = roles.Count;
                float rolesW = (n > 0) ? (n * RoleColW + (n + 1) * Pad) : 0f;
                float totalW = PoolPanelW + LRGap + rolesW + 32f;
                return new Vector2(totalW, WindowH);
            }
        }

        public Dialog_AssignRole(Ritual ritual)
        {
            forcePause = true;
            draggable = false;
            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;

            this.ritual = ritual;
            extra = ritual.GetRitualExtraData();

            roles.Add(null);
            if (!RoleLists.ContainsKey("none"))
                RoleLists["none"] = new List<Pawn>();
            roleScrolls["none"] = Vector2.zero;

            foreach (var role in ritual.Precept.behavior.def.roles)
            {
                roles.Add(role);
                if (!RoleLists.ContainsKey(role.id))
                    RoleLists[role.id] = new List<Pawn>();
                roleScrolls[role.id] = Vector2.zero;
            }
            RoleLists.RemoveAll(pair => pair.Key != "none" && !ritual.Precept.behavior.def.roles.Any(role => role.id == pair.Key));

            BuildPoolAll();
            ClearNotInPool();
            RebuildPool();
        }

        private void ClearNotInPool()
        {
            foreach (var role in RoleLists.Keys)
                RoleLists[role].RemoveAll(pawn => !poolAll.Contains(pawn));
        }

        private void BuildPoolAll()
        {
            poolAll.Clear();
            foreach (var p in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners)
            {
                if (p == null || p.Destroyed) continue;
                if (p.Faction != Faction.OfPlayer) continue;
                if (p.RaceProps == null || !p.RaceProps.Humanlike) continue;
                poolAll.Add(p);
            }
            poolAll.SortBy(p => p.LabelCap);
            poolAll.SortBy(p => p.IsSlave ? 1 : p.IsPrisoner ? 2 : 0);
        }

        private void RebuildPool()
        {
            var assigned = new HashSet<Pawn>();
            foreach (var kv in RoleLists)
            {
                var lst = kv.Value;
                for (int i = 0; i < lst.Count; i++) assigned.Add(lst[i]);
            }
            pool.Clear();
            for (int i = 0; i < poolAll.Count; i++)
                if (!assigned.Contains(poolAll[i])) pool.Add(poolAll[i]);
            pool.SortBy(p => p.LabelCap);
            pool.SortBy(p => p.IsSlave ? 1 : p.IsPrisoner ? 2 : 0);
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;

            var titleRect = new Rect(inRect.x, y, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "DoTheRitualsYourselves.UI.AssignRole.Label".Translate());
            Text.Font = GameFont.Small;
            y += titleRect.height + 4f;

            var descRect = new Rect(inRect.x, y, inRect.width, 60f);
            Widgets.Label(descRect, "DoTheRitualsYourselves.UI.AssignRole.Desc".Translate());
            y += descRect.height;

            float poolW = PoolPanelW;
            float rolesW = inRect.width - poolW - LRGap;

            var poolRect = new Rect(inRect.x, y, poolW, inRect.height - (y - inRect.y));
            var rolesRect = new Rect(poolRect.xMax + LRGap, y, rolesW, poolRect.height);

            DrawPoolPanel(poolRect);
            DrawRolesArea(rolesRect);
            DrawDragGhost();

            if (Event.current.type == EventType.MouseUp && _dragging)
            {
                _dragging = false;
                _dragPawn = null;
                _fromRoleId = null;
                Event.current.Use();
            }
        }

        private static int CalcInsertIndex(Rect inner, float scrollY, float rowTotalH, int count)
        {
            float localY = Event.current.mousePosition.y - inner.y + scrollY;
            int idx = Mathf.FloorToInt(localY / rowTotalH + 0.5f);
            if (idx < 0) idx = 0;
            if (idx > count) idx = count;
            return idx;
        }

        private void DrawPoolPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(6f);

            var head = new Rect(inner.x, inner.y, inner.width, HeaderH);
            Widgets.Label(head, "DoTheRitualsYourselves.UI.AssignRole.Unassigned".Translate());
            TooltipHandler.TipRegion(head, "DoTheRitualsYourselves.UI.AssignRole.Unassigned.Tip".Translate());

            inner.yMin += (HeaderH + 6f);

            if (_dragging && Mouse.IsOver(inner)) Widgets.DrawBox(inner, 2);

            float rowTotalH = RowH + RowGap;
            float contentH = pool.Count * rowTotalH;
            var view = new Rect(0f, 0f, inner.width - 16f, Mathf.Max(inner.height, contentH));
            Widgets.BeginScrollView(inner, ref _poolScrollState, view);

            float curY = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                var row = new Rect(view.x, curY, view.width, RowH);
                DrawPoolRow(row, pool[i], i);
                curY += rowTotalH;
            }

            Widgets.EndScrollView();

            if (_dragging && Mouse.IsOver(inner))
            {
                int hoverInsertIdx = CalcInsertIndex(inner, _poolScrollState.y, rowTotalH, pool.Count);
                float guideY = inner.y + (hoverInsertIdx * rowTotalH) - _poolScrollState.y;
                guideY = Mathf.Clamp(guideY, inner.y, inner.yMax);
                Widgets.DrawLineHorizontal(inner.x, guideY, inner.width);

                if (Event.current.type == EventType.MouseUp)
                {
                    TryDropIntoPool(hoverInsertIdx);
                    Event.current.Use();
                }
            }
        }

        private void DrawPoolRow(Rect row, Pawn pawn, int index)
        {
            if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);

            var iconR = new Rect(row.x + 4f, row.y + 3 + (RowH - Portrait) * 0.5f, Portrait, Portrait);
            Widgets.ThingIcon(iconR, pawn);

            var labelR = new Rect(iconR.xMax + 6f, row.y, row.width - (iconR.width + 10f), row.height);

            var old = GUI.color;
            var oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (pawn != null)
            {
                if (pawn.IsSlave) GUI.color = SlaveColor;
                else if (pawn.IsPrisonerOfColony) GUI.color = PrisonerColor;
            }
            Widgets.Label(labelR, pawn.LabelCap);
            GUI.color = old;
            Text.Anchor = oldAnchor;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && row.Contains(Event.current.mousePosition))
            {
                _dragging = true;
                _dragPawn = pawn;
                _fromRoleId = null;
                _fromIndex = index;
                _dragOffset = Event.current.mousePosition - new Vector2(row.x, row.y);
                Event.current.Use();
            }
        }

        private void DrawRolesArea(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(6f);

            int n = roles.Count;
            if (n <= 0) return;

            for (int i = 0; i < n; i++)
            {
                var role = roles[i];
                float x = inner.x + i * (RoleColW + Pad);
                var col = new Rect(x, inner.y, RoleColW, inner.height);
                DrawSingleRoleList(col, role);
            }
        }

        private void DrawSingleRoleList(Rect rect, RitualRole role)
        {
            Widgets.DrawMenuSection(rect);

            var head = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, HeaderH);
            Widgets.Label(head, role?.LabelCap ?? "DoTheRitualsYourselves.UI.AssignRole.None".Translate());

            var inner = rect.ContractedBy(6f);
            inner.yMin += (HeaderH + 6f);

            if (_dragging && Mouse.IsOver(inner)) Widgets.DrawBox(inner, 2);

            Vector2 scroll;
            if (!roleScrolls.TryGetValue(role?.id ?? "none", out scroll)) scroll = Vector2.zero;

            List<Pawn> list = RoleLists[role?.id ?? "none"];
            float rowTotalH = RowH + RowGap;
            float contentH = list.Count * rowTotalH;
            var view = new Rect(0f, 0f, inner.width - 16f, Mathf.Max(inner.height, contentH));
            Widgets.BeginScrollView(inner, ref scroll, view);

            float curY = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                var row = new Rect(view.x, curY, view.width, RowH);
                DrawPawnRow(row, list[i], role?.id ?? "none", i);
                curY += rowTotalH;
            }

            Widgets.EndScrollView();

            if (_dragging && Mouse.IsOver(inner))
            {
                int hoverInsertIdx = CalcInsertIndex(inner, scroll.y, rowTotalH, list.Count);
                float guideY = inner.y + (hoverInsertIdx * rowTotalH) - scroll.y;
                guideY = Mathf.Clamp(guideY, inner.y, inner.yMax);
                Widgets.DrawLineHorizontal(inner.x, guideY, inner.width);

                if (Event.current.type == EventType.MouseUp)
                {
                    TryDropIntoRole(role?.id ?? "none", hoverInsertIdx);
                    Event.current.Use();
                }
            }

            roleScrolls[role?.id ?? "none"] = scroll;
        }

        private void DrawPawnRow(Rect row, Pawn pawn, string roleId, int index)
        {
            if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);

            var iconR = new Rect(row.x + 4f, row.y + 3 + (RowH - Portrait) * 0.5f, Portrait, Portrait);
            Widgets.ThingIcon(iconR, pawn);

            var labelR = new Rect(iconR.xMax + 6f, row.y, row.width - (iconR.width + 10f), row.height);

            var old = GUI.color;
            var oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (pawn != null)
            {
                if (pawn.IsSlave) GUI.color = SlaveColor;
                else if (pawn.IsPrisonerOfColony) GUI.color = PrisonerColor;
            }
            Widgets.Label(labelR, pawn.LabelCap);
            GUI.color = old;
            Text.Anchor = oldAnchor;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && row.Contains(Event.current.mousePosition))
            {
                _dragging = true;
                _dragPawn = pawn;
                _fromRoleId = roleId;
                _fromIndex = index;
                _dragOffset = Event.current.mousePosition - new Vector2(row.x, row.y);
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && row.Contains(Event.current.mousePosition))
            {
                RoleLists[roleId].RemoveAt(index);
                RebuildPool();
                Event.current.Use();
            }
        }

        private void DrawDragGhost()
        {
            if (!_dragging || _dragPawn == null) return;
            if (Event.current.type != EventType.Repaint) return;

            string name = _dragPawn.LabelCap;
            Vector2 size = Text.CalcSize(name);
            float ghostW = 12f + Portrait + 6f + size.x + 12f;

            Vector2 pos = Event.current.mousePosition - _dragOffset;
            Rect ghost = new Rect(pos.x, pos.y, ghostW, RowH);

            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            Widgets.DrawMenuSection(ghost);

            Rect iconR = new Rect(ghost.x + 4f, ghost.y + 3f, Portrait, Portrait);
            Widgets.ThingIcon(iconR, _dragPawn);

            Rect labelR = new Rect(iconR.xMax + 6f, ghost.y, ghost.width - (iconR.width + 10f), ghost.height);

            var old = GUI.color;
            if (_dragPawn.IsSlave) GUI.color = SlaveColor;
            else if (_dragPawn.IsPrisonerOfColony) GUI.color = PrisonerColor;
            Widgets.Label(labelR, name);
            GUI.color = old;

            GUI.color = Color.white;
            GUI.DrawTexture(ghost, TexUI.HighlightTex);
        }

        private void TryDropIntoRole(string targetRoleId, int insertIndex)
        {
            if (_dragPawn == null) { _dragging = false; return; }
            List<Pawn> target = RoleLists[targetRoleId];

            if (_fromRoleId != null && _fromRoleId == targetRoleId)
            {
                int curIdx = target.IndexOf(_dragPawn);
                if (curIdx >= 0)
                {
                    target.RemoveAt(curIdx);
                    if (insertIndex > curIdx) insertIndex--;
                    insertIndex = Mathf.Clamp(insertIndex, 0, target.Count);
                    target.Insert(insertIndex, _dragPawn);
                }
            }
            else
            {
                if (_fromRoleId == null)
                {
                    if (_fromIndex >= 0 && _fromIndex < pool.Count && pool[_fromIndex] == _dragPawn)
                        pool.RemoveAt(_fromIndex);
                    else
                        pool.Remove(_dragPawn);
                }
                else
                {
                    RoleLists[_fromRoleId].Remove(_dragPawn);
                }

                if (insertIndex < 0) insertIndex = target.Count;
                insertIndex = Mathf.Clamp(insertIndex, 0, target.Count);
                if (!target.Contains(_dragPawn))
                    target.Insert(insertIndex, _dragPawn);
            }

            RebuildPool();

            _dragging = false;
            _dragPawn = null;
            _fromRoleId = null;
        }

        private void TryDropIntoPool(int insertIndex)
        {
            if (_dragPawn == null) { _dragging = false; return; }

            if (_fromRoleId != null)
            {
                RoleLists[_fromRoleId].Remove(_dragPawn);
            }
            else
            {
                int cur = pool.IndexOf(_dragPawn);
                if (cur >= 0)
                {
                    pool.RemoveAt(cur);
                    if (insertIndex > cur) insertIndex--;
                    insertIndex = Mathf.Clamp(insertIndex, 0, pool.Count);
                    pool.Insert(insertIndex, _dragPawn);
                    _dragging = false;
                    _dragPawn = null;
                    _fromRoleId = null;
                    return;
                }
            }

            insertIndex = Mathf.Clamp(insertIndex, 0, pool.Count);
            if (!pool.Contains(_dragPawn))
                pool.Insert(insertIndex, _dragPawn);

            _dragging = false;
            _dragPawn = null;
            _fromRoleId = null;
        }
    }
}
