using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public static partial class LearningNeckArt
    {
        private static readonly BitmapSource[] Material,Trace;
        static LearningNeckArt(){Material=Load(false);Trace=Load(true);}
        private static BitmapSource[] Load(bool trace)
        {
            var frames=new BitmapSource[156];int w=(int)Bounds.Width,h=(int)Bounds.Height;
            for(int start=0;start<156;start+=64)
            {
                BitmapSource sheet;
                using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("assets.mod-learning-neck-"+(trace?"trace-":"")+(start/64)+".png"))
                {
                    var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();
                    sheet=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);sheet.Freeze();
                }
                for(int i=0;i<64 && start+i<156;i++)
                {
                    var data=new byte[w*h*4];sheet.CopyPixels(new Int32Rect(i%8*w,i/8*h,w,h),data,w*4,0);
                    var image=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,data,w*4);image.Freeze();frames[start+i]=image;
                }
            }
            return frames;
        }
        public static bool Ready {get{return Material.Length==156 && Trace.Length==156;}}
        public static BitmapSource Frame(int index,bool trace=false){return (trace?Trace:Material)[index];}
    }
}
