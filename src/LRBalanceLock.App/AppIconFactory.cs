namespace LRBalanceLock.App;

internal static class AppIconFactory
{
    private static readonly Lazy<Icon> Icon = new(CreateIcon);

    public static Icon AppIcon => Icon.Value;

    private static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var background = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 64, 64),
            Color.FromArgb(47, 107, 255),
            Color.FromArgb(111, 66, 193),
            45f);
        graphics.FillEllipse(background, 4, 4, 56, 56);

        using var ring = new Pen(Color.FromArgb(235, 255, 255, 255), 3f);
        graphics.DrawEllipse(ring, 7, 7, 50, 50);

        using var line = new Pen(Color.White, 5f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        graphics.DrawLine(line, 18, 32, 46, 32);

        using var knobBrush = new SolidBrush(Color.FromArgb(255, 255, 255));
        using var shadowBrush = new SolidBrush(Color.FromArgb(55, 0, 0, 0));
        graphics.FillEllipse(shadowBrush, 26, 21, 15, 25);
        graphics.FillEllipse(knobBrush, 25, 20, 15, 24);

        using var font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8f, FontStyle.Bold, GraphicsUnit.Point);
        using var textBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255));
        graphics.DrawString("L", font, textBrush, 13, 42);
        graphics.DrawString("R", font, textBrush, 44, 42);

        return Icon.FromHandle(bitmap.GetHicon());
    }
}
