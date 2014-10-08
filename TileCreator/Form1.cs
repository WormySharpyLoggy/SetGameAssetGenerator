using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TileCreator
{
    public partial class Form1 : Form
    {
        Dictionary<string, Size> screenSizes = new Dictionary<string, Size>{
            {"ldpi", new Size(240, 320)},
            {"mdpi", new Size(320, 470)},
            {"hdpi", new Size(720, 960)},
            {"xhdpi", new Size(1440, 1920)},
            {"xxhdpi", new Size(2160, 2880)},
            {"xxxhdpi", new Size(2880, 3840)}
        };

        Dictionary<string, Size> tileSizes = new Dictionary<string, Size>();
        Dictionary<string, Size> shapeSizes = new Dictionary<string, Size>();

        Random random = new Random();

        public Form1()
        {
            InitializeComponent();

            foreach (string key in screenSizes.Keys)
            {
                int maxTileWidth = (int)(screenSizes[key].Width / 3 * .9);
                int maxTileHeight = (int)(screenSizes[key].Height / 3 * .9);

                // standard playing cards have width 2.5 in, height 3.5 in
                // we want the same ratio

                int tileWidth, tileHeight;

                tileWidth = maxTileWidth;
                tileHeight = (int)(maxTileWidth * 3.5 / 2.5);

                double scale = maxTileHeight / (maxTileWidth * 3.5 / 2.5);
                if (scale < 1.0)
                {
                    tileWidth = (int)(scale * tileWidth);
                    tileHeight = (int)(scale * tileHeight);
                }

                tileSizes[key] = new Size(tileWidth, tileHeight);

                shapeSizes[key] = new Size((int)(tileWidth * .9), (int)(tileHeight * .9 / 3));
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = Environment.CurrentDirectory;
        }

        private Bitmap GetShape(Size size, int iColor, int iShape, int iShading)
        {
            Bitmap bmp = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            Size s = size;
            g.SmoothingMode = SmoothingMode.HighQuality;
            Color color = new Color[] { Color.Red, Color.Green, Color.Purple }[iColor - 1];
            Pen pen = new Pen(new SolidBrush(color), Math.Max(2f, .02f * size.Height));
            Region shapeRegion;
            GraphicsPath shapeBorder;

            if (iShading == 1)
            {
                // filled
                g.FillRectangle(new SolidBrush(color), new Rectangle(new Point(0, 0), s));
            }
            else if (iShading == 2)
            {
                Pen thin = new Pen(new SolidBrush(color), 1f);
                // lined
                for (int i = 10; i < Math.Max((s.Width - 1), (s.Height - 1)) * 2; i += 5)
                {
                    g.DrawLine(thin, new Point(i, 0), new Point(0, i));
                }
            }
            else
            {
                // no fill
            }

            Region complementRegion = new Region(new RectangleF(s.Width * -.1f, s.Height * -.1f, s.Width, s.Height));


            g.TranslateTransform(s.Width * .1f, s.Height * .1f);
            s = new Size((int)(size.Width * .8), (int)(size.Height * .8));


            #region Create Shapes as Regions
            if (iShape == 1)
            {
                // rounded rectangle
                shapeBorder = new GraphicsPath();
                shapeBorder.AddClosedCurve(new Point[]{
                    new Point(0, 0),
                    new Point((s.Width - 1), 0),
                    new Point((s.Width - 1), (s.Height - 1)),
                    new Point(0, (s.Height - 1))
                }, .2f);
                shapeRegion = new Region(shapeBorder);

            }
            else if (iShape == 2)
            {
                // Diamond
                shapeBorder = new GraphicsPath();
                shapeBorder.AddPolygon(new Point[]{
                    new Point((s.Width - 1) / 2, 0), // top
                    new Point(0, (s.Height - 1) / 2), // left
                    new Point((s.Width - 1)/2, (s.Height - 1)), // bottom
                    new Point((s.Width - 1), (s.Height - 1) / 2) // right
                });
                shapeRegion = new Region(shapeBorder);
            }
            else
            {
                // squiggle
                shapeBorder = new GraphicsPath();
                shapeBorder.AddClosedCurve(new Point[]{
                    new Point((s.Width - 1), 0),
                    new Point((s.Width - 1), (s.Height - 1)/2),
                    new Point(2*(s.Width - 1)/3, (s.Height - 1)),
                    new Point((s.Width - 1)/3, 2*(s.Height - 1)/3),
                    new Point(0, (s.Height - 1)),
                    new Point(0, (s.Height - 1)/2),
                    new Point((s.Width - 1)/3, 0),
                    new Point(2*(s.Width - 1)/3, (s.Height - 1)/3)
                });
                shapeRegion = new Region(shapeBorder);
            }
            #endregion

            shapeRegion.Complement(complementRegion);

            g.FillRegion(Brushes.White, shapeRegion);
            g.DrawPath(pen, shapeBorder);
            return bmp;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            int offset = 0;
            foreach (string key in shapeSizes.Keys)
            {
                Bitmap shape = GetShape(shapeSizes[key], random.Next(3) + 1, random.Next(3) + 1, random.Next(3) + 1);
                e.Graphics.DrawImage(shape, new Point(offset, 20));
                offset += shapeSizes[key].Width + 20;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            panel1.Invalidate();

            if (folderBrowserDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return;

            string folder = folderBrowserDialog1.SelectedPath;

            foreach (string key in tileSizes.Keys)
            {
                Size tileSize = tileSizes[key];
                Size shapeSize = shapeSizes[key];
                string keyPath = Path.Combine(folder, "drawable-" + key);
                Directory.CreateDirectory(keyPath);


                int betweenShapes = (int)(0.05 * shapeSize.Height);
                int leftSpacing = (tileSize.Width - shapeSize.Width) / 2;

                // Blank Tile
                Bitmap tileImage = new Bitmap(tileSize.Width, tileSize.Height);
                Graphics tileGraphics = Graphics.FromImage(tileImage);
                tileGraphics.FillRectangle(Brushes.White, new Rectangle(0, 0, tileSize.Width, tileSize.Height));

                for (int i = 0; i < tileSize.Width * 2 || i < tileSize.Height * 2; i += 10)
                {
                    tileGraphics.DrawLine(Pens.Gray, new Point(0, i), new Point(i, 0));
                }
                tileImage.Save(Path.Combine(keyPath, "tile_blank.png"), ImageFormat.Png);


                for (int shape = 1; shape < 4; shape++)
                {
                    for (int color = 1; color < 4; color++)
                    {
                        for (int shading = 1; shading < 4; shading++)
                        {
                            Bitmap shapeImage = GetShape(shapeSize, color, shape, shading);
                            for (int quantity = 1; quantity < 4; quantity++)
                            {
                                tileImage = new Bitmap(tileSize.Width, tileSize.Height);
                                tileGraphics = Graphics.FromImage(tileImage);
                                tileGraphics.FillRectangle(Brushes.White, new Rectangle(0, 0, tileSize.Width, tileSize.Height));

                                int topSpacing = (tileSize.Height - shapeSize.Height * quantity - betweenShapes * (quantity - 1)) / 2;
                                Point[] shapeLocations = new Point[quantity];
                                shapeLocations[0] = new Point(leftSpacing, topSpacing);
                                for (int i = 1; i < quantity; i++)
                                {
                                    shapeLocations[i] = new Point(leftSpacing, shapeLocations[i - 1].Y + shapeSize.Height + betweenShapes);
                                }

                                for (int shapeIndex = 0; shapeIndex < quantity; shapeIndex++)
                                {
                                    tileGraphics.DrawImage(shapeImage, shapeLocations[shapeIndex]);
                                }

                                tileImage.Save(Path.Combine(keyPath, string.Format("tile_{0}{1}{2}{3}.png", quantity, shape, color, shading)), ImageFormat.Png);
                            }
                        }
                    }
                }
            }
        }
    }
}
