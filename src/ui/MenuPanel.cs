using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using GTA;
using GTA.UI;

namespace GTA5Buddy
{
    /// <summary>
    /// The in-game control panel.
    ///
    /// RENDERING, and why it is split this way:
    ///
    ///   * Graphics (background, header bar, accent stripe, selection highlight, border,
    ///     scrollbar, separator) are drawn with the DRAW_RECT native - verified to render on
    ///     this setup, and independent of every wrapper property that misbehaved here
    ///     (ContainerElement drew nothing, TextElement.Position was ignored).
    ///
    ///   * Text is drawn as ONE multi-line TextElement, because that is the form that was
    ///     confirmed to lay out correctly after the pile-up bug. Rows are therefore separated
    ///     by real newlines and prefixed with a marker; the graphics sit behind them at
    ///     matching row positions.
    ///
    /// All elements are allocated once and only mutated per frame, so drawing costs no garbage.
    /// </summary>
    internal sealed class MenuPanel
    {
        private const int MaxVisibleRows = 12;

        // ---- layout in 0..1 screen space ----
        private const float PanelWidth = 0.38f;
        private const float Pad = 0.008f;
        private const float HeaderHeight = 0.044f;
        private const float RowHeight = 0.027f;
        private const float HintHeight = 0.030f;
        private const float ScrollbarWidth = 0.004f;

        // ---- palette ----
        private static readonly Color BackColor = Color.FromArgb(242, 10, 13, 20);
        private static readonly Color HeaderColor = Color.FromArgb(252, 26, 58, 104);
        private static readonly Color AccentColor = Color.FromArgb(255, 90, 150, 235);
        private static readonly Color SelectionColor = Color.FromArgb(215, 42, 92, 160);
        private static readonly Color BorderColor = Color.FromArgb(120, 90, 130, 190);

        private readonly string _title;
        private readonly List<string> _rows = new List<string>();
        private readonly List<Action> _actions = new List<Action>();
        private readonly List<string> _footers = new List<string>();

        private readonly TextElement _bodyText;
        private readonly TextElement _hintText;
        private readonly StringBuilder _sb = new StringBuilder(2048);

        private int _selected;
        private int _scroll;

        internal MenuPanel(string title, string hint)
        {
            _title = title;

            _bodyText = new TextElement(string.Empty, new PointF(0f, 0f), 0.34f)
            {
                Color = Color.FromArgb(255, 242, 246, 255),
                Outline = true,
            };

            _hintText = new TextElement(hint, new PointF(0f, 0f), 0.30f)
            {
                Color = Color.FromArgb(255, 205, 218, 236),
                Outline = true,
            };

            ApplyStyle();
        }

        internal void ApplyStyle()
        {
            try
            {
                GTA.UI.Font f = BuddyIni.ResolveFont();
                float s = BuddyIni.PanelScale <= 0f ? 1f : BuddyIni.PanelScale;

                _bodyText.Font = f;
                _bodyText.Scale = 0.34f * s;

                _hintText.Font = f;
                _hintText.Scale = 0.30f * s;
            }
            catch (Exception) { }
        }

        internal int Count { get { return _rows.Count; } }
        internal int SelectedIndex { get { return _selected; } }
        internal string Title { get { return _title; } }
        internal string Hint { get { return _hintText.Caption; } set { _hintText.Caption = value ?? string.Empty; } }

        /// <summary>Overrides the drawn heading without disturbing the stored title.</summary>
        internal string TitleOverride { get; set; }

        internal string SelectedRow
        {
            get { return _rows.Count == 0 ? string.Empty : _rows[_selected]; }
        }

        internal void Clear()
        {
            _rows.Clear();
            _actions.Clear();
            _footers.Clear();
            _selected = 0;
            _scroll = 0;
        }

        internal void Add(string row, Action action, string footer)
        {
            _rows.Add(row ?? string.Empty);
            _actions.Add(action);
            _footers.Add(footer ?? string.Empty);
        }

        internal void Select(int index)
        {
            _selected = index;
            Normalize();
        }

        /// <summary>Rewrites the caption of an existing row (folds a count into a header row).</summary>
        internal void SetRow(int index, string row)
        {
            if (index < 0 || index >= _rows.Count) return;
            _rows[index] = row ?? string.Empty;
        }

        internal void Normalize()
        {
            if (_rows.Count == 0) { _selected = 0; _scroll = 0; return; }
            if (_selected >= _rows.Count) _selected = _rows.Count - 1;
            if (_selected < 0) _selected = 0;
            EnsureVisible();
        }

        internal void MoveSelection(int delta)
        {
            if (_rows.Count == 0) return;
            _selected += delta;
            if (_selected < 0) _selected = _rows.Count - 1;
            if (_selected >= _rows.Count) _selected = 0;
            EnsureVisible();
        }

        internal void ActivateSelected()
        {
            if (_rows.Count == 0) return;
            Action a = _actions[_selected];
            if (a != null) a();
        }

