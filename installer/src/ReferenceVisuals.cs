using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace Marko22.PTTP
{
    [DataContract]
    public sealed class UiProfile
    {
        [DataMember(Name = "backgroundImage")] public string BackgroundImage { get; set; }
        [DataMember(Name = "brandImage")] public string BrandImage { get; set; }
        [DataMember(Name = "brandWidth")] public int BrandWidth { get; set; }
        [DataMember(Name = "brandPositionX")] public int BrandPositionX { get; set; }
        [DataMember(Name = "brandPositionY")] public int BrandPositionY { get; set; }
        [DataMember(Name = "brandPosition")] public string BrandPosition { get; set; }
        [DataMember(Name = "brandScale")] public double BrandScale { get; set; }
        [DataMember(Name = "brandOffsetX")] public int BrandOffsetX { get; set; }
        [DataMember(Name = "brandOffsetY")] public int BrandOffsetY { get; set; }
        [DataMember(Name = "brandIconImage")] public string BrandIconImage { get; set; }
        [DataMember(Name = "brandCropX")] public int BrandCropX { get; set; }
        [DataMember(Name = "brandGap")] public int BrandGap { get; set; }
        [DataMember(Name = "sectionBackgroundOpacity")] public double SectionBackgroundOpacity { get; set; }
        [DataMember(Name = "supportUrl")] public string SupportUrl { get; set; }
        [DataMember(Name = "gameLogoImage")] public string GameLogoImage { get; set; }
        [DataMember(Name = "gameLogoWidth")] public int GameLogoWidth { get; set; }
        [DataMember(Name = "gameLogoOffsetX")] public int GameLogoOffsetX { get; set; }
        [DataMember(Name = "gameLogoOffsetY")] public int GameLogoOffsetY { get; set; }
    }

    // Z-index: pełne tło konkretnej gry, emblemat, kontrolki potomne.
    public sealed class LayeredAssetHost : Panel
    {
        private Image background;
        private Image brand;
        private Image brandIcon;
        private Image gameLogo;
        private Bitmap composedScene;
        private UiProfile profile;

        public double SectionBackgroundOpacity
        {
            get { return profile == null ? 0D : Math.Max(0D, Math.Min(1D, profile.SectionBackgroundOpacity)); }
        }

        public string SupportUrl
        {
            get { return profile == null ? null : profile.SupportUrl; }
        }

        public LayeredAssetHost()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(8, 11, 14);
        }

        public void LoadProfile(string profilePath)
        {
            if (!File.Exists(profilePath)) throw new FileNotFoundException("Brak profilu warstw UI.", profilePath);
            var serializer = new DataContractJsonSerializer(typeof(UiProfile));
            using (var stream = File.OpenRead(profilePath)) profile = (UiProfile)serializer.ReadObject(stream);
            string root = Path.GetDirectoryName(Path.GetFullPath(profilePath));
            background = LoadUnlocked(ResolveAsset(root, profile.BackgroundImage));
            brand = LoadUnlocked(ResolveAsset(root, profile.BrandImage));
            if (!String.IsNullOrWhiteSpace(profile.BrandIconImage)) brandIcon = LoadUnlocked(ResolveAsset(root, profile.BrandIconImage));
            if (!String.IsNullOrWhiteSpace(profile.GameLogoImage)) gameLogo = LoadUnlocked(ResolveAsset(root, profile.GameLogoImage));
            if (profile.BrandWidth <= 0) profile.BrandWidth = 286;
            RebuildSceneCache();
            Invalidate();
        }

        public void LoadEmbeddedProfile()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var serializer = new DataContractJsonSerializer(typeof(UiProfile));
            using (Stream stream = OpenResource(assembly, "Marko22Installer.UiProfile.json"))
                profile = (UiProfile)serializer.ReadObject(stream);
            background = LoadEmbedded(assembly, ResourceNameFor(profile.BackgroundImage));
            brand = LoadEmbedded(assembly, ResourceNameFor(profile.BrandImage));
            if (!String.IsNullOrWhiteSpace(profile.BrandIconImage))
                brandIcon = LoadEmbedded(assembly, ResourceNameFor(profile.BrandIconImage));
            if (!String.IsNullOrWhiteSpace(profile.GameLogoImage))
                gameLogo = LoadEmbedded(assembly, ResourceNameFor(profile.GameLogoImage));
            if (profile.BrandWidth <= 0) profile.BrandWidth = 286;
            RebuildSceneCache();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (composedScene != null) e.Graphics.DrawImageUnscaled(composedScene, 0, 0);
            base.OnPaint(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            RebuildSceneCache();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (background != null) background.Dispose();
                if (brand != null) brand.Dispose();
                if (brandIcon != null) brandIcon.Dispose();
                if (gameLogo != null) gameLogo.Dispose();
                if (composedScene != null) composedScene.Dispose();
            }
            base.Dispose(disposing);
        }

        private static string ResolveAsset(string root, string relative)
        {
            if (String.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Profil UI zawiera nieprawidłową ścieżkę assetu.");
            string rootPrefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(rootPrefix, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Ścieżka assetu wychodzi poza katalog assets.");
            if (!File.Exists(full)) throw new FileNotFoundException("Brak skonfigurowanego assetu UI.", full);
            return full;
        }

        private static Image LoadUnlocked(string path)
        {
            using (var source = Image.FromFile(path)) return new Bitmap(source);
        }

        private static string ResourceNameFor(string relative)
        {
            string normalized = (relative ?? String.Empty).Replace('\\', '/');
            if (String.Equals(normalized, "Games/PowerToThePeople/background.jpg", StringComparison.OrdinalIgnoreCase))
                return "Marko22Installer.Ui.Background.jpg";
            if (String.Equals(normalized, "Games/PowerToThePeople/logo.png", StringComparison.OrdinalIgnoreCase))
                return "Marko22Installer.Ui.GameLogo.png";
            if (String.Equals(normalized, "Common/marko22-brand.png", StringComparison.OrdinalIgnoreCase))
                return "Marko22Installer.Ui.Brand.png";
            if (String.Equals(normalized, "Common/marko22-scorpion.png", StringComparison.OrdinalIgnoreCase))
                return "Marko22Installer.Ui.BrandIcon.png";
            throw new InvalidDataException("Profil UI wskazuje asset, który nie został osadzony w aplikacji: " + relative);
        }

        private static Stream OpenResource(Assembly assembly, string resourceName)
        {
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) throw new FileNotFoundException("Brak osadzonego zasobu UI.", resourceName);
            return stream;
        }

        private static Image LoadEmbedded(Assembly assembly, string resourceName)
        {
            using (Stream stream = OpenResource(assembly, resourceName))
            using (Image source = Image.FromStream(stream))
                return new Bitmap(source);
        }

        private void RebuildSceneCache()
        {
            if (profile == null || background == null || brand == null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            var scene = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(scene))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.DrawImage(background, Cover(background, ClientRectangle, 1F, 0F, 0F));
                if (gameLogo != null)
                {
                    double dpiScale; using (Graphics target = CreateGraphics()) dpiScale = target.DpiX / 96D;
                    int logoWidth = Math.Max(1, (int)Math.Round(profile.GameLogoWidth * dpiScale));
                    int logoHeight = Math.Max(1, (int)Math.Round(logoWidth * gameLogo.Height / (double)gameLogo.Width));
                    graphics.DrawImage(gameLogo, new Rectangle(
                        (int)Math.Round(profile.GameLogoOffsetX * dpiScale),
                        (int)Math.Round(profile.GameLogoOffsetY * dpiScale), logoWidth, logoHeight));
                }
                if (String.Equals(profile.BrandPosition, "bottom-left", StringComparison.OrdinalIgnoreCase) && brandIcon != null)
                    DrawBottomLeftBrand(graphics);
                else
                {
                    double dpiScale; using (Graphics target = CreateGraphics()) dpiScale = target.DpiX / 96D;
                    int brandWidth = (int)Math.Round(profile.BrandWidth * dpiScale);
                    int brandHeight = (int)Math.Round(brandWidth * brand.Height / (double)brand.Width);
                    graphics.DrawImage(brand, new Rectangle((int)Math.Round(profile.BrandPositionX * dpiScale), (int)Math.Round(profile.BrandPositionY * dpiScale), brandWidth, brandHeight));
                }
            }
            Bitmap old = composedScene;
            composedScene = scene;
            if (old != null) old.Dispose();
        }

        private void DrawBottomLeftBrand(Graphics graphics)
        {
            double dpiScale; using (Graphics target = CreateGraphics()) dpiScale = target.DpiX / 96D;
            double scale = (profile.BrandScale > 0D ? profile.BrandScale : 1D) * dpiScale;
            int cropX = Math.Max(0, Math.Min(brand.Width - 1, profile.BrandCropX));
            int sourceTextWidth = brand.Width - cropX;
            int height = Math.Max(1, (int)Math.Round(Math.Max(brandIcon.Height, brand.Height) * scale));
            int iconWidth = Math.Max(1, (int)Math.Round(brandIcon.Width * height / (double)brandIcon.Height));
            int textWidth = Math.Max(1, (int)Math.Round(sourceTextWidth * height / (double)brand.Height));
            int gap = Math.Max(0, (int)Math.Round(profile.BrandGap * dpiScale));
            int x = Math.Max(0, (int)Math.Round(profile.BrandOffsetX * dpiScale));
            int y = Math.Max(0, ClientSize.Height - height - Math.Max(0, (int)Math.Round(profile.BrandOffsetY * dpiScale)));
            graphics.DrawImage(brandIcon, new Rectangle(x, y, iconWidth, height));
            graphics.DrawImage(
                brand,
                new Rectangle(x + iconWidth + gap, y, textWidth, height),
                new Rectangle(cropX, 0, sourceTextWidth, brand.Height),
                GraphicsUnit.Pixel);
        }

        private static Rectangle Cover(Image image, Rectangle bounds, float scale, float positionX, float positionY)
        {
            double cover = Math.Max(bounds.Width / (double)image.Width, bounds.Height / (double)image.Height) * scale;
            int width = Math.Max(1, (int)Math.Round(image.Width * cover));
            int height = Math.Max(1, (int)Math.Round(image.Height * cover));
            int x = bounds.Left + (bounds.Width - width) / 2 + (int)Math.Round(positionX * bounds.Width);
            int y = bounds.Top + (bounds.Height - height) / 2 + (int)Math.Round(positionY * bounds.Height);
            return new Rectangle(x, y, width, height);
        }
    }

    public sealed class TranslucentPanel : Panel
    {
        public Color TintColor { get; set; }
        public Color BorderColor { get; set; }
        public int CornerRadius { get; set; }
        public TranslucentPanel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            TintColor = Color.FromArgb(204, 13, 17, 21);
            BorderColor = Color.FromArgb(105, 205, 155, 63);
            CornerRadius = 18;
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (var brush = new SolidBrush(TintColor)) e.Graphics.FillPath(brush, path);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (var pen = new Pen(BorderColor)) e.Graphics.DrawPath(pen, path);
        }
        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }
        private static GraphicsPath RoundedPath(Rectangle rectangle, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
