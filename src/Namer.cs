// 클튜 레이어 이름 붙이기 — 라투디 파츠 분리용.
// 클튜 옆에 띄워 두고, 클튜에서 레이어를 고른 뒤 이 창의 이름 버튼을 누르면
// 클튜로 돌아가 [레이어 이름 변경] 단축키 → 전체 선택 → 붙여넣기 → Enter 를 대신 누른다.
// 클튜 공식 플러그인은 필터만 만들 수 있어서 레이어 이름을 직접 못 건드림 → 키 입력으로 대신함.
// 이름 바꾸기 칸이 안 뜨면(단축키 안 붙음 등) 아무것도 안 누르고 멈춤 — 캔버스에 Ctrl+A/V 가 들어가지 않게.
// C# 5 (윈도우 기본 csc) 로 빌드: build.cmd
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

static class Native
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint tid, ref GUITHREADINFO info);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);
    [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int h2, uint flags);
    public const int GWL_EXSTYLE = -20, WS_EX_TOPMOST = 8;
    public const uint SWP_KEEP = 0x0002 | 0x0001 | 0x0010; // NOMOVE · NOSIZE · NOACTIVATE
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int l, t, r, b; }
    [StructLayout(LayoutKind.Sequential)]
    public struct GUITHREADINFO
    {
        public int cbSize, flags;
        public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
        public RECT rcCaret;
    }
    public const uint KEYUP = 2;
}

class Hotkey
{
    public bool Ctrl, Alt, Shift;
    public byte Vk;
    public string Text;
    public static Hotkey Parse(string s)
    {
        var k = new Hotkey();
        k.Text = s.Trim();
        foreach (var raw in k.Text.Split('+'))
        {
            var p = raw.Trim().ToUpperInvariant();
            if (p == "") continue;
            if (p == "CTRL" || p == "CONTROL") k.Ctrl = true;
            else if (p == "ALT") k.Alt = true;
            else if (p == "SHIFT") k.Shift = true;
            else if (p.Length == 1 && ((p[0] >= 'A' && p[0] <= 'Z') || (p[0] >= '0' && p[0] <= '9'))) k.Vk = (byte)p[0];
            else if (p == "[") k.Vk = 0xDB;
            else if (p == "]") k.Vk = 0xDD;
            else if (p.StartsWith("F") && p.Length <= 3) { int n; if (int.TryParse(p.Substring(1), out n) && n >= 1 && n <= 12) k.Vk = (byte)(0x6F + n); }
            else if (p == "ENTER") k.Vk = 0x0D;
        }
        return k.Vk == 0 ? null : k;
    }
    public void Press()
    {
        if (Ctrl) Native.keybd_event(0x11, 0, 0, UIntPtr.Zero);
        if (Alt) Native.keybd_event(0x12, 0, 0, UIntPtr.Zero);
        if (Shift) Native.keybd_event(0x10, 0, 0, UIntPtr.Zero);
        Native.keybd_event(Vk, 0, 0, UIntPtr.Zero);
        Native.keybd_event(Vk, 0, Native.KEYUP, UIntPtr.Zero);
        if (Shift) Native.keybd_event(0x10, 0, Native.KEYUP, UIntPtr.Zero);
        if (Alt) Native.keybd_event(0x12, 0, Native.KEYUP, UIntPtr.Zero);
        if (Ctrl) Native.keybd_event(0x11, 0, Native.KEYUP, UIntPtr.Zero);
    }
}

class Settings
{
    public string Lang = "ko";
    public string RenameKey = "Ctrl+Alt+R";
    public string NextKey = "Alt+[";
    public int Wait = 1200;
    public int Settle = 120;
    public string Format = "{앞}{이름}{앞뒤}{번호}{방향}{뒤}";
    public string Left = "<";
    public string Right = ">";
    public string Front = "_앞";
    public string Back = "_뒤";
    public bool NoCheck = false;
    public string Target = "CLIPStudioPaint";
    public string Path;

