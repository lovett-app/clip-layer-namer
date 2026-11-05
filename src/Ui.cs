// 화면 부품 — 기본 WinForms 단추가 투박해서 직접 그림(둥근 칩 · 방향 고르기 · 스위치 · 입력칸 · 카드).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class Theme
{
    public static readonly Color Bg = Color.FromArgb(244, 244, 247);
    public static readonly Color Card = Color.White;
    public static readonly Color Line = Color.FromArgb(228, 228, 234);
    public static readonly Color Ink = Color.FromArgb(31, 32, 48);
    public static readonly Color Ink2 = Color.FromArgb(107, 110, 128);
    public static readonly Color Ink3 = Color.FromArgb(160, 163, 177);
    public static readonly Color Accent = Color.FromArgb(99, 102, 241);
    public static readonly Color AccentDeep = Color.FromArgb(79, 70, 229);
    public static readonly Color AccentSoft = Color.FromArgb(238, 240, 255);
    public static readonly Color AccentLine = Color.FromArgb(199, 203, 251);
    public static readonly Color Track = Color.FromArgb(236, 236, 242);
    public static readonly Color Off = Color.FromArgb(213, 214, 222);
    public static readonly Color Danger = Color.FromArgb(209, 67, 67);
    public static float S = 1f; // 화면 배율(dpi/96)

    public static string Family;
    public static Font Ui, Small, Bold, SmallBold;
    public static void Init(float dpiScale, string lang)
    {
        S = dpiScale;
        // 언어마다 그 글자가 예쁘게 나오는 윈도우 글꼴
        string semi = null;
        if (lang == "ja") Family = Has("Yu Gothic UI") ? "Yu Gothic UI" : Has("Meiryo UI") ? "Meiryo UI" : "MS UI Gothic";
        else if (lang == "en") Family = "Segoe UI";
        else
        {
            Family = Has("Pretendard") ? "Pretendard" : "맑은 고딕";
            semi = Has("Pretendard SemiBold") ? "Pretendard SemiBold" : null;
        }
        Ui = new Font(Family, 9.5f);
        Small = new Font(Family, 8.5f);
        Bold = semi != null ? new Font(semi, 9.5f) : new Font(Family, 9.5f, FontStyle.Bold);
        SmallBold = semi != null ? new Font(semi, 8.5f) : new Font(Family, 8.5f, FontStyle.Bold);
    }
    static bool Has(string name)
    {
        foreach (var f in FontFamily.Families) if (f.Name == name) return true;
        return false;
    }
    public static int P(float px) { return (int)Math.Round(px * S); }

    public static GraphicsPath Round(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
        if (d <= 0.5f) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
    public static void Box(Graphics g, Rectangle bounds, float rad, Color fill, Color line)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1f, bounds.Height - 1f);
        using (var path = Round(r, rad))
        {
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            if (line != Color.Empty) using (var pen = new Pen(line, Math.Max(1f, S))) g.DrawPath(pen, path);
        }
    }
    public static Color Mix(Color a, Color b, float t)
    {
        return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
}

class Drawn : Control
{
    protected bool hover, down;
    public Drawn()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
    }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected void Text0(Graphics g, string s, Font f, Color c, Rectangle r, TextFormatFlags extra)
    {
        TextRenderer.DrawText(g, s, f, r, c, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | extra);
    }
}

enum ChipKind { Name, Ghost, Link }

class Chip : Drawn
{
    public ChipKind Kind = ChipKind.Name;
    float flash = 0f;
    Timer flashTimer;

