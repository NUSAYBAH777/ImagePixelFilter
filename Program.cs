using System;
using System.Drawing;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    private readonly PictureBox picLeft = new();
    private readonly PictureBox picRight = new();

    private readonly Button btnLoadLeft = new();
    private readonly Button btnLoadRight = new();
    private readonly Button btnStep = new();
    private readonly Button btnStartStop = new();

    private readonly System.Windows.Forms.Timer timer = new();

    private Bitmap? leftBitmap;
    private Bitmap? rightBitmap;

    private int leftPixelIndex = 0;
    private int rightPixelIndex = 0;

    private bool running = false;

    private readonly Color loadBase  = Color.Purple;
    private readonly Color stepBase  = Color.DodgerBlue;
    private readonly Color startBase = Color.LimeGreen;
    private readonly Color stopBase  = Color.DarkRed;


    private const int StepBatch = 2500;
    private const int AutoBatch = 900;
    private const int AutoIntervalMs = 15;

    public MainForm()
    {
        Text = "Image Pixel Filter";
        Width = 1100;
        Height = 720;
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        BackColor = Color.FromArgb(18, 18, 18);

        ConfigurePictureBox(picLeft);
        ConfigurePictureBox(picRight);

        ConfigureButton(btnLoadLeft, "Load Left", loadBase);
        ConfigureButton(btnLoadRight, "Load Right", loadBase);
        ConfigureButton(btnStep, "STEP", stepBase);

        btnStartStop.Text = "▶";
        btnStartStop.Font = new Font("Segoe UI Symbol", 13, FontStyle.Bold);
        ConfigureButton(btnStartStop, btnStartStop.Text, startBase);

        btnStep.Enabled = false;
        btnStartStop.Enabled = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(2),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        btnLoadLeft.Anchor = AnchorStyles.Left;
        btnLoadRight.Anchor = AnchorStyles.Right;

        topBar.Controls.Add(btnLoadLeft, 0, 0);
        topBar.Controls.Add(btnLoadRight, 1, 0);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 92));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 8));

        var leftButtonHost = new Panel { Dock = DockStyle.Fill };
        var rightButtonHost = new Panel { Dock = DockStyle.Fill };

        leftButtonHost.Controls.Add(btnStep);
        rightButtonHost.Controls.Add(btnStartStop);

        leftButtonHost.Resize += (_, __) => CenterControl(btnStep, leftButtonHost);
        rightButtonHost.Resize += (_, __) => CenterControl(btnStartStop, rightButtonHost);

        grid.Controls.Add(picLeft, 0, 0);
        grid.Controls.Add(picRight, 1, 0);
        grid.Controls.Add(leftButtonHost, 0, 1);
        grid.Controls.Add(rightButtonHost, 1, 1);

        root.Controls.Add(topBar, 0, 0);
        root.Controls.Add(grid, 0, 1);

        Controls.Add(root);

        btnLoadLeft.Click += (_, __) => LoadLeftImage();
        btnLoadRight.Click += (_, __) => LoadRightImage();
        btnStep.Click += (_, __) => DoStep();
        btnStartStop.Click += (_, __) => ToggleStartStop();

        timer.Interval = AutoIntervalMs;
        timer.Tick += (_, __) => DoAutoTick();
    }

    private static void CenterControl(Control c, Control host)
    {
        c.Location = new Point(
            (host.ClientSize.Width - c.Width) / 2,
            (host.ClientSize.Height - c.Height) / 2
        );
    }

    private void ConfigurePictureBox(PictureBox pb)
{
    pb.Dock = DockStyle.Fill;
    pb.SizeMode = PictureBoxSizeMode.StretchImage;
    pb.BackColor = Color.Black;

    pb.Padding = Padding.Empty;          
    pb.BorderStyle = BorderStyle.Fixed3D;   
    pb.Margin = Padding.Empty;          
}


    private void ConfigureButton(Button b, string text, Color baseColor)
    {
        b.Text = text;
        b.Width = 180;
        b.Height = 46;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.ForeColor = Color.White;
        b.Font = b == btnStartStop
            ? b.Font
            : new Font("Segoe UI", 12, FontStyle.Bold);

        b.Tag = baseColor;
        b.BackColor = baseColor;

        b.MouseEnter += (_, __) =>
        {
            var bc = (Color)b.Tag;
            b.BackColor = Lighten(bc, 0.12f);
        };
        b.MouseLeave += (_, __) =>
        {
            var bc = (Color)b.Tag;
            b.BackColor = bc;
        };
        b.MouseDown += (_, __) =>
        {
            var bc = (Color)b.Tag;
            b.BackColor = Darken(bc, 0.10f);
        };
        b.MouseUp += (_, __) =>
        {
            var bc = (Color)b.Tag;
            b.BackColor = Lighten(bc, 0.12f);
        };
    }

    private void LoadLeftImage()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select Left Image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        leftBitmap?.Dispose();
        leftBitmap = new Bitmap(ofd.FileName);
        picLeft.Image = leftBitmap;
        leftPixelIndex = 0;

        btnStep.Enabled = true;
    }

    private void LoadRightImage()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select Right Image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        StopAnimation();

        rightBitmap?.Dispose();
        rightBitmap = new Bitmap(ofd.FileName);
        picRight.Image = rightBitmap;
        rightPixelIndex = 0;

        btnStartStop.Enabled = true;
    }

    private void DoStep()
    {
        if (leftBitmap == null) return;

        for (int i = 0; i < StepBatch; i++)
            ChangeSinglePixel(leftBitmap, ref leftPixelIndex);

        picLeft.Invalidate();
    }

    private void DoAutoTick()
    {
        if (rightBitmap == null) return;

        for (int i = 0; i < AutoBatch; i++)
            ChangeSinglePixel(rightBitmap, ref rightPixelIndex);

        picRight.Invalidate();
    }

    private void ToggleStartStop()
    {
        if (rightBitmap == null) return;

        running = !running;

        if (running)
        {
            btnStartStop.Text = "⏸";
            btnStartStop.Tag = stopBase;
            btnStartStop.BackColor = stopBase;
            timer.Start();
        }
        else
        {
            StopAnimation();
        }
    }

    private void StopAnimation()
    {
        timer.Stop();
        running = false;
        btnStartStop.Text = "▶";
        btnStartStop.Tag = startBase;
        btnStartStop.BackColor = startBase;
    }

    private static void ChangeSinglePixel(Bitmap bmp, ref int index)
    {
        int total = bmp.Width * bmp.Height;
        if (total <= 0) return;
        if (index >= total) index = 0;

        int x = index % bmp.Width;
        int y = index / bmp.Width;

        Color c = bmp.GetPixel(x, y);

        int t = index;
        int r = (255 - c.R + (t % 97)) & 255;
        int g = (c.G + 40 + ((t / 3) % 83)) & 255;
        int b = (c.B + 80 + ((t / 7) % 71)) & 255;

        bmp.SetPixel(x, y, Color.FromArgb(r, g, b));
        index++;
    }

    private static Color Lighten(Color c, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        int r = (int)(c.R + (255 - c.R) * amount);
        int g = (int)(c.G + (255 - c.G) * amount);
        int b = (int)(c.B + (255 - c.B) * amount);
        return Color.FromArgb(r, g, b);
    }

    private static Color Darken(Color c, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        int r = (int)(c.R * (1f - amount));
        int g = (int)(c.G * (1f - amount));
        int b = (int)(c.B * (1f - amount));
        return Color.FromArgb(r, g, b);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        timer.Stop();
        leftBitmap?.Dispose();
        rightBitmap?.Dispose();
        base.OnFormClosing(e);
    }
}
