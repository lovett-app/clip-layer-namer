// 프로그램 안 설정 창 · 이름 목록 창 (메모장 대신)
using System;
using System.Drawing;
using System.Windows.Forms;

class DialogBase : Form
{
    protected int pad;
    public DialogBase(string title, int w, int h)
    {
        Text = title;
        AutoScaleMode = AutoScaleMode.None;
        Font = Theme.Ui;
        BackColor = Theme.Bg;
        DoubleBuffered = true;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        ShowIcon = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        ClientSize = new Size(w, h);
        pad = Theme.P(18);
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { e.Handled = true; DialogResult = DialogResult.Cancel; } };
    }
    protected Label Lbl(string t, Font f, Color c)
    {
        var l = new Label { Text = t, Font = f, ForeColor = c, BackColor = Theme.Bg, AutoSize = true };
        Controls.Add(l);
        return l;
    }
    // 폭에 맞춰 줄바꿈되는 안내 글
    protected Label Note(string t, int x, int y, int w)
    {
        var fl = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
        int h = TextRenderer.MeasureText(t, Theme.Small, new Size(w, 9999), fl).Height + Theme.P(2);
        var l = new Label { Text = t, Font = Theme.Small, ForeColor = Theme.Ink2, BackColor = Theme.Bg, AutoSize = false };
        l.SetBounds(x, y, w, h);
        Controls.Add(l);
        return l;
    }
    protected Field Box(string text, string cue)
    {
        var f = new Field(cue) { BackColor = Theme.Bg };
        f.Box.Text = text;
        Controls.Add(f);
        return f;
    }
    protected Chip Btn(string t, ChipKind k)
    {
        var c = new Chip(t, k) { BackColor = Theme.Bg };
        Controls.Add(c);
        return c;
    }
    protected void Rule(int y)
    {
        var l = new Label { BackColor = Theme.Line, AutoSize = false };
        l.SetBounds(pad, y, ClientSize.Width - pad * 2, Math.Max(1, Theme.P(1)));
        Controls.Add(l);
    }
    // 오른쪽 아래 [취소] [저장]
    protected int Buttons(int y, EventHandler save)
    {
        int h = Theme.P(34);
        var ok = Btn(Lang.T("저장", "保存", "Save"), ChipKind.Name);
        var no = Btn(Lang.T("취소", "キャンセル", "Cancel"), ChipKind.Ghost);
        int ow = Math.Max(Theme.P(84), ok.PreferredWidth(h)), nw = Math.Max(Theme.P(84), no.PreferredWidth(h));
        ok.SetBounds(ClientSize.Width - pad - ow, y, ow, h);
        no.SetBounds(ok.Left - Theme.P(8) - nw, y, nw, h);
        ok.Click += save;
        no.Click += delegate { DialogResult = DialogResult.Cancel; };
        return y + h;
    }
}

// 칸을 누르고 키를 누르면 "Ctrl+Alt+R" 처럼 적힘
class KeyField : Field
{
    public KeyField(string text) : base("")
    {
        Box.ReadOnly = true;
        Box.BackColor = Theme.Card;
        Box.Text = text;
        Box.Cursor = Cursors.Hand;
        Box.KeyDown += (s, e) =>
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            var k = KeyName(e.KeyCode);
            if (k == null) return;
            Box.Text = (e.Control ? "Ctrl+" : "") + (e.Alt ? "Alt+" : "") + (e.Shift ? "Shift+" : "") + k;
            Box.SelectAll();
        };
    }
    static string KeyName(Keys k)
    {
        if (k >= Keys.A && k <= Keys.Z) return k.ToString();
        if (k >= Keys.D0 && k <= Keys.D9) return ((int)(k - Keys.D0)).ToString();
        if (k >= Keys.F1 && k <= Keys.F12) return k.ToString();
        if (k == Keys.OemOpenBrackets) return "[";
        if (k == Keys.OemCloseBrackets) return "]";
        return null;
    }
}

