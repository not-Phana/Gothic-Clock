using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Collections.Generic;

public class GenerateIcon
{
    public static void Main(string[] args)
    {
        string outIco = args.Length > 0 ? args[0] : "app.ico";
        string outPng = args.Length > 1 ? args[1] : "icon_preview.png";

        int[] sizes = new int[] { 256, 128, 64, 48, 32, 16 };
        List<Bitmap> bitmaps = new List<Bitmap>();

        // Generate master at 512 for super-sampled crispness
        using (Bitmap master = RenderEmblem(512))
        {
            master.Save(outPng, ImageFormat.Png);

            foreach (int s in sizes)
            {
                Bitmap bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.Clear(Color.Transparent);
                    g.DrawImage(master, new Rectangle(0, 0, s, s));
                }
                bitmaps.Add(bmp);
            }
        }

        SaveIco(outIco, bitmaps);

        foreach (Bitmap b in bitmaps)
        {
            b.Dispose();
        }

        Console.WriteLine("SUCCESS: Multi-resolution icon saved to " + outIco);
    }

    private static Bitmap RenderEmblem(int size)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            float cx = size / 2f;
            float cy = size / 2f;

            // 1. Ambient Glow Aura behind the emblem
            using (GraphicsPath glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(cx - 210, cy - 210, 420, 420);
                using (PathGradientBrush pgb = new PathGradientBrush(glowPath))
                {
                    pgb.CenterColor = Color.FromArgb(70, 0, 240, 255);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 240, 255) };
                    g.FillEllipse(pgb, cx - 210, cy - 210, 420, 420);
                }
            }

            // 2. Chamfered Cyber-Shield Hexagon/Octagon Chassis
            float pad = 36f;
            float chamfer = 60f;
            PointF[] shield = new PointF[]
            {
                new PointF(pad + chamfer, pad),
                new PointF(size - pad - chamfer, pad),
                new PointF(size - pad, pad + chamfer),
                new PointF(size - pad, size - pad - chamfer),
                new PointF(size - pad - chamfer, size - pad),
                new PointF(pad + chamfer, size - pad),
                new PointF(pad, size - pad - chamfer),
                new PointF(pad, pad + chamfer)
            };

            // Chassis Background: Deep Obsidian with gradient
            using (GraphicsPath shieldPath = new GraphicsPath())
            {
                shieldPath.AddPolygon(shield);
                using (LinearGradientBrush lgb = new LinearGradientBrush(
                    new PointF(0, pad), new PointF(0, size - pad),
                    Color.FromArgb(250, 10, 14, 22), Color.FromArgb(255, 4, 6, 10)))
                {
                    g.FillPath(lgb, shieldPath);
                }

                // Inner carbon lines (horizontal telemetry scanlines)
                using (Pen scanPen = new Pen(Color.FromArgb(20, 0, 240, 255), 1.5f))
                {
                    for (float y = pad + 20; y < size - pad - 10; y += 16)
                    {
                        g.DrawLine(scanPen, pad + 15, y, size - pad - 15, y);
                    }
                }

                // High-voltage Neon Cyan Border
                using (Pen glowPen = new Pen(Color.FromArgb(60, 0, 240, 255), 14f))
                {
                    glowPen.LineJoin = LineJoin.Round;
                    g.DrawPath(glowPen, shieldPath);
                }
                using (Pen borderPen = new Pen(Color.FromArgb(255, 0, 240, 255), 5.5f))
                {
                    borderPen.LineJoin = LineJoin.Miter;
                    g.DrawPath(borderPen, shieldPath);
                }
            }

            // 3. Cyber Yellow Corner Reinforcements
            float bLen = 28f;
            using (Pen yellowPen = new Pen(Color.FromArgb(255, 255, 230, 0), 4.5f))
            {
                yellowPen.StartCap = LineCap.Square;
                yellowPen.EndCap = LineCap.Square;

                // Top-Left Chamfer
                g.DrawLine(yellowPen, pad + chamfer - bLen, pad, pad + chamfer + bLen, pad);
                // Top-Right Chamfer
                g.DrawLine(yellowPen, size - pad - chamfer - bLen, pad, size - pad - chamfer + bLen, pad);
                // Bottom-Left Chamfer
                g.DrawLine(yellowPen, pad + chamfer - bLen, size - pad, pad + chamfer + bLen, size - pad);
                // Bottom-Right Chamfer
                g.DrawLine(yellowPen, size - pad - chamfer - bLen, size - pad, size - pad - chamfer + bLen, size - pad);
            }

            // 4. Circular Chrono Bezel & Telemetry Ring
            float rOuter = 160f;
            using (Pen ringPen = new Pen(Color.FromArgb(50, 0, 240, 255), 2f))
            {
                g.DrawEllipse(ringPen, cx - rOuter, cy - rOuter, rOuter * 2, rOuter * 2);
            }

            float rBezel = 142f;
            using (Pen bezelPen = new Pen(Color.FromArgb(120, 0, 240, 255), 3f))
            {
                g.DrawEllipse(bezelPen, cx - rBezel, cy - rBezel, rBezel * 2, rBezel * 2);
            }

            // 12 Chronometer Ticks around dial
            for (int i = 0; i < 12; i++)
            {
                double angle = i * (Math.PI / 6.0);
                bool isMajor = (i % 3 == 0); // 12, 3, 6, 9
                float tickLen = isMajor ? 20f : 10f;
                float r1 = rBezel - 2f;
                float r2 = r1 - tickLen;

                float x1 = cx + (float)(Math.Sin(angle) * r1);
                float y1 = cy - (float)(Math.Cos(angle) * r1);
                float x2 = cx + (float)(Math.Sin(angle) * r2);
                float y2 = cy - (float)(Math.Cos(angle) * r2);

                Color tCol = isMajor ? Color.FromArgb(255, 255, 230, 0) : Color.FromArgb(200, 0, 240, 255);
                float tWidth = isMajor ? 4.5f : 2.5f;

                using (Pen tp = new Pen(tCol, tWidth))
                {
                    tp.StartCap = LineCap.Flat;
                    tp.EndCap = LineCap.Flat;
                    g.DrawLine(tp, x1, y1, x2, y2);
                }
            }

            // Sub-ticks (60-second micro pips)
            for (int s = 0; s < 60; s++)
            {
                if (s % 5 == 0) continue; // skip major ticks
                double angle = s * (Math.PI / 30.0);
                float r1 = rBezel - 2f;
                float r2 = r1 - 4f;

                float x1 = cx + (float)(Math.Sin(angle) * r1);
                float y1 = cy - (float)(Math.Cos(angle) * r1);
                float x2 = cx + (float)(Math.Sin(angle) * r2);
                float y2 = cy - (float)(Math.Cos(angle) * r2);

                using (Pen sp = new Pen(Color.FromArgb(80, 0, 240, 255), 1.5f))
                {
                    g.DrawLine(sp, x1, y1, x2, y2);
                }
            }

            // 5. Crosshair Guides
            using (Pen chPen = new Pen(Color.FromArgb(40, 0, 240, 255), 1.5f))
            {
                chPen.DashStyle = DashStyle.Dash;
                g.DrawLine(chPen, cx - rBezel + 24, cy, cx + rBezel - 24, cy);
                g.DrawLine(chPen, cx, cy - rBezel + 24, cx, cy + rBezel - 24);
            }

            // 6. High-Tech Cyber Hands (Hour hand at 10:00, Minute hand at 02:00)
            // Hour Hand (10:00 = 300 deg)
            double hAngle = 300 * Math.PI / 180.0;
            float hLen = 78f;
            PointF hTip = new PointF(cx + (float)(Math.Sin(hAngle) * hLen), cy - (float)(Math.Cos(hAngle) * hLen));
            PointF hLeft = new PointF(cx + (float)(Math.Sin(hAngle - Math.PI / 2) * 9), cy - (float)(Math.Cos(hAngle - Math.PI / 2) * 9));
            PointF hRight = new PointF(cx + (float)(Math.Sin(hAngle + Math.PI / 2) * 9), cy - (float)(Math.Cos(hAngle + Math.PI / 2) * 9));
            PointF hTail = new PointF(cx - (float)(Math.Sin(hAngle) * 18), cy + (float)(Math.Cos(hAngle) * 18));

            GraphicsPath hPath = new GraphicsPath();
            hPath.AddPolygon(new PointF[] { hTip, hRight, hTail, hLeft });
            using (SolidBrush hBrush = new SolidBrush(Color.FromArgb(255, 255, 230, 0))) // Cyber Yellow
            {
                g.FillPath(hBrush, hPath);
            }
            using (Pen hBorder = new Pen(Color.FromArgb(255, 255, 255, 120), 1.5f))
            {
                g.DrawPath(hBorder, hPath);
            }

            // Minute Hand (02:00 = 60 deg)
            double mAngle = 60 * Math.PI / 180.0;
            float mLen = 112f;
            PointF mTip = new PointF(cx + (float)(Math.Sin(mAngle) * mLen), cy - (float)(Math.Cos(mAngle) * mLen));
            PointF mLeft = new PointF(cx + (float)(Math.Sin(mAngle - Math.PI / 2) * 7.5), cy - (float)(Math.Cos(mAngle - Math.PI / 2) * 7.5));
            PointF mRight = new PointF(cx + (float)(Math.Sin(mAngle + Math.PI / 2) * 7.5), cy - (float)(Math.Cos(mAngle + Math.PI / 2) * 7.5));
            PointF mTail = new PointF(cx - (float)(Math.Sin(mAngle) * 24), cy + (float)(Math.Cos(mAngle) * 24));

            GraphicsPath mPath = new GraphicsPath();
            mPath.AddPolygon(new PointF[] { mTip, mRight, mTail, mLeft });
            using (SolidBrush mBrush = new SolidBrush(Color.FromArgb(255, 255, 230, 0)))
            {
                g.FillPath(mBrush, mPath);
            }
            using (Pen mBorder = new Pen(Color.FromArgb(255, 255, 255, 120), 1.5f))
            {
                g.DrawPath(mBorder, mPath);
            }

            // Second Needle (Pointing down at 180 deg = 30s)
            float sLen = 118f;
            using (Pen secPen = new Pen(Color.FromArgb(255, 0, 240, 255), 2.5f))
            {
                g.DrawLine(secPen, cx, cy - 20, cx, cy + sLen);
            }
            using (SolidBrush secTipBrush = new SolidBrush(Color.FromArgb(255, 0, 240, 255)))
            {
                g.FillEllipse(secTipBrush, cx - 4, cy + sLen - 6, 8, 8);
            }

            // 7. Center Reactor Pin (Multi-layered cyber jewel)
            using (SolidBrush ringDark = new SolidBrush(Color.FromArgb(255, 10, 14, 22)))
            {
                g.FillEllipse(ringDark, cx - 18, cy - 18, 36, 36);
            }
            using (Pen ringCyan = new Pen(Color.FromArgb(255, 0, 240, 255), 3f))
            {
                g.DrawEllipse(ringCyan, cx - 18, cy - 18, 36, 36);
            }
            using (SolidBrush yellowPin = new SolidBrush(Color.FromArgb(255, 255, 230, 0)))
            {
                g.FillEllipse(yellowPin, cx - 8, cy - 8, 16, 16);
            }
            using (SolidBrush whiteCore = new SolidBrush(Color.White))
            {
                g.FillEllipse(whiteCore, cx - 3.5f, cy - 3.5f, 7, 7);
            }

            // 8. Cyberpunk HUD Reticle Brackets `[ ` and ` ]` on flanks
            using (Font fBracket = new Font("Consolas", 32, FontStyle.Bold))
            using (SolidBrush brkBrush = new SolidBrush(Color.FromArgb(220, 0, 240, 255)))
            {
                StringFormat sfL = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat sfR = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString("[", fBracket, brkBrush, cx - rBezel + 14, cy - 2, sfL);
                g.DrawString("]", fBracket, brkBrush, cx + rBezel - 14, cy - 2, sfR);
            }

            // 9. Telemetry Markings: "2077" at bottom and "SYS" at top
            using (Font fTele = new Font("Consolas", 15, FontStyle.Bold))
            using (SolidBrush teleYellow = new SolidBrush(Color.FromArgb(255, 255, 230, 0)))
            using (SolidBrush teleCyan = new SolidBrush(Color.FromArgb(220, 0, 240, 255)))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("// 2077 //", fTele, teleYellow, cx, size - pad - 26, sf);
                g.DrawString("CHRONO", fTele, teleCyan, cx, pad + 24, sf);
            }
        }
        return bmp;
    }

    private static void SaveIco(string path, List<Bitmap> bitmaps)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            // ICONDIR Header
            bw.Write((short)0); // Reserved
            bw.Write((short)1); // Type 1 = Icon
            bw.Write((short)bitmaps.Count); // Count

            List<byte[]> pngBytes = new List<byte[]>();
            foreach (Bitmap b in bitmaps)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Png);
                    pngBytes.Add(ms.ToArray());
                }
            }

            int offset = 6 + (16 * bitmaps.Count);
            for (int i = 0; i < bitmaps.Count; i++)
            {
                Bitmap b = bitmaps[i];
                byte[] data = pngBytes[i];

                byte w = (byte)(b.Width >= 256 ? 0 : b.Width);
                byte h = (byte)(b.Height >= 256 ? 0 : b.Height);

                bw.Write(w);
                bw.Write(h);
                bw.Write((byte)0); // Color count
                bw.Write((byte)0); // Reserved
                bw.Write((short)1); // Color planes
                bw.Write((short)32); // Bits per pixel
                bw.Write((int)data.Length);
                bw.Write((int)offset);

                offset += data.Length;
            }

            foreach (byte[] data in pngBytes)
            {
                bw.Write(data);
            }
        }
    }
}
