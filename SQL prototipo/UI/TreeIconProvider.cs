using System.Drawing.Drawing2D;

namespace SQL_prototipo;

/// <summary>
/// Provides the tree view icons (server, databases, folders, tables) used across the UI.
/// All icons are generated programmatically as 16x16 images.
/// </summary>
public static class TreeIconProvider
{
    /// <summary>
    /// Creates a shared <see cref="ImageList"/> populated with all tree view icons.
    /// </summary>
    public static ImageList CreateImageList()
    {
        ImageList imageList = new ImageList();
        imageList.ImageSize = new Size(16, 16);
        imageList.ColorDepth = ColorDepth.Depth32Bit;

        imageList.Images.Add("server", CreateServerIcon());

        // Active Icons
        imageList.Images.Add("database_active", CreateDatabaseIcon(Color.FromArgb(43, 87, 151), true));
        imageList.Images.Add("database_sqlite_active", CreateDatabaseIcon(Color.FromArgb(0, 100, 150), true));
        imageList.Images.Add("database_sqlserver_active", CreateDatabaseIcon(Color.FromArgb(186, 12, 47), true));
        imageList.Images.Add("database_localdb_active", CreateDatabaseIcon(Color.FromArgb(120, 40, 140), true));

        // Inactive Icons
        imageList.Images.Add("database_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));
        imageList.Images.Add("database_sqlite_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));
        imageList.Images.Add("database_sqlserver_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));
        imageList.Images.Add("database_localdb_inactive", CreateDatabaseIcon(Color.FromArgb(150, 155, 160), false));

        imageList.Images.Add("folder", CreateFolderIcon());
        imageList.Images.Add("table", CreateTableIcon());

        return imageList;
    }

    /// <summary>
    /// Returns the image key for a database node depending on its type and active state.
    /// </summary>
    public static string GetDatabaseIconKey(string databaseType, bool isActive)
    {
        string suffix = isActive ? "_active" : "_inactive";
        return databaseType.ToLower() switch
        {
            "sqlite" => "database_sqlite" + suffix,
            "sqlserver" => "database_sqlserver" + suffix,
            "localdb" => "database_localdb" + suffix,
            _ => "database" + suffix
        };
    }

    private static Image CreateServerIcon()
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color frameColor = Color.FromArgb(70, 80, 95);
            Color faceColor = Color.FromArgb(230, 235, 240);
            Color ledColor = Color.FromArgb(0, 200, 100);

            // First blade
            using (Brush brush = new SolidBrush(faceColor))
            using (Pen pen = new Pen(frameColor, 1f))
            {
                g.FillRectangle(brush, 1, 3, 14, 4);
                g.DrawRectangle(pen, 1, 3, 14, 4);

                g.FillRectangle(brush, 1, 9, 14, 4);
                g.DrawRectangle(pen, 1, 9, 14, 4);
            }

            // LED indicators
            using (Brush ledBrush = new SolidBrush(ledColor))
            {
                g.FillEllipse(ledBrush, 3, 4, 2, 2);
                g.FillEllipse(ledBrush, 3, 10, 2, 2);
            }

            // Vents
            using (Pen linePen = new Pen(Color.FromArgb(120, 130, 140), 1))
            {
                g.DrawLine(linePen, 7, 5, 12, 5);
                g.DrawLine(linePen, 7, 11, 12, 11);
            }
        }
        return bmp;
    }

    private static Image CreateDatabaseIcon(Color color, bool isActive)
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int x = 2, w = 12;
            int h = 4; // Height of the ellipse

            Color darkColor = Color.FromArgb(
                Math.Max(0, color.R - 30),
                Math.Max(0, color.G - 30),
                Math.Max(0, color.B - 30)
            );
            Color lightColor = Color.FromArgb(
                Math.Min(255, color.R + 40),
                Math.Min(255, color.G + 40),
                Math.Min(255, color.B + 40)
            );

            // Draw stacked segments
            using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                new Rectangle(x, 1, w, 14), darkColor, lightColor, LinearGradientMode.Horizontal))
            {
                // Bottom cylinder section
                g.FillRectangle(bodyBrush, x, 9, w, 4);
                g.FillEllipse(bodyBrush, x, 11, w, h);

                // Middle cylinder section
                g.FillRectangle(bodyBrush, x, 5, w, 4);
                g.FillEllipse(bodyBrush, x, 7, w, h);

                // Top cylinder section body
                g.FillRectangle(bodyBrush, x, 1, w, 4);
                g.FillEllipse(bodyBrush, x, 3, w, h);
            }

            // Top lid
            using (LinearGradientBrush lidBrush = new LinearGradientBrush(
                new Rectangle(x, 1, w, h), lightColor, color, LinearGradientMode.Vertical))
            {
                g.FillEllipse(lidBrush, x, 1, w, h);
            }

            // Outlines
            Color outlineColor = Color.FromArgb(120, 255, 255, 255);
            using (Pen outlinePen = new Pen(outlineColor, 1f))
            {
                g.DrawEllipse(outlinePen, x, 1, w, h);
                g.DrawEllipse(outlinePen, x, 5, w, h);
                g.DrawEllipse(outlinePen, x, 9, w, h);
            }

            // Side borders
            using (Pen borderPen = new Pen(darkColor, 1f))
            {
                g.DrawLine(borderPen, x, 3, x, 13);
                g.DrawLine(borderPen, x + w, 3, x + w, 13);
            }

            // Status indicator badge in bottom-right corner
            if (isActive)
            {
                // Draw white background circle for contrast
                using (Brush whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteBrush, 10, 10, 6, 6);
                }
                // Fill with green color
                using (Brush greenBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
                {
                    g.FillEllipse(greenBrush, 11, 11, 4, 4);
                }
            }
            else
            {
                // Draw white background circle for contrast
                using (Brush whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteBrush, 10, 10, 6, 6);
                }
                // Fill with muted gray color
                using (Brush grayBrush = new SolidBrush(Color.FromArgb(180, 185, 190)))
                {
                    g.FillEllipse(grayBrush, 11, 11, 4, 4);
                }
            }
        }
        return bmp;
    }

    private static Image CreateFolderIcon()
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color baseColor = Color.FromArgb(240, 173, 78);
            Color lightColor = Color.FromArgb(252, 218, 141);
            Color shadowColor = Color.FromArgb(200, 130, 30);

            using (LinearGradientBrush brush = new LinearGradientBrush(
                new Rectangle(2, 2, 12, 12), lightColor, baseColor, LinearGradientMode.ForwardDiagonal))
            {
                GraphicsPath path = new GraphicsPath();
                path.AddLine(2, 4, 2, 13);
                path.AddLine(2, 13, 14, 13);
                path.AddLine(14, 13, 14, 4);
                path.AddLine(14, 4, 8, 4);
                path.AddLine(7, 2, 2, 2);
                path.CloseFigure();

                g.FillPath(brush, path);
                using (Pen borderPen = new Pen(shadowColor, 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            using (LinearGradientBrush flapBrush = new LinearGradientBrush(
                new Rectangle(2, 5, 12, 8), Color.FromArgb(255, 230, 170), baseColor, LinearGradientMode.Vertical))
            {
                g.FillRectangle(flapBrush, 2, 5, 12, 8);
                using (Pen borderPen = new Pen(shadowColor, 1f))
                {
                    g.DrawRectangle(borderPen, 2, 5, 12, 8);
                }
            }
        }
        return bmp;
    }

    private static Image CreateTableIcon()
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color headerColor = Color.FromArgb(41, 128, 185);
            Color gridColor = Color.FromArgb(200, 210, 220);
            Color rowColor2 = Color.FromArgb(240, 244, 248);

            // Table body
            g.FillRectangle(Brushes.White, 2, 2, 12, 12);

            // Header
            using (Brush hb = new SolidBrush(headerColor))
            {
                g.FillRectangle(hb, 2, 2, 12, 4);
            }

            // Alternate row
            using (Brush r2 = new SolidBrush(rowColor2))
            {
                g.FillRectangle(r2, 2, 9, 12, 2);
            }

            // Grid border
            using (Pen borderPen = new Pen(Color.FromArgb(100, 110, 120), 1f))
            {
                g.DrawRectangle(borderPen, 2, 2, 12, 12);
            }

            // Grid lines (horizontal)
            using (Pen gridPen = new Pen(gridColor, 1f))
            {
                g.DrawLine(gridPen, 2, 6, 14, 6);
                g.DrawLine(gridPen, 2, 9, 14, 9);
                g.DrawLine(gridPen, 2, 11, 14, 11);

                // Vertical column dividers
                g.DrawLine(gridPen, 6, 6, 6, 14);
                g.DrawLine(gridPen, 10, 6, 10, 14);
            }
        }
        return bmp;
    }
}