class SettingsForm : DialogBase
{
    public Settings Result;
    readonly Settings old;
    readonly string startLang;
    Settings cur; // 창에 보이는 값(언어를 바꿔 다시 그릴 때 그대로 옮김)
    Segmented lang;
    KeyField renameK, nextK;
    Field leftF, rightF, frontF, backF, formatF, waitF, settleF;
    Toggle noCheckT;
    Label err;

    public SettingsForm(Settings cfg) : base(Lang.T("설정", "設定", "Settings"), Theme.P(420), Theme.P(200))
    {
        old = cfg;
        cur = cfg.Copy();
        startLang = Lang.Code;
        Build();
        // 언어만 바꿔 보고 취소하면 본 창 글자 · 글꼴은 원래대로
        FormClosed += delegate { if (Result == null || Result.Lang == startLang) { Lang.Code = startLang; Theme.Init(Theme.S, startLang); } };
    }

    // 언어를 고르면 그 언어로 창을 바로 다시 그림
    void SwitchLang(string code)
    {
        if (code == cur.Lang) return;
        Grab();
        if (cur.Front == Lang.Front(cur.Lang)) cur.Front = Lang.Front(code);
        if (cur.Back == Lang.Back(cur.Lang)) cur.Back = Lang.Back(code);
        cur.Lang = code;
        Lang.Code = code;
        Theme.Init(Theme.S, code);
        BeginInvoke((MethodInvoker)Build); // 누른 단추를 그 단추의 클릭 안에서 지우지 않게
    }

    // 칸에 적힌 값을 cur 로(검사는 저장할 때)
    void Grab()
    {
        int n;
        cur.RenameKey = renameK.Box.Text;
        cur.NextKey = nextK.Box.Text;
        cur.Left = leftF.Box.Text.Trim();
        cur.Right = rightF.Box.Text.Trim();
        cur.Front = frontF.Box.Text.Trim();
        cur.Back = backF.Box.Text.Trim();
        cur.Format = Lang.ReadFormat(formatF.Box.Text.Trim());
        if (int.TryParse(waitF.Box.Text, out n)) cur.Wait = n;
        if (int.TryParse(settleF.Box.Text, out n)) cur.Settle = n;
        cur.NoCheck = noCheckT.Checked;
    }

