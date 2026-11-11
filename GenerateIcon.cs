using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;

public class GenerateIcon
{
    public static void Main(string[] args)
    {
        string outIco = args.Length > 0 ? args[0] : "app.ico";
        string outPng = args.Length > 1 ? args[1] : "icon_preview.png";

        int[] sizes = new int[] { 256, 128, 64, 48, 32, 16 };
        List<Bitmap> bitmaps = new List<Bitmap>();

        using (Bitmap master = RenderBleachBadge(512))
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

        Console.WriteLine("SUCCESS: Bleach Shinigami Badge icon saved to " + outIco);
    }

    private static Bitmap RenderBleachBadge(int size)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float cx = size / 2f;
            float cy = size / 2f;

            // 1. Getsuga Tensho Crimson Spiritual Pressure (Reiatsu) Aura
            using (GraphicsPath reiatsuPath = new GraphicsPath())
            {
                reiatsuPath.AddEllipse(cx - 220, cy - 220, 440, 440);
                using (PathGradientBrush pgb = new PathGradientBrush(reiatsuPath))
                {
                    pgb.CenterColor = Color.FromArgb(160, 225, 29, 72); // Crimson blood
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 225, 29, 72) };
                    g.FillEllipse(pgb, cx - 220, cy - 220, 440, 440);
                }
            }

            // Reiatsu flame wisps (Outer spiritual spikes)
            for (int i = 0; i < 16; i++)
            {
                double angle = i * (Math.PI / 8.0);
                float r1 = 180f;
                float r2 = 230f;
                float fx = cx + (float)(Math.Sin(angle) * r2);
                float fy = cy - (float)(Math.Cos(angle) * r2);
                using (Pen flamePen = new Pen(Color.FromArgb(50, 255, 85, 0), 6f)) // Reiatsu orange
                {
                    flamePen.StartCap = LineCap.Round;
                    flamePen.EndCap = LineCap.Round;
                    g.DrawLine(flamePen, cx + (float)(Math.Sin(angle) * r1), cy - (float)(Math.Cos(angle) * r1), fx, fy);
                }
            }

            // 2. Top Cord Suspension Ring (Tassel Ring of Daikōshō)
            float ringY = 56f;
            using (Pen cordPen = new Pen(Color.FromArgb(255, 225, 29, 72), 7f))
            {
                g.DrawEllipse(cordPen, cx - 28, ringY - 28, 56, 56);
            }
            using (SolidBrush knotBrush = new SolidBrush(Color.FromArgb(255, 180, 20, 50)))
            {
                g.FillEllipse(knotBrush, cx - 14, ringY + 12, 28, 20);
            }

            // 3. Substitute Shinigami Badge (代行証) Pentagonal Tablet
            PointF[] badge = new PointF[]
            {
                new PointF(cx - 105, 82),      // Top Left
                new PointF(cx + 105, 82),      // Top Right
                new PointF(cx + 165, 145),     // Upper Shoulder Right
                new PointF(cx + 165, 315),     // Waist Right
                new PointF(cx, 460),           // Bottom Tip (V-shape)
                new PointF(cx - 165, 315),     // Waist Left
                new PointF(cx - 165, 145)      // Upper Shoulder Left
            };

            using (GraphicsPath badgePath = new GraphicsPath())
            {
                badgePath.AddPolygon(badge);

                // Badge Body Gradient: Deep Soul Society Obsidian / Ebony Wood
                using (LinearGradientBrush lgb = new LinearGradientBrush(
                    new PointF(cx, 80), new PointF(cx, 460),
                    Color.FromArgb(255, 22, 24, 34), Color.FromArgb(255, 8, 9, 14)))
                {
                    g.FillPath(lgb, badgePath);
                }

                // Outer Glowing Border (Blood Crimson)
                using (Pen borderPen = new Pen(Color.FromArgb(255, 225, 29, 72), 6f))
                {
                    borderPen.LineJoin = LineJoin.Round;
                    g.DrawPath(borderPen, badgePath);
                }
            }

            // Inner Carved Inset (Gold / Amber Rim)
            PointF[] innerBadge = new PointF[]
            {
                new PointF(cx - 92, 98),
                new PointF(cx + 92, 98),
                new PointF(cx + 146, 154),
                new PointF(cx + 146, 305),
                new PointF(cx, 436),
                new PointF(cx - 146, 305),
                new PointF(cx - 146, 154)
            };
            using (GraphicsPath innerPath = new GraphicsPath())
            {
                innerPath.AddPolygon(innerBadge);
                using (Pen rimPen = new Pen(Color.FromArgb(200, 245, 158, 11), 3f)) // Soul Reaper Gold
                {
                    rimPen.LineJoin = LineJoin.Round;
                    g.DrawPath(rimPen, innerPath);
                }
            }

            // 4. Center Shinigami Skull Crest (The Iconic Substitute Soul Reaper Emblem)
            float skullCy = cy + 5;

            // Cranium (Head dome)
            using (SolidBrush boneBrush = new SolidBrush(Color.FromArgb(250, 248, 250, 252))) // Bone white
            {
                // Upper Cranium Dome
                g.FillPie(boneBrush, cx - 76, skullCy - 105, 152, 135, 180, 180);

                // Lower Face / Cheekbones
                PointF[] face = new PointF[]
                {
                    new PointF(cx - 76, skullCy - 38),
                    new PointF(cx + 76, skullCy - 38),
                    new PointF(cx + 56, skullCy + 45),
                    new PointF(cx + 38, skullCy + 82),  // Jaw right
                    new PointF(cx - 38, skullCy + 82),  // Jaw left
                    new PointF(cx - 56, skullCy + 45)
                };
                g.FillPolygon(boneBrush, face);

                // Forehead Crest Diamond (Soul Reaper mark)
                PointF[] crest = new PointF[]
                {
                    new PointF(cx, skullCy - 118),
                    new PointF(cx + 14, skullCy - 96),
                    new PointF(cx, skullCy - 74),
                    new PointF(cx - 14, skullCy - 96)
                };
                using (SolidBrush crimsonCrest = new SolidBrush(Color.FromArgb(255, 225, 29, 72)))
                {
                    g.FillPolygon(crimsonCrest, crest);
                }

                // Eye Sockets (Deep Void with Crimson Reiatsu)
                using (GraphicsPath eyeL = new GraphicsPath())
                {
                    eyeL.AddPolygon(new PointF[] {
                        new PointF(cx - 52, skullCy - 30),
                        new PointF(cx - 14, skullCy - 16),
                        new PointF(cx - 22, skullCy + 14),
                        new PointF(cx - 56, skullCy - 2)
                    });
                    using (SolidBrush eyeBrush = new SolidBrush(Color.FromArgb(255, 12, 14, 20)))
                    {
                        g.FillPath(eyeBrush, eyeL);
                    }
                    using (Pen eyeGlow = new Pen(Color.FromArgb(200, 225, 29, 72), 2.5f))
                    {
                        g.DrawPath(eyeGlow, eyeL);
                    }
                }

                using (GraphicsPath eyeR = new GraphicsPath())
                {
                    eyeR.AddPolygon(new PointF[] {
                        new PointF(cx + 52, skullCy - 30),
                        new PointF(cx + 14, skullCy - 16),
                        new PointF(cx + 22, skullCy + 14),
                        new PointF(cx + 56, skullCy - 2)
                    });
                    using (SolidBrush eyeBrush = new SolidBrush(Color.FromArgb(255, 12, 14, 20)))
                    {
                        g.FillPath(eyeBrush, eyeR);
                    }
                    using (Pen eyeGlow = new Pen(Color.FromArgb(200, 225, 29, 72), 2.5f))
                    {
                        g.DrawPath(eyeGlow, eyeR);
                    }
                }

                // Nasal Cavity (Inverted Triangle)
                PointF[] nose = new PointF[]
                {
                    new PointF(cx, skullCy + 6),
                    new PointF(cx + 9, skullCy + 26),
                    new PointF(cx - 9, skullCy + 26)
                };
                using (SolidBrush voidBrush = new SolidBrush(Color.FromArgb(255, 12, 14, 20)))
                {
                    g.FillPolygon(voidBrush, nose);
                }

                // Teeth / Jaw Slits (4 distinct vertical slits)
                using (Pen teethPen = new Pen(Color.FromArgb(255, 12, 14, 20), 4f))
                {
                    teethPen.StartCap = LineCap.Round;
                    teethPen.EndCap = LineCap.Round;
                    g.DrawLine(teethPen, cx - 22, skullCy + 48, cx - 22, skullCy + 72);
                    g.DrawLine(teethPen, cx - 7, skullCy + 48, cx - 7, skullCy + 75);
                    g.DrawLine(teethPen, cx + 7, skullCy + 48, cx + 7, skullCy + 75);
                    g.DrawLine(teethPen, cx + 22, skullCy + 48, cx + 22, skullCy + 72);
                }

                // Horizontal Teeth Divider
                using (Pen hLine = new Pen(Color.FromArgb(255, 12, 14, 20), 2.5f))
                {
                    g.DrawLine(hLine, cx - 28, skullCy + 60, cx + 28, skullCy + 60);
                }
            }

            // 5. Stylized Tensa Zangetsu Crossguard / Manji Flanks (Bankai energy marks)
            using (Pen bankaiPen = new Pen(Color.FromArgb(180, 225, 29, 72), 3f))
            {
                // Left flank markings
                g.DrawLine(bankaiPen, cx - 110, skullCy - 60, cx - 110, skullCy + 60);
                g.DrawLine(bankaiPen, cx - 130, skullCy - 20, cx - 90, skullCy - 20);
                g.DrawLine(bankaiPen, cx - 130, skullCy + 20, cx - 90, skullCy + 20);

                // Right flank markings
                g.DrawLine(bankaiPen, cx + 110, skullCy - 60, cx + 110, skullCy + 60);
                g.DrawLine(bankaiPen, cx + 90, skullCy - 20, cx + 130, skullCy - 20);
                g.DrawLine(bankaiPen, cx + 90, skullCy + 20, cx + 130, skullCy + 20);
            }

            // 6. Bottom Kanji-inspired Soul Reaper Carving: "代行" (Daikō - Substitute)
            // Left character "代"
            using (Pen kanjiPen = new Pen(Color.FromArgb(230, 245, 158, 11), 3.5f))
            {
                kanjiPen.StartCap = LineCap.Round;
                kanjiPen.EndCap = LineCap.Round;

                // Left wing: "代"
                g.DrawLine(kanjiPen, cx - 55, 360, cx - 45, 395); // left stroke
                g.DrawLine(kanjiPen, cx - 50, 375, cx - 25, 375); // horiz
                g.DrawLine(kanjiPen, cx - 35, 360, cx - 35, 400); // vert
                g.DrawLine(kanjiPen, cx - 25, 362, cx - 25, 368); // dot

                // Right wing: "行"
                g.DrawLine(kanjiPen, cx + 25, 360, cx + 35, 375); // top left
                g.DrawLine(kanjiPen, cx + 30, 378, cx + 30, 402); // vert left
                g.DrawLine(kanjiPen, cx + 45, 362, cx + 62, 362); // top horiz
                g.DrawLine(kanjiPen, cx + 42, 375, cx + 65, 375); // mid horiz
                g.DrawLine(kanjiPen, cx + 55, 375, cx + 55, 405); // vert hook
            }
        }
        return bmp;
    }

    private static void SaveIco(string path, List<Bitmap> bitmaps)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)bitmaps.Count);

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
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((short)1);
                bw.Write((short)32);
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