    public static Settings Load(string path)
    {
        var s = new Settings();
        s.Path = path;
        if (!File.Exists(path))
        {
            // 처음 켤 때: 윈도우 언어를 따라감
            s.Lang = global::Lang.Detect();
            s.Front = global::Lang.Front(s.Lang);
            s.Back = global::Lang.Back(s.Lang);
            s.Save();
            return s;
        }
        bool old = true; // 앞쪽/뒤쪽 이 생기기 전 설정 파일
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            var t = line.Trim();
            if (t == "" || t.StartsWith("#") || t.StartsWith(";")) continue;
            int i = t.IndexOf('=');
            if (i < 0) continue;
            var key = t.Substring(0, i).Trim();
            var val = t.Substring(i + 1).Trim();
            int n;
            switch (key)
            {
                case "언어": s.Lang = global::Lang.Codes[global::Lang.Index(val)]; break;
                case "이름바꾸기단축키": s.RenameKey = val; break;
                case "아래레이어단축키": s.NextKey = val; break;
                case "기다림": if (int.TryParse(val, out n)) s.Wait = n; break;
                case "여유": if (int.TryParse(val, out n)) s.Settle = n; break;
                case "이름모양": s.Format = val; break;
                case "왼쪽": s.Left = val; break;
                case "오른쪽": s.Right = val; break;
                case "앞쪽": s.Front = val; old = false; break;
                case "뒤쪽": s.Back = val; old = false; break;
                case "확인안함": s.NoCheck = val == "1"; break;
                case "대상프로그램": s.Target = val; break;
            }
        }
        if (old)
        {
            // 옛 이름 모양엔 {앞뒤} 가 없어서 골라도 안 붙음 → 이름 바로 뒤에 넣어 줌. < > 앞 띄어쓰기도 뺌
            if (!s.Format.Contains("{앞뒤}")) s.Format = s.Format.Contains("{이름}") ? s.Format.Replace("{이름}", "{이름}{앞뒤}") : s.Format + "{앞뒤}";
            s.Format = s.Format.Replace(" {방향}", "{방향}");
            s.Save();
        }
        return s;
    }
    // 이제 설정은 프로그램 안 [설정] 창에서 고침 — 파일은 저장용
    public void Save()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Layer Namer");
        sb.AppendLine("언어=" + Lang);
        sb.AppendLine("이름바꾸기단축키=" + RenameKey);
        sb.AppendLine("아래레이어단축키=" + NextKey);
        sb.AppendLine("기다림=" + Wait);
        sb.AppendLine("여유=" + Settle);
        sb.AppendLine("이름모양=" + Format);
        sb.AppendLine("왼쪽=" + Left);
        sb.AppendLine("오른쪽=" + Right);
        sb.AppendLine("앞쪽=" + Front);
        sb.AppendLine("뒤쪽=" + Back);
        sb.AppendLine("확인안함=" + (NoCheck ? "1" : "0"));
        sb.AppendLine("대상프로그램=" + Target);
        File.WriteAllText(Path, sb.ToString(), new UTF8Encoding(true));
    }
    public Settings Copy() { return (Settings)MemberwiseClone(); }
}

class NamerForm : Form
{
    readonly string dir;
    readonly string namesPath, cfgPath;
    Settings cfg;
    readonly Dictionary<string, int> counters = new Dictionary<string, int>();
    string side = "";
    int depth = 0; // 0 없음 · 1 앞쪽 · 2 뒤쪽
    string lastBase = null;
    bool busy = false;

    Field preF, sufF, freeF, countF;
    Label countL, countU;
    Toggle numT, nextT;
    Segmented sideSeg, depthSeg;
    Chip flipC, resetC, editNamesC, cfgC;
    Label preL, sufL, status, credit;
    Pill preview;
    Card card;
    Panel scroll;
    Chip pressed;
    bool statusErr;
    const string SideNone = "", SideLeft = "L", SideRight = "R";

    TextBox preBox { get { return preF.Box; } }
    TextBox sufBox { get { return sufF.Box; } }
    Toggle numChk { get { return numT; } }
    Toggle nextChk { get { return nextT; } }