    void Build()
    {
        SuspendLayout();
        var olds = new System.Collections.Generic.List<Control>();
        foreach (Control c in Controls) olds.Add(c);
        Controls.Clear();
        foreach (var c in olds) c.Dispose();
        Text = Lang.T("설정", "設定", "Settings");
        Font = Theme.Ui;
        var cfg = cur;
        int W = ClientSize.Width, iw = W - pad * 2, y = pad, rowH = Theme.P(34);

        y = Head(Lang.T("언어", "言語", "Language"), y);
        lang = new Segmented(Lang.Names) { BackColor = Theme.Bg };
        lang.SelectedIndex = Lang.Index(cfg.Lang);
        lang.Changed += delegate { SwitchLang(Lang.Codes[lang.SelectedIndex]); };
        lang.SetBounds(pad, y, iw, rowH);
        Controls.Add(lang);
        y += rowH + Theme.P(20);

        y = Head(Lang.T("클립 스튜디오 단축키", "CLIP STUDIO のショートカット", "Clip Studio shortcuts"), y);
        renameK = new KeyField(cfg.RenameKey) { BackColor = Theme.Bg };
        nextK = new KeyField(cfg.NextKey) { BackColor = Theme.Bg };
        y = Row(Lang.T("레이어 이름 변경", "レイヤーの名前変更", "Change Layer Name"), renameK, y);
        y = Row(Lang.T("아래 레이어 선택", "下のレイヤーを選択", "Select layer below"), nextK, y);
        y = Note(Lang.T(
            "처음 한 번만 해 두세요.\n" +
            "1. 클튜 [파일 > 단축 키 설정]에서 [레이어 이름 변경]에 단축키를 붙여요.\n" +
            "2. 위 [레이어 이름 변경] 칸을 누르고 그 키를 똑같이 눌러요.\n" +
            "[아래 레이어 선택]은 클튜 기본값이 Alt+[ 라서 보통 그대로 두면 돼요.",
            "最初に一度だけ設定してください。\n" +
            "1. クリスタの［ファイル］→［ショートカットキー設定］で［レイヤーの名前変更］にキーを割り当てます。\n" +
            "2. 上の［レイヤーの名前変更］欄をクリックして、同じキーを押します。\n" +
            "［下のレイヤーを選択］はクリスタの初期設定が Alt+[ なので、通常はそのままで大丈夫です。",
            "First-time setup (once):\n" +
            "1. In Clip Studio, open File > Shortcut Settings and assign keys to \"Change Layer Name\".\n" +
            "2. Click the \"Change Layer Name\" box above and press the same keys.\n" +
            "\"Select layer below\" is Alt+[ by default in Clip Studio, so you can usually leave it as is."),
            pad, y, iw).Bottom + Theme.P(20);

        y = Head(Lang.T("이름에 붙는 글자", "名前につく文字", "Marks added to names"), y);
        leftF = Box(cfg.Left, "<"); rightF = Box(cfg.Right, ">");
        frontF = Box(cfg.Front, Lang.Front(cfg.Lang)); backF = Box(cfg.Back, Lang.Back(cfg.Lang));
        y = Pair(Lang.T("왼쪽", "左", "Left"), leftF, Lang.T("오른쪽", "右", "Right"), rightF, y);
        y = Pair(Lang.T("앞쪽", "前", "Front"), frontF, Lang.T("뒤쪽", "後ろ", "Back"), backF, y);
        formatF = Box(Lang.ShowFormat(cfg.Format), "");
        // 이름 모양은 길어서 칸을 한 줄 통째로
        var fl = Lbl(Lang.T("이름 모양", "名前の形", "Name pattern"), Theme.Ui, Theme.Ink);
        fl.Location = new Point(pad, y + Theme.P(4));
        y += Theme.P(28);
        formatF.SetBounds(pad, y, iw, rowH);
        y += rowH + Theme.P(8);
        y = Note(Lang.FormatHelp(), pad, y - Theme.P(4), iw).Bottom + Theme.P(20);

        y = Head(Lang.T("고급", "詳細", "Advanced"), y);
        waitF = Num(cfg.Wait); settleF = Num(cfg.Settle);
        y = Row(Lang.T("이름 칸을 기다리는 시간 (ms)", "入力欄を待つ時間 (ms)", "Wait for rename box (ms)"), waitF, y);
        y = Row(Lang.T("키 사이 쉬는 시간 (ms)", "キーの間の待ち時間 (ms)", "Pause between keys (ms)"), settleF, y);
        noCheckT = new Toggle(Lang.T("이름 칸이 떴는지 확인 안 함", "入力欄が開いたか確認しない", "Don't check that the rename box opened"), cfg.NoCheck) { BackColor = Theme.Bg };
        noCheckT.SetBounds(pad, y, noCheckT.PreferredWidth(), Theme.P(28));
        Controls.Add(noCheckT);
        y += Theme.P(28) + Theme.P(18);

        Rule(y);
        y += Theme.P(14);
        y = Note(Lang.T(
            "이 프로그램은 Claude를 이용한 바이브 코딩으로 만들었어요.",
            "このプログラムは Claude を使ったバイブコーディングで作られました。",
            "This program was vibe-coded with Claude."),
            pad, y, iw).Bottom + Theme.P(14);

        err = new Label { Font = Theme.Small, ForeColor = Theme.Danger, BackColor = Theme.Bg, AutoSize = false };
        err.SetBounds(pad, y, iw, Theme.P(18));
        Controls.Add(err);
        y += Theme.P(22);
        y = Buttons(y, delegate { Save(); }) + pad;
        ClientSize = new Size(W, y);
        ResumeLayout();
    }