        private void EnsureVisible()
        {
            if (_selected < _scroll) _scroll = _selected;
            if (_selected >= _scroll + MaxVisibleRows) _scroll = _selected - MaxVisibleRows + 1;
            if (_scroll < 0) _scroll = 0;
        }

        internal void Draw()
        {
            float x = BuddyIni.PanelX;
            float y = BuddyIni.PanelY;

            int visible = _rows.Count == 0 ? 1 : Math.Min(MaxVisibleRows, _rows.Count);
            float bodyHeight = (visible * RowHeight) + 0.008f;
            float panelHeight = HeaderHeight + bodyHeight + HintHeight;

            DrawChrome(x, y, panelHeight, visible);
            DrawBody(x, y, visible);
            DrawHint(x, y, bodyHeight);
        }

        // ------------------------------------------------------------------
        // graphics
        // ------------------------------------------------------------------

        private void DrawChrome(float x, float y, float panelHeight, int visible)
        {
            // border: four thin rects just outside the body
            float bx = x - 0.002f, by = y - 0.002f;
            float bw = PanelWidth + 0.004f, bh = panelHeight + 0.004f;
            HudDraw.Rect(bx, by, bw, 0.002f, BorderColor);                       // top
            HudDraw.Rect(bx, by + bh - 0.002f, bw, 0.002f, BorderColor);         // bottom
            HudDraw.Rect(bx, by, 0.002f, bh, BorderColor);                        // left
            HudDraw.Rect(bx + bw - 0.002f, by, 0.002f, bh, BorderColor);          // right

            // main body + darker header
            HudDraw.Rect(x, y, PanelWidth, panelHeight, BackColor);
            HudDraw.Rect(x, y, PanelWidth, HeaderHeight, HeaderColor);

            // accent stripe under the header
            HudDraw.Rect(x, y + HeaderHeight - 0.0025f, PanelWidth, 0.0025f, AccentColor);

            // selection highlight, aligned to the row the marker sits on
            int slot = _selected - _scroll;
            if (_rows.Count > 0 && slot >= 0 && slot < MaxVisibleRows)
            {
                float rowY = y + HeaderHeight + (slot * RowHeight);
                HudDraw.Rect(x, rowY - 0.001f, PanelWidth, RowHeight, SelectionColor);
                // left marker bar on the selected row
                HudDraw.Rect(x, rowY - 0.001f, 0.004f, RowHeight, AccentColor);
            }

            // scrollbar when the list overflows
            if (_rows.Count > MaxVisibleRows)
            {
                float trackY = y + HeaderHeight;
                float trackH = visible * RowHeight;
                HudDraw.Rect(x + PanelWidth - ScrollbarWidth - 0.002f, trackY, ScrollbarWidth, trackH,
                             Color.FromArgb(150, 30, 40, 60));

                float thumbH = trackH * ((float)MaxVisibleRows / _rows.Count);
                if (thumbH < 0.012f) thumbH = 0.012f;
                float maxScroll = Math.Max(1, _rows.Count - MaxVisibleRows);
                float thumbY = trackY + (trackH - thumbH) * ((float)_scroll / maxScroll);

                HudDraw.Rect(x + PanelWidth - ScrollbarWidth - 0.002f, thumbY, ScrollbarWidth, thumbH,
                             AccentColor);
            }
        }

        private void DrawBody(float x, float y, int visible)
        {
            // header caption, drawn as part of the body element's first line for reliability
            string heading = string.IsNullOrEmpty(TitleOverride) ? _title : TitleOverride;
            if (_rows.Count > MaxVisibleRows)
            {
                heading += "   " + (_selected + 1) + "/" + _rows.Count;
            }

            _sb.Length = 0;
            _sb.Append(heading).Append('\n');

            for (int i = 0; i < visible; i++)
            {
                int index = _scroll + i;
                if (index >= _rows.Count) break;

                bool selected = (index == _selected);
                _sb.Append(selected ? "> " : "  ");

                string line = _rows[index];
                string footer = _footers[index];
                if (!string.IsNullOrEmpty(footer))
                {
                    line = line + "  |  " + footer;
                }
                _sb.Append(Truncate(line, 44));

                if (i < visible - 1) _sb.Append('\n');
            }

            _bodyText.Caption = _sb.ToString();
            _bodyText.Position = new PointF(x + Pad, y + 0.010f);
            _bodyText.Draw();
        }

        private void DrawHint(float x, float y, float bodyHeight)
        {
            // separator above the hint line
            float sepY = y + HeaderHeight + bodyHeight;
            HudDraw.Rect(x, sepY, PanelWidth, 0.0015f, Color.FromArgb(110, 80, 110, 160));

            _hintText.Position = new PointF(x + Pad, sepY + 0.004f);
            _hintText.Draw();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max - 1) + "\u2026";
        }
    }
}
