using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed class CardInfo
    {
        public string Skill, Asset, Name, CompanionEffect, Category;
        public CardInfo(string skill, string asset, string name, string effect, string category)
        { Skill = skill; Asset = asset; Name = name; CompanionEffect = effect; Category = category; }
    }
    public static partial class GameArt
    {
        private static readonly Dictionary<string, BitmapSource> bitmaps = new Dictionary<string, BitmapSource>();
        public static BitmapSource Bitmap(string key)
        {
            BitmapSource existing;
            if (bitmaps.TryGetValue(key, out existing)) return existing;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("assets." + key + ".png"))
            {
                if (stream == null) throw new InvalidOperationException("缺少游戏素材：" + key);
                var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); bitmaps[key] = bitmap; return bitmap;
            }
        }
        public static CardInfo Card(string skill)
        { foreach (var card in Cards) if (card.Skill == skill) return card; return null; }
        public static string OrbName(OrbKind kind)
        { return kind == OrbKind.Lightning ? "lightning" : kind == OrbKind.Frost ? "frost" : kind == OrbKind.Dark ? "dark" : "plasma"; }
        public static BitmapSource Sequence(int variant, int frame)
        {
            string key = "lightning-frame-" + variant + "-" + frame;
            BitmapSource existing;
            if (bitmaps.TryGetValue(key, out existing)) return existing;
            var crop = new CroppedBitmap(Bitmap("lightning-sequence-" + variant), new Int32Rect(frame % 3 * 170, frame / 3 * 170, 170, 170));
            crop.Freeze(); bitmaps[key] = crop; return crop;
        }
        public static BitmapSource EffectFrame(string name, int columns, int rows, int frame)
        {
            frame = System.Math.Max(0,System.Math.Min(columns*rows-1,frame));
            string key = "fx-frame-" + name + "-" + frame;
            BitmapSource existing; if(bitmaps.TryGetValue(key,out existing)) return existing;
            BitmapSource sheet=Bitmap("fx-"+name); int w=sheet.PixelWidth/columns,h=sheet.PixelHeight/rows;
            var crop=new CroppedBitmap(sheet,new Int32Rect(frame%columns*w,frame/columns*h,w,h));
            crop.Freeze();bitmaps[key]=crop;return crop;
        }
    }
}