    int Head(string t, int y)
    {
        var l = Lbl(t, Theme.SmallBold, Theme.Ink2);
        l.Location = new Point(pad + Theme.P(2), y);
        return y + Theme.P(24);
    }
    // 왼쪽 글 + 오른쪽 칸
    int Row(string t, Control box, int y)
    {
        int h = Theme.P(34), bw = (ClientSize.Width - pad * 2) * 11 / 20;
        var l = Lbl(t, Theme.Ui, Theme.Ink);
        l.Location = new Point(pad, y + (h - l.PreferredHeight) / 2);
        box.SetBounds(ClientSize.Width - pad - bw, y, bw, h);
        if (box.Parent == null) Controls.Add(box);
        return y + h + Theme.P(8);
    }
    int Pair(string t1, Field f1, string t2, Field f2, int y)
    {
        int h = Theme.P(34), half = (ClientSize.Width - pad * 2 - Theme.P(16)) / 2, lw = Theme.P(70);
        var l1 = Lbl(t1, Theme.Ui, Theme.Ink);
        var l2 = Lbl(t2, Theme.Ui, Theme.Ink);
        l1.Location = new Point(pad, y + (h - l1.PreferredHeight) / 2);
        f1.SetBounds(pad + lw, y, half - lw, h);
        int x2 = pad + half + Theme.P(16);
        l2.Location = new Point(x2, y + (h - l2.PreferredHeight) / 2);
        f2.SetBounds(x2 + lw, y, half - lw, h);
        return y + h + Theme.P(8);
    }
    Field Num(int v)
    {
        var f = Box(v.ToString(), "");
        f.Box.MaxLength = 5;
        f.Box.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
        return f;
    }

    void Save()
    {
        int wait, settle;
        if (Hotkey.Parse(renameK.Box.Text) == null || Hotkey.Parse(nextK.Box.Text) == null) { err.Text = Lang.T("단축키 칸을 채워 주세요.", "ショートカット欄を入力してください。", "Please set both shortcuts."); return; }
        if (!int.TryParse(waitF.Box.Text, out wait) || !int.TryParse(settleF.Box.Text, out settle)) { err.Text = Lang.T("시간 칸에 숫자를 넣어 주세요.", "時間欄に数字を入力してください。", "Please enter numbers for the times."); return; }
        Grab(); // 앞뒤 글자 기본값은 언어를 고를 때 이미 바꿔 둠
        var s = cur.Copy();
        if (s.Format == "") s.Format = "{앞}{이름}{앞뒤}{번호}{방향}{뒤}";
        s.Wait = Math.Max(100, wait);
        s.Settle = Math.Max(0, settle);
        Result = s;
        DialogResult = DialogResult.OK;
    }
}

// 여러 줄 입력칸(둥근 테두리)
class NoteBox : Control
{
    public readonly TextBox Box;
    public NoteBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Box = new TextBox { Multiline = true, AcceptsReturn = true, AcceptsTab = false, ScrollBars = ScrollBars.Vertical, WordWrap = false, BorderStyle = BorderStyle.None, Font = Theme.Ui, BackColor = Theme.Card, ForeColor = Theme.Ink };
        Controls.Add(Box);
        Box.GotFocus += delegate { Invalidate(); };
        Box.LostFocus += delegate { Invalidate(); };
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        int p = Theme.P(10);
        Box.SetBounds(p, Theme.P(8), Width - p - Theme.P(4), Height - Theme.P(16));
        base.OnLayout(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        Theme.Box(e.Graphics, ClientRectangle, Theme.P(10), Theme.Card, Box.Focused ? Theme.Accent : Theme.Line);
    }
}