    public NamerForm()
    {
        dir = AppDomain.CurrentDomain.BaseDirectory;
        namesPath = Path.Combine(dir, "names.txt");
        cfgPath = Path.Combine(dir, "settings.txt");
        Rename(Path.Combine(dir, "이름목록.txt"), namesPath); // 옛 파일 이름
        Rename(Path.Combine(dir, "설정.txt"), cfgPath);
        cfg = Settings.Load(cfgPath);
        Lang.Code = cfg.Lang;
        if (!File.Exists(namesPath)) File.WriteAllText(namesPath, Lang.DefaultNames(cfg.Lang), new UTF8Encoding(true));

        // CreateGraphics 는 창을 미리 만들어 버려서 TopMost 가 안 먹음 → 화면 쪽에서 재기
        using (var g = Graphics.FromHwnd(IntPtr.Zero)) Theme.Init(g.DpiX / 96f, cfg.Lang);
        Text = Lang.T("레이어 이름 붙이기", "レイヤー名つけ", "Layer Namer");
        var ico = typeof(NamerForm).Assembly.GetManifestResourceStream("icon.ico");
        if (ico != null) using (ico) Icon = new Icon(ico);
        TopMost = true;
        AutoScaleMode = AutoScaleMode.None;
        Font = Theme.Ui;
        BackColor = Theme.Bg;
        DoubleBuffered = true;
        StartPosition = FormStartPosition.Manual;
        var wa = Screen.PrimaryScreen.WorkingArea;
        Size = new Size(Theme.P(360), Math.Min(wa.Height - Theme.P(40), Theme.P(720)));
        MinimumSize = new Size(Theme.P(300), Theme.P(480));
        Location = new Point(wa.Right - Width - Theme.P(8), wa.Top + Theme.P(40));
        if (Keep != Rectangle.Empty) Bounds = Keep; // 언어를 바꿔 다시 열 때 그 자리 그대로

        card = new Card { BackColor = Theme.Card };
        preL = Cap(Lang.T("앞에 붙일 글자", "前につける文字", "Prefix"));
        sufL = Cap(Lang.T("뒤에 붙일 글자", "後ろにつける文字", "Suffix"));
        preF = new Field(Lang.T("예: ", "例: ", "e.g. ") + "A_") { BackColor = Theme.Card };
        sufF = new Field(Lang.T("예: ", "例: ", "e.g. ") + "_1") { BackColor = Theme.Card };
        preBox.TextChanged += delegate { UpdatePreview(null); };
        sufBox.TextChanged += delegate { UpdatePreview(null); };
        sideSeg = new Segmented(new[] { None(), "<", ">" }) { BackColor = Theme.Card };
        sideSeg.Changed += delegate { side = sideSeg.SelectedIndex == 1 ? SideLeft : sideSeg.SelectedIndex == 2 ? SideRight : SideNone; UpdatePreview(null); };
        depthSeg = new Segmented(DepthItems()) { BackColor = Theme.Card };
        depthSeg.Changed += delegate { depth = depthSeg.SelectedIndex; UpdatePreview(null); };
        flipC = new Chip("⇄  " + Lang.T("반대쪽 한 번 더", "反対側ももう一回", "Other side too"), ChipKind.Ghost) { BackColor = Theme.Card };
        flipC.Click += delegate { FlipAgain(); };
        numT = new Toggle(Lang.T("번호 붙이기", "番号をつける", "Add number"), false) { BackColor = Theme.Card };
        numT.Changed += delegate { UpdatePreview(null); };
        resetC = new Chip(Lang.T("1부터 다시", "1から数え直す", "Restart at 1"), ChipKind.Link) { BackColor = Theme.Card };
        resetC.Click += delegate { counters.Clear(); UpdatePreview(null); Say(Lang.T("번호를 1부터 다시 셉니다.", "番号を1から数え直します。", "Numbering restarts at 1."), false); };
        nextT = new Toggle(Lang.T("붙이고 아래 레이어로", "つけたら下のレイヤーへ", "Then move to layer below"), true) { BackColor = Theme.Card };
        countL = new Label { Text = Lang.T("한 번에", "まとめて", "Batch"), AutoSize = true, Font = Theme.Small, ForeColor = Theme.Ink2, BackColor = Theme.Card };
        countU = new Label { Text = Lang.T("개", "個", "layers"), AutoSize = true, Font = Theme.Small, ForeColor = Theme.Ink2, BackColor = Theme.Card };
        countF = new Field("1") { BackColor = Theme.Card };
        countF.Box.TextAlign = HorizontalAlignment.Center;
        countF.Box.MaxLength = 2;
        countF.Box.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
        countF.Box.TextChanged += delegate { UpdatePreview(null); };
        card.Controls.AddRange(new Control[] { preL, sufL, preF, sufF, sideSeg, depthSeg, flipC, numT, resetC, nextT, countL, countF, countU });

        preview = new Pill { BackColor = Theme.Bg };
        scroll = new Panel { AutoScroll = true, BackColor = Theme.Bg };
        scroll.Resize += delegate { LayoutChips(); };

        freeF = new Field(Lang.T("직접 쓰고 Enter", "直接入力して Enter", "Type a name, press Enter")) { BackColor = Theme.Bg };
        freeF.Box.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && freeF.Box.Text.Trim() != "")
            {
                e.SuppressKeyPress = true;
                pressed = null;
                Apply(freeF.Box.Text.Trim());
            }
        };
        editNamesC = new Chip(Lang.T("이름 목록", "名前リスト", "Names"), ChipKind.Ghost) { BackColor = Theme.Bg };
        editNamesC.Click += delegate { EditNames(); };
        cfgC = new Chip(Lang.T("설정", "設定", "Settings"), ChipKind.Ghost) { BackColor = Theme.Bg };
        cfgC.Click += delegate { EditSettings(); };
        status = new Label { BackColor = Theme.Bg, ForeColor = Theme.Ink2, Font = Theme.Small, AutoSize = false };
        credit = new Label { Text = "@lovett_35", BackColor = Theme.Bg, ForeColor = Theme.Ink3, Font = Theme.Small, AutoSize = true };

        Controls.AddRange(new Control[] { card, preview, scroll, freeF, editNamesC, cfgC, status, credit });
        Resize += delegate { LayoutAll(); };
        var topTimer = new System.Windows.Forms.Timer { Interval = 500 };
        topTimer.Tick += delegate { KeepOnTop(); };
        topTimer.Start();
        BuildButtons();
        LayoutAll();
        UpdatePreview(null);
        Say(Lang.T("클튜에서 레이어를 고르고 이름을 누르세요.  단축키 ", "クリスタでレイヤーを選んで名前をクリック。ショートカット ", "Pick a layer in Clip Studio, then click a name.  Shortcut ") + cfg.RenameKey, false);
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= Native.WS_EX_TOPMOST; return cp; }
    }
    // 다른 프로그램 때문에 항상 위가 풀려도 되찾음(켜져 있을 땐 아무것도 안 함 — 다른 항상 위 창과 다투지 않게)
    void KeepOnTop()
    {
        if (!IsHandleCreated) return;
        if ((Native.GetWindowLong(Handle, Native.GWL_EXSTYLE) & Native.WS_EX_TOPMOST) == 0)
            Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.SWP_KEEP);
    }

    string[] DepthItems() { return new[] { None(), cfg.Front, cfg.Back }; }
    static string None() { return Lang.T("없음", "なし", "None"); }

    static void Rename(string from, string to)
    {
        try { if (File.Exists(from) && !File.Exists(to)) File.Move(from, to); } catch { }
    }

    void EditNames()
    {
        string text = "";
        try { text = File.ReadAllText(namesPath, Encoding.UTF8); } catch { }
        using (var f = new NamesForm(text))
        {
            if (f.ShowDialog(this) != DialogResult.OK) return;
            File.WriteAllText(namesPath, f.Result, new UTF8Encoding(true));
        }
        BuildButtons();
        Say(Lang.T("이름 목록을 저장했어요.", "名前リストを保存しました。", "Name list saved."), false);
    }

    void EditSettings()
    {
        Settings next;
        using (var f = new SettingsForm(cfg))
        {
            if (f.ShowDialog(this) != DialogResult.OK) return;
            next = f.Result;
        }
        next.Save();
        if (next.Lang != cfg.Lang)
        {
            // 이름 목록을 안 고쳤으면(어느 언어든 기본 목록 그대로) 새 언어 목록으로
            try { if (Lang.IsDefaultNames(File.ReadAllText(namesPath, Encoding.UTF8))) File.WriteAllText(namesPath, Lang.DefaultNames(next.Lang), new UTF8Encoding(true)); } catch { }
            // 글꼴 · 글자가 다 바뀌니 창을 새로 염(같은 자리)
            Keep = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            Restart = true;
            Close();
            return;
        }
        cfg = next;
        depthSeg.SetItems(DepthItems());
        LayoutAll();
        UpdatePreview(null);
        Say(Lang.T("설정을 저장했어요.", "設定を保存しました。", "Settings saved."), false);
    }
    public static bool Restart;
    public static Rectangle Keep = Rectangle.Empty;
    string DepthText() { return depth == 1 ? cfg.Front : depth == 2 ? cfg.Back : ""; }

    Label Cap(string t) { return new Label { Text = t, AutoSize = false, Font = Theme.Small, ForeColor = Theme.Ink2, BackColor = Theme.Card, TextAlign = ContentAlignment.BottomLeft }; }

    void LayoutAll()
    {
        if (card == null) return;
        // 최소화 중엔 창 크기가 0 이라 칩이 엉뚱하게 놓이고 스크롤 범위가 그대로 남음 → 되살릴 때 다시 놓음
        if (WindowState == FormWindowState.Minimized || ClientSize.Height < Theme.P(100)) return;
        int pad = Theme.P(12), W = ClientSize.Width, H = ClientSize.Height;
        int ip = Theme.P(14), cw = W - pad * 2, iw = cw - ip * 2, gap = Theme.P(8);
        int half = (iw - gap) / 2, y = ip;
        preL.SetBounds(ip, y, half, Theme.P(18));
        sufL.SetBounds(ip + half + gap, y, half, Theme.P(18));
        y += Theme.P(22);
        preF.SetBounds(ip, y, half, Theme.P(34));
        sufF.SetBounds(ip + half + gap, y, half, Theme.P(34));
        y += Theme.P(34) + Theme.P(14);
        int segW = Math.Min(iw, Math.Max(Theme.P(138), Math.Max(sideSeg.PreferredWidth(), depthSeg.PreferredWidth()))), rowH = Theme.P(34);
        int fw = flipC.PreferredWidth(rowH);
        sideSeg.SetBounds(ip, y, segW, rowH);
        if (segW + gap + fw <= iw)
        {
            flipC.SetBounds(ip + iw - fw, y, fw, rowH);
            y += rowH + gap;
            depthSeg.SetBounds(ip, y, segW, rowH);
        }
        else
        {
            depthSeg.SetBounds(ip, y + rowH + gap, segW, rowH);
            y += (rowH + gap) * 2;
            flipC.SetBounds(ip, y, fw, rowH);
        }
        y += rowH + Theme.P(14);
        int th = Theme.P(28), lift = Theme.P(2);
        numT.SetBounds(ip, y, numT.PreferredWidth(), th);
        int rw = resetC.PreferredWidth(Theme.P(24));
        resetC.SetBounds(numT.Right + Theme.P(8), y + lift, rw, Theme.P(24));
        y += th + Theme.P(8);
        nextT.SetBounds(ip, y, nextT.PreferredWidth(), th);
        // 한 번에 N개: 칸이 모자라면 다음 줄로
        int fw2 = Theme.P(44), lw = countL.PreferredWidth, uw = countU.PreferredWidth, g2 = Theme.P(5);
        int total = lw + g2 + fw2 + g2 + uw, cx = ip + iw - total;
        if (cx < nextT.Right + Theme.P(8)) { y += th + Theme.P(6); cx = ip; }
        countL.Location = new Point(cx, y + (th - countL.PreferredHeight) / 2);
        countF.SetBounds(cx + lw + g2, y, fw2, th);
        countU.Location = new Point(countF.Right + g2, y + (th - countU.PreferredHeight) / 2);
        y += th + ip;
        card.SetBounds(pad, pad, cw, y);

        preview.SetBounds(pad, card.Bottom + Theme.P(10), cw, Theme.P(40));

        int statusH = Theme.P(36), toolH = Theme.P(30), freeH = Theme.P(36);
        int bottomTop = H - pad - statusH - Theme.P(4) - toolH - Theme.P(8) - freeH;
        freeF.SetBounds(pad, bottomTop, cw, freeH);
        int ty = bottomTop + freeH + Theme.P(8), tx = pad;
        foreach (var c in new[] { editNamesC, cfgC })
        {
            int w = c.PreferredWidth(toolH);
            c.SetBounds(tx, ty, w, toolH);
            tx += w + Theme.P(6);
        }
        credit.Location = new Point(pad + cw - credit.PreferredWidth, ty + (toolH - credit.PreferredHeight) / 2);
        status.SetBounds(pad + Theme.P(2), ty + toolH + Theme.P(4), cw - Theme.P(4), statusH);

        int sTop = preview.Bottom + Theme.P(10);
        scroll.SetBounds(pad, sTop, cw + Theme.P(6), Math.Max(Theme.P(60), bottomTop - Theme.P(10) - sTop));
        LayoutChips();
    }

    void BuildButtons()
    {
        scroll.SuspendLayout();
        var olds = new List<Control>();
        foreach (Control c in scroll.Controls) olds.Add(c); // Dispose 하면 목록에서 빠져서 그대로 돌면 절반을 건너뜀
        scroll.Controls.Clear();
        foreach (var c in olds) c.Dispose();
        scroll.AutoScrollPosition = Point.Empty;
        string[] lines;
        try { lines = File.ReadAllLines(namesPath, Encoding.UTF8); }
        catch (Exception ex) { Say(Lang.T("이름 목록을 못 읽었어요: ", "名前リストを読めません: ", "Couldn't read the name list: ") + ex.Message, true); scroll.ResumeLayout(); return; }
        foreach (var raw in lines)
        {
            var t = raw.Trim();
            if (t == "") continue;
            if (IsRule(t))
            {
                // 메모장에 --- 처럼 가로선만 쓴 줄 → 구분선
                scroll.Controls.Add(new Label { BackColor = Theme.Off, AutoSize = false, Tag = "sep" });
                continue;
            }
            if (t.StartsWith("#"))
            {
                scroll.Controls.Add(new Label { Text = t.TrimStart('#').Trim(), Font = Theme.SmallBold, ForeColor = Theme.Ink2, BackColor = Theme.Bg, AutoSize = false, TextAlign = ContentAlignment.BottomLeft, Tag = "h" });
                continue;
            }
            var name = t;
            var b = new Chip(name, ChipKind.Name) { BackColor = Theme.Bg };
            b.Click += delegate { pressed = b; Apply(name); };
            b.MouseEnter += delegate { UpdatePreview(name); };
            b.MouseLeave += delegate { UpdatePreview(null); };
            scroll.Controls.Add(b);
        }
        scroll.ResumeLayout();
        LayoutChips();
    }

    // ---, ───, ===, ___ 처럼 선 글자만 두 개 이상
    static bool IsRule(string t)
    {
        if (t.Length < 2) return false;
        foreach (char ch in t) if ("-─━—–=_~＿ー".IndexOf(ch) < 0) return false;
        return true;
    }

    void LayoutChips()
    {
        if (scroll == null || laying) return;
        int cw = scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - Theme.P(4);
        if (cw < Theme.P(40)) return;
        laying = true;
        try { PlaceChips(cw); } finally { laying = false; }
    }
    bool laying;

    void PlaceChips(int cw)
    {
        int chipH = Theme.P(32), gap = Theme.P(6), x = 0, y = 0;
        var o = scroll.AutoScrollPosition;
        bool rowOpen = false; // 지금 줄에 칩이 있는지
        foreach (Control c in scroll.Controls)
        {
            var tag = c.Tag as string;
            if (tag == "sep")
            {
                y += rowOpen ? chipH + Theme.P(12) : Theme.P(4);
                c.SetBounds(o.X + Theme.P(2), o.Y + y, cw - Theme.P(2), Math.Max(1, Theme.P(1)));
                y += Theme.P(10);
                x = 0;
                rowOpen = false;
                continue;
            }
            if (tag == "h")
            {
                if (rowOpen) y += chipH + Theme.P(14);
                c.SetBounds(o.X + Theme.P(2), o.Y + y, cw, Theme.P(20));
                y += Theme.P(26);
                x = 0;
                rowOpen = false;
                continue;
            }
            var chip = (Chip)c;
            int w = Math.Min(cw, chip.PreferredWidth(chipH));
            if (x > 0 && x + w > cw) { x = 0; y += chipH + gap; }
            chip.SetBounds(o.X + x, o.Y + y, w, chipH);
            x += w + gap;
            rowOpen = true;
        }
        // 칩을 옮긴 뒤엔 스크롤 범위를 다시 안 재서(다시 읽기 때 스크롤바가 사라짐) 높이를 직접 알려 줌
        // 창 크기가 바뀌는 도중에 넣은 값은 스크롤바에 안 걸리고 남을 때가 있어서(아래가 잘림) 매번 다시 재게 함
        scroll.AutoScrollMinSize = new Size(0, y + (rowOpen ? chipH : 0) + Theme.P(4));
        scroll.PerformLayout();
    }

    int NextNum(string baseName)
    {
        int n;
        counters.TryGetValue(Key(baseName), out n);
        return n + 1;
    }
    string Key(string baseName) { return preBox.Text + "\u0001" + baseName + "\u0001" + side + "\u0001" + depth; }

    string Compose(string baseName, string sideVal, int num)
    {
        var s = cfg.Format
            .Replace("{앞}", preBox.Text)
            .Replace("{이름}", baseName)
            .Replace("{앞뒤}", DepthText())
            .Replace("{번호}", num > 0 ? num.ToString() : "")
            .Replace("{방향}", sideVal == SideLeft ? cfg.Left : sideVal == SideRight ? cfg.Right : "")
            .Replace("{뒤}", sufBox.Text);
        while (s.Contains("  ")) s = s.Replace("  ", " ");
        return s.Trim();
    }

    void UpdatePreview(string hover)
    {
        if (preview == null || numT == null) return; // 창을 만드는 중
        var b = hover ?? lastBase;
        if (b == null) { preview.Set("", Lang.T("이름에 마우스를 올리면 붙을 이름이 보여요", "名前にマウスを乗せると、つく名前が見えます", "Hover a name to preview it"), true); return; }
        int n = Count();
        if (n > 1) { int st = NextNum(b); preview.Set(Cap2(hover != null) + " " + Many(n), Compose(b, side, st) + " ~ " + Compose(b, side, st + n - 1), false); return; }
        preview.Set(Cap2(hover != null), Compose(b, side, numChk.Checked ? NextNum(b) : 0), false);
    }

    static string Cap2(bool hover) { return hover ? Lang.T("누르면", "クリックで", "Click") : Lang.T("다음", "次", "Next"); }
    static string Many(int n) { return Lang.T(n + "개", n + "個", n + " layers"); }

    int Count()
    {
        int n;
        if (countF == null || !int.TryParse(countF.Box.Text, out n)) return 1;
        return Math.Max(1, Math.Min(99, n));
    }

    void FlipAgain()
    {
        if (lastBase == null) { Say(Lang.T("먼저 이름을 하나 붙이세요.", "先に名前を一つつけてください。", "Name a layer first."), true); return; }
        if (side == SideNone) { Say(Lang.T("< 나 > 를 고른 상태에서만 쓸 수 있어요.", "< か > を選んでいるときだけ使えます。", "Only works when < or > is selected."), true); return; }
        sideSeg.SelectedIndex = side == SideLeft ? 2 : 1;
        pressed = flipC;
        Apply(lastBase);
    }

    void Say(string s, bool err) { statusErr = err; status.ForeColor = err ? Theme.Danger : Theme.Ink2; status.Text = s; }
    void Say(string s) { Say(s, true); }

    IntPtr FindClip(out uint pid)
    {
        pid = 0;
        foreach (var p in Process.GetProcessesByName(cfg.Target))
        {
            if (p.MainWindowHandle != IntPtr.Zero) { pid = (uint)p.Id; return p.MainWindowHandle; }
        }
        return IntPtr.Zero;
    }

    static IntPtr FocusOf(IntPtr fg)
    {
        uint pid;
        uint tid = Native.GetWindowThreadProcessId(fg, out pid);
        var gi = new Native.GUITHREADINFO();
        gi.cbSize = Marshal.SizeOf(typeof(Native.GUITHREADINFO));
        return Native.GetGUIThreadInfo(tid, ref gi) ? gi.hwndFocus : IntPtr.Zero;
    }

    static void WaitKeysUp()
    {
        // 사용자가 아직 누르고 있는 키(Enter 등)가 클튜로 섞여 들어가지 않게
        for (int i = 0; i < 40; i++)
        {
            bool any = false;
            foreach (int vk in new[] { 0x0D, 0x10, 0x11, 0x12, 0x01 }) if ((Native.GetAsyncKeyState(vk) & 0x8000) != 0) any = true;
            if (!any) return;
            Thread.Sleep(25);
        }
    }

    // 고른 레이어 하나 이름 바꾸기. 문제가 있으면 이유를, 없으면 null 을 돌려줌
    string RenameOne(string name, Hotkey rename, uint pid)
    {
        Clipboard.SetText(name);
        var fg0 = Native.GetForegroundWindow();
        uint fpid;
        Native.GetWindowThreadProcessId(fg0, out fpid);
        if (fpid != pid) return Lang.T("클튜가 아닌 창이 앞에 있어서 멈췄어요. 클튜를 한 번 누르고 다시 해 보세요.", "クリスタ以外のウィンドウが前にあったので止めました。クリスタを一度クリックしてから、もう一度試してください。", "Stopped: another window was in front. Click Clip Studio once and try again.");
        var focus0 = FocusOf(fg0);

        rename.Press();
        bool opened = cfg.NoCheck, dialog = false;
        var sw = Stopwatch.StartNew();
        while (!opened && sw.ElapsedMilliseconds < cfg.Wait)
        {
            Thread.Sleep(30);
            var fg = Native.GetForegroundWindow();
            uint p2;
            Native.GetWindowThreadProcessId(fg, out p2);
            if (p2 == pid && fg != fg0) { opened = true; dialog = true; }
            else if (p2 == pid && FocusOf(fg) != focus0) opened = true;
        }
        if (!opened) return Lang.T("클튜에서 이름 바꾸기 칸이 안 떴어요. 아무것도 안 눌렀어요.\n클튜 [파일 > 단축 키 설정]에서 [레이어 이름 변경]에 " + cfg.RenameKey + " 를 붙였는지 확인해 주세요.", "クリスタで名前の入力欄が開きませんでした。何も押していません。\nクリスタの［ファイル］→［ショートカットキー設定］で［レイヤーの名前変更］に " + cfg.RenameKey + " を割り当てたか確認してください。", "The rename box didn't open in Clip Studio. Nothing was typed.\nIn Clip Studio, check that " + cfg.RenameKey + " is assigned to \"Change Layer Name\" (File > Shortcut Settings).");
        Thread.Sleep(cfg.Settle);
        Hotkey.Parse("Ctrl+A").Press();
        Thread.Sleep(30);
        Hotkey.Parse("Ctrl+V").Press();
        Thread.Sleep(cfg.Settle);
        Hotkey.Parse("Enter").Press();
        if (dialog)
        {
            // 이름 창이 닫힌 뒤에 다음 키를 눌러야 함
            sw.Restart();
            while (Native.GetForegroundWindow() != fg0 && sw.ElapsedMilliseconds < cfg.Wait) Thread.Sleep(20);
        }
        return null;
    }

    static string Q(string s) { return Lang.Code == "ja" ? "「" + s + "」" : "\"" + s + "\""; }

    void Apply(string baseName)
    {
        if (busy) return;
        var rename = Hotkey.Parse(cfg.RenameKey);
        if (rename == null) { Say(Lang.T("설정의 이름 변경 단축키를 못 읽었어요: ", "設定の名前変更ショートカットが読めません: ", "Can't read the rename shortcut in Settings: ") + cfg.RenameKey); return; }
        var next = Hotkey.Parse(cfg.NextKey);
        int count = Count();
        bool many = count > 1;
        if (many && next == null) { Say(Lang.T("설정의 아래 레이어 단축키를 못 읽었어요: ", "設定の下のレイヤーのショートカットが読めません: ", "Can't read the layer-below shortcut in Settings: ") + cfg.NextKey); return; }
        uint pid;
        var clip = FindClip(out pid);
        if (clip == IntPtr.Zero) { Say(Lang.T("클립 스튜디오가 안 켜져 있어요.", "CLIP STUDIO PAINT が起動していません。", "Clip Studio Paint isn't running.")); return; }

        busy = true;
        string oldClip = null, first = null, last = null;
        int done = 0;
        bool stopped = false;
        try { if (Clipboard.ContainsText()) oldClip = Clipboard.GetText(); } catch { }
        try
        {
            WaitKeysUp();
            if (Native.IsIconic(clip)) Native.ShowWindow(clip, 9);
            Native.SetForegroundWindow(clip);
            Thread.Sleep(cfg.Settle);
            Native.GetAsyncKeyState(0x1B); // 이전에 눌린 Esc 비우기
            for (int i = 0; i < count; i++)
            {
                if ((Native.GetAsyncKeyState(0x1B) & 0x8001) != 0) { stopped = true; break; }
                int num = many || numChk.Checked ? NextNum(baseName) : 0;
                var name = Compose(baseName, side, num);
                if (many) { Say((i + 1) + " / " + count + "  " + Q(name) + Lang.T(" 붙이는 중…  Esc 로 멈춤", " をつけています…  Esc で停止", " …  Esc to stop"), false); status.Update(); }
                var err = RenameOne(name, rename, pid);
                if (err != null) { Say((done > 0 ? Lang.T(done + "개 붙이고 멈췄어요. ", done + "個つけて止まりました。", "Stopped after " + done + ". ") : "") + err); return; }
                if (num > 0) counters[Key(baseName)] = num;
                lastBase = baseName;
                if (first == null) first = name;
                last = name;
                done++;
                if (i < count - 1 || nextChk.Checked)
                {
                    if (next != null) { Thread.Sleep(cfg.Settle * 2); next.Press(); Thread.Sleep(cfg.Settle * 2); }
                }
            }
            if (done > 0 && pressed != null) pressed.Flash();
            if (stopped) Say(Lang.T("Esc 로 멈췄어요. " + done + "개 붙였어요.", "Esc で止めました。" + done + "個つけました。", "Stopped with Esc. Named " + done + "."), false);
            else if (many) Say(Q(first) + " ~ " + Q(last) + Lang.T(" " + done + "개 붙였어요.", " " + done + "個つけました。", "  named " + done + "."), false);
            else Say(Q(last) + Lang.T(" 붙였어요.", " をつけました。", " named.") + (nextChk.Checked ? Lang.T("  아래 레이어로 옮겼어요.", "  下のレイヤーへ移りました。", "  Moved to the layer below.") : ""), false);
        }
        catch (Exception ex) { Say(Lang.T("문제가 생겼어요: ", "問題が発生しました: ", "Something went wrong: ") + ex.Message); }
        finally
        {
            busy = false;
            if (many) countF.Box.Text = ""; // 실수로 같은 개수를 또 붙이지 않게 1 로 되돌림
            UpdatePreview(null);
            var keep = oldClip;
            var t = new System.Windows.Forms.Timer { Interval = 600 };
            t.Tick += delegate { t.Stop(); t.Dispose(); try { if (keep != null) Clipboard.SetText(keep); } catch { } };
            t.Start();
        }
    }

    [STAThread]
    static void Main()
    {
        try { Native.SetProcessDPIAware(); } catch { }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => Crash(e.Exception);
        try { do { NamerForm.Restart = false; Application.Run(new NamerForm()); } while (NamerForm.Restart); }
        catch (Exception ex) { Crash(ex); }
    }

    static void Crash(Exception ex)
    {
        try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.txt"), ex.ToString(), new UTF8Encoding(true)); } catch { }
        MessageBox.Show(Lang.T("문제가 생겨 멈췄어요. 같은 폴더의 error.txt 를 보내 주세요.", "問題が発生して止まりました。同じフォルダの error.txt を送ってください。", "Something went wrong. Please send error.txt from the same folder.") + "\n\n" + ex.Message, "Layer Namer");
    }
}
