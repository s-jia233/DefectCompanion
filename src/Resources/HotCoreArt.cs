using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public static partial class HotCoreArt
    {
        public static readonly Dictionary<string,NativeClip> Clips=new Dictionary<string,NativeClip>();
        private static readonly Dictionary<string,BitmapSource[]> Red=new Dictionary<string,BitmapSource[]>(),Hot=new Dictionary<string,BitmapSource[]>();
        static partial void Bind();
        static HotCoreArt()
        {
            Bind();
            foreach(var clip in Clips.Values){Load(clip,"red",Red);Load(clip,"hot",Hot);}
        }
        private static void Load(NativeClip clip,string kind,Dictionary<string,BitmapSource[]> into)
        {
            int w=(int)clip.Bounds.Width,h=(int)clip.Bounds.Height;var frames=new BitmapSource[clip.Count];
            for(int start=0;start<clip.Count;start+=64)
            {
                var sheet=new FormatConvertedBitmap(GameArt.Bitmap("native-"+kind+"-core-"+clip.Name+"-"+(start/64)),PixelFormats.Bgra32,null,0);sheet.Freeze();
                for(int i=0;i<64 && start+i<clip.Count;i++)
                {
                    var pixels=new byte[w*h*4];sheet.CopyPixels(new Int32Rect(i%8*w,i/8*h,w,h),pixels,w*4,0);
                    var image=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,pixels,w*4);image.Freeze();frames[start+i]=image;
                }
            }
            into.Add(clip.Name,frames);
        }
        public static int SampleCount {get{int count=0;foreach(var clip in Clips.Values)count+=clip.Count;return count;}}
        public static bool Ready {get{return Clips.Count==23 && SampleCount==3272 && Red.Count==23 && Hot.Count==23;}}
        public static Point Center(string name,int frame){return Clips[name].Anchors[frame][0];}
        public static bool Visible(string name,int frame){return Clips[name].Anchors[frame][1].X>0;}
        public static BitmapSource Frame(string name,int frame,bool hot=false){return (hot?Hot:Red)[name][frame];}
        public static Rect Bounds(string name){return Clips[name].Bounds;}
    }
}
