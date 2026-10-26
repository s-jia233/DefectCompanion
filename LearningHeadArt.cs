using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public static class LearningHeadArt
    {
        private static readonly Dictionary<string,BitmapSource[]> Heads=new Dictionary<string,BitmapSource[]>(),Traces=new Dictionary<string,BitmapSource[]>();
        static LearningHeadArt()
        {
            foreach(var clip in InstalledModArt.ColdHeadClips.Values){Load(clip,"head",Heads);Load(clip,"trace",Traces);}
        }
        private static void Load(NativeClip clip,string kind,Dictionary<string,BitmapSource[]> into)
        {
            int w=(int)clip.Bounds.Width,h=(int)clip.Bounds.Height;var frames=new BitmapSource[clip.Count];
            for(int start=0;start<clip.Count;start+=64)
            {
                var sheet=new FormatConvertedBitmap(GameArt.Bitmap("mod-learning-"+kind+"-"+clip.Name+"-"+(start/64)),PixelFormats.Bgra32,null,0);sheet.Freeze();
                for(int i=0;i<64 && start+i<clip.Count;i++)
                {
                    var data=new byte[w*h*4];sheet.CopyPixels(new Int32Rect(i%8*w,i/8*h,w,h),data,w*4,0);
                    var bitmap=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,data,w*4);bitmap.Freeze();frames[start+i]=bitmap;
                }
            }
            into.Add(clip.Name,frames);
        }
        public static bool Ready {get{return Heads.Count==23 && Traces.Count==23;}}
        public static BitmapSource Frame(string name,int frame,bool trace=false){return (trace?Traces:Heads)[name][frame];}
    }
}