class NamesForm : DialogBase
{
    public string Result;
    NoteBox note;
    Label hint;
    Chip headC, ruleC, resetC, okC, noC;

    public NamesForm(string text) : base(Lang.T("이름 목록", "名前リスト", "Name list"), Theme.P(380), Theme.P(600))
    {
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(Theme.P(320), Theme.P(360));
        hint = new Label
        {
            Text = Lang.T("한 줄에 이름 하나.  # 으로 시작하면 묶음 제목,  --- 는 구분선이에요.",
                          "1行に名前1つ。# で始まる行はグループ名、--- は区切り線です。",
                          "One name per line. Lines starting with # are group titles, --- draws a divider."),
            Font = Theme.Small, ForeColor = Theme.Ink2, BackColor = Theme.Bg, AutoSize = false
        };
        Controls.Add(hint);
        headC = Btn(Lang.T("+ 묶음 제목", "+ グループ名", "+ Group title"), ChipKind.Link);
        ruleC = Btn(Lang.T("+ 구분선", "+ 区切り線", "+ Divider"), ChipKind.Link);
        headC.Click += delegate { InsertLine("# ", true); };
        ruleC.Click += delegate { InsertLine("---", false); };
        note = new NoteBox { BackColor = Theme.Bg };
        note.Box.Text = text.TrimStart('﻿');
        Controls.Add(note);
        resetC = Btn(Lang.T("기본 목록으로", "初期リストに戻す", "Reset to default"), ChipKind.Ghost);
        resetC.Click += delegate { note.Box.Text = Lang.DefaultNames(Lang.Code); note.Box.Focus(); };
        okC = Btn(Lang.T("저장", "保存", "Save"), ChipKind.Name);
        noC = Btn(Lang.T("취소", "キャンセル", "Cancel"), ChipKind.Ghost);
        okC.Click += delegate { Result = note.Box.Text; DialogResult = DialogResult.OK; };
        noC.Click += delegate { DialogResult = DialogResult.Cancel; };
        Resize += delegate { Lay(); };
        Lay();
        Shown += delegate { note.Box.Focus(); note.Box.Select(0, 0); };
    }

    void Lay()
    {
        int W = ClientSize.Width, H = ClientSize.Height, iw = W - pad * 2, y = pad;
        var fl = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
        int hh = TextRenderer.MeasureText(hint.Text, Theme.Small, new Size(iw, 9999), fl).Height + Theme.P(2);
        hint.SetBounds(pad, y, iw, hh);
        y += hh + Theme.P(8);
        int ch = Theme.P(26), x = pad;
        foreach (var c in new[] { headC, ruleC })
        {
            int w = c.PreferredWidth(ch);
            c.SetBounds(x, y, w, ch);
            x += w + Theme.P(4);
        }
        y += ch + Theme.P(8);
        int bh = Theme.P(34), by = H - pad - bh;
        note.SetBounds(pad, y, iw, Math.Max(Theme.P(80), by - Theme.P(14) - y));
        int rw = resetC.PreferredWidth(bh);
        resetC.SetBounds(pad, by, rw, bh);
        int ow = Math.Max(Theme.P(84), okC.PreferredWidth(bh)), nw = Math.Max(Theme.P(84), noC.PreferredWidth(bh));
        okC.SetBounds(W - pad - ow, by, ow, bh);
        noC.SetBounds(okC.Left - Theme.P(8) - nw, by, nw, bh);
    }

    // 커서가 있는 줄 앞에 한 줄 끼워 넣기
    void InsertLine(string line, bool caretAfter)
    {
        var b = note.Box;
        int at = b.SelectionStart;
        int start = at == 0 ? 0 : b.Text.LastIndexOf('\n', Math.Max(0, at - 1)) + 1;
        b.Select(start, 0);
        b.SelectedText = line + "\r\n";
        b.Focus();
        if (caretAfter) b.Select(start + line.Length, 0);
    }
}