    public Chip(string text, ChipKind kind)
    {
        Text = text;
        Kind = kind;
        Font = kind == ChipKind.Link ? Theme.SmallBold : Theme.Ui;
    }
    public int PreferredWidth(int height)
    {
        int pad = Kind == ChipKind.Link ? Theme.P(8) : Theme.P(12);
        return TextRenderer.MeasureText(Text, Font, new Size(9999, height), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + pad * 2;
    }
    public void Flash()
    {
        flash = 1f;
        if (flashTimer == null)
        {
            flashTimer = new Timer { Interval = 30 };
            flashTimer.Tick += delegate { flash -= 0.06f; if (flash <= 0) { flash = 0; flashTimer.Stop(); } Invalidate(); };
        }
        flashTimer.Start();
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        float rad = Theme.P(Kind == ChipKind.Name ? 9 : 8);
        Color fill, line, ink;
        if (Kind == ChipKind.Link)
        {
            fill = down ? Theme.AccentLine : hover ? Theme.AccentSoft : BackColor;
            line = Color.Empty; ink = Theme.Accent;
        }
        else if (Kind == ChipKind.Ghost)
        {
            fill = down ? Theme.Track : hover ? Color.FromArgb(248, 248, 251) : Theme.Card;
            line = hover ? Theme.Ink3 : Theme.Line; ink = Theme.Ink;
        }
        else
        {
            fill = down ? Theme.AccentLine : hover ? Theme.AccentSoft : Theme.Card;
            line = hover || down ? Theme.AccentLine : Theme.Line;
            ink = hover || down ? Theme.AccentDeep : Theme.Ink;
        }
        if (flash > 0)
        {
            fill = Theme.Mix(fill, Theme.Accent, flash);
            line = Theme.Mix(line == Color.Empty ? fill : line, Theme.Accent, flash);
            ink = Theme.Mix(ink, Color.White, flash);
        }
        Theme.Box(g, ClientRectangle, rad, fill, line);
        Text0(g, Text, Font, ink, ClientRectangle, TextFormatFlags.HorizontalCenter);
    }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
}

class Segmented : Drawn
{
    string[] items;
    int sel = 0, hot = -1;
    public event EventHandler Changed;
    public Segmented(string[] items) { this.items = items; Font = Theme.Bold; }
    public void SetItems(string[] next) { items = next; Invalidate(); }
    // 칸마다 가장 긴 글자가 들어가는 폭
    public int PreferredWidth()
    {
        int m = 0;
        foreach (var s in items) m = Math.Max(m, TextRenderer.MeasureText(s, Font, new Size(9999, 99), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width);
        return items.Length * (m + Theme.P(20)) + Theme.P(6);
    }
    public int SelectedIndex
    {
        get { return sel; }
        set { if (value == sel) return; sel = value; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); }
    }
    int At(int x) { return Math.Max(0, Math.Min(items.Length - 1, x * items.Length / Math.Max(1, Width))); }
    protected override void OnMouseMove(MouseEventArgs e) { int h = At(e.X); if (h != hot) { hot = h; Invalidate(); } base.OnMouseMove(e); }
    protected override void OnMouseLeave(EventArgs e) { hot = -1; base.OnMouseLeave(e); }
    protected override void OnMouseClick(MouseEventArgs e) { SelectedIndex = At(e.X); base.OnMouseClick(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        Theme.Box(g, ClientRectangle, Theme.P(9), Theme.Track, Color.Empty);
        int pad = Theme.P(3);
        float w = (Width - pad * 2) / (float)items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            var r = new Rectangle((int)(pad + w * i), pad, (int)w, Height - pad * 2);
            if (i == sel)
            {
                var sh = r; sh.Offset(0, 1);
                Theme.Box(g, sh, Theme.P(7), Color.FromArgb(224, 224, 232), Color.Empty);
                Theme.Box(g, r, Theme.P(7), Theme.Card, Color.Empty);
            }
            var ink = i == sel ? Theme.Accent : i == hot ? Theme.Ink : Theme.Ink2;
            Text0(g, items[i], Font, ink, r, TextFormatFlags.HorizontalCenter);
        }
    }
}

class Toggle : Drawn
{
    bool on;
    public event EventHandler Changed;
    public Toggle(string text, bool initial) { Text = text; on = initial; Font = Theme.Ui; }
    public bool Checked
    {
        get { return on; }
        set { if (value == on) return; on = value; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); }
    }
    public int PreferredWidth()
    {
        return Theme.P(34) + Theme.P(8) + TextRenderer.MeasureText(Text, Font, new Size(9999, 99), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + 2;
    }
    protected override void OnMouseClick(MouseEventArgs e) { Checked = !Checked; base.OnMouseClick(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        int tw = Theme.P(34), th = Theme.P(20), ty = (Height - th) / 2;
        var track = new Rectangle(0, ty, tw, th);
        var tc = on ? (hover ? Theme.AccentDeep : Theme.Accent) : (hover ? Theme.Ink3 : Theme.Off);
        Theme.Box(g, track, th / 2f, tc, Color.Empty);
        int k = th - Theme.P(4);
        var knob = new Rectangle(on ? tw - k - Theme.P(2) : Theme.P(2), ty + Theme.P(2), k, k);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, knob);
        var tr = new Rectangle(tw + Theme.P(8), 0, Width - tw - Theme.P(8), Height);
        Text0(g, Text, Font, Theme.Ink, tr, TextFormatFlags.Left);
    }
}

class Field : Control
{
    public readonly TextBox Box;
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);

    public Field(string cue)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Box = new TextBox { BorderStyle = BorderStyle.None, Font = Theme.Ui, BackColor = Theme.Card, ForeColor = Theme.Ink };
        Controls.Add(Box);
        Box.GotFocus += delegate { Invalidate(); };
        Box.LostFocus += delegate { Invalidate(); };
        Box.HandleCreated += delegate { SendMessage(Box.Handle, 0x1501, (IntPtr)1, cue); }; // EM_SETCUEBANNER
        Cursor = Cursors.IBeam;
    }
    protected override void OnClick(EventArgs e) { Box.Focus(); base.OnClick(e); }
    protected override void OnLayout(LayoutEventArgs e)
    {
        int px = Theme.P(10);
        Box.SetBounds(px, (Height - Box.PreferredHeight) / 2, Width - px * 2, Box.PreferredHeight);
        base.OnLayout(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        bool f = Box.Focused;
        Theme.Box(e.Graphics, ClientRectangle, Theme.P(8), Theme.Card, f ? Theme.Accent : Theme.Line);
    }
}

class Card : Panel
{
    public Card() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
        Theme.Box(e.Graphics, ClientRectangle, Theme.P(14), Theme.Card, Theme.Line);
    }
}

// 미리보기 줄: "누르면" + 붙을 이름
class Pill : Control
{
    public string Caption = "", Value = "";
    public bool Muted;
    public Pill() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); }
    public void Set(string caption, string value, bool muted) { Caption = caption; Value = value; Muted = muted; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        Theme.Box(g, ClientRectangle, Theme.P(10), Muted ? Theme.Track : Theme.AccentSoft, Color.Empty);
        var fl = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        int x = Theme.P(14);
        if (Caption != "")
        {
            int cw = TextRenderer.MeasureText(Caption, Theme.Small, new Size(9999, Height), fl).Width;
            TextRenderer.DrawText(g, Caption, Theme.Small, new Rectangle(x, 0, cw + 2, Height), Muted ? Theme.Ink3 : Theme.Ink2, fl);
            x += cw + Theme.P(10);
        }
        TextRenderer.DrawText(g, Value, Muted ? Theme.Ui : Theme.Bold, new Rectangle(x, 0, Width - x - Theme.P(12), Height), Muted ? Theme.Ink3 : Theme.AccentDeep, fl);
    }
}
