using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed class NativePoseSample
    {
        public double Time;public BitmapSource Image;public Rect Bounds;
    }
    public sealed class NativeClip
    {
        public readonly string Name;
        public readonly int Count;
        public readonly Rect Bounds;
        public readonly Point[][] Anchors;
        public NativeClip(string name,int count,Rect bounds,Point[][] anchors)
        {Name=name;Count=count;Bounds=bounds;Anchors=anchors;}
    }
    public sealed partial class NativeAnimation
    {
        private readonly Dictionary<string,NativeClip> clips=new Dictionary<string,NativeClip>();
        private readonly Dictionary<string,BitmapSource> images=new Dictionary<string,BitmapSource>();
        private readonly Dictionary<string,byte[]> masks=new Dictionary<string,byte[]>();
        private NativeClip current;
        private int index;
        private string requested;
        private CardAnimation score;
        private double started=-100;
        private BitmapSource previous;
        private Rect previousBounds;
        private double mixedAt=-100;
        private readonly List<NativePoseSample> history=new List<NativePoseSample>();
        public NativeAnimation(){LoadClips();LoadCardPoses();PreloadFrames();current=clips["idle_loop"];}
        private void PreloadFrames()
        {
            // Each animation frame owns a small frozen bitmap. Cropping a large PNG lazily
            // during OnRender forces repeated decoding and makes the first playback stutter.
            foreach(var clip in clips.Values)
            {
                int w=(int)clip.Bounds.Width,h=(int)clip.Bounds.Height;
                for(int start=0;start<clip.Count;start+=64)
                {
                    BitmapSource sheet;
                    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("assets.native-"+clip.Name+"-"+(start/64)+".png"))
                    {
                        var source=new BitmapImage();source.BeginInit();source.CacheOption=BitmapCacheOption.OnLoad;
                        source.StreamSource=stream;source.EndInit();source.Freeze();
                        sheet=new FormatConvertedBitmap(source,PixelFormats.Bgra32,null,0);sheet.Freeze();
                    }
                    for(int n=0;n<64 && start+n<clip.Count;n++)
                    {
                        string key=clip.Name+"-"+(start+n);
                        var pixels=new byte[w*h*4];
                        sheet.CopyPixels(new Int32Rect(n%8*w,n/8*h,w,h),pixels,w*4,0);
                        var image=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,pixels,w*4);image.Freeze();images[key]=image;
                        var mask=new byte[w*h];for(int i=0;i<mask.Length;i++)mask[i]=pixels[i*4+3];masks[key]=mask;
                    }
                }
            }
        }
        public int SampleCount {get {int n=0;foreach(var c in clips.Values)n+=c.Count;return n;}}
        public bool FramesReady {get {return images.Count==SampleCount && masks.Count==SampleCount;}}
        public Rect Bounds {get {return current.Bounds;}}
        public string ClipName {get {return current.Name;}}
        public int Frame {get {return index;}}
        public static string CardClip(string skill)
        {
            return CardAnimation.For(skill).Clip;
        }
        public static double ReleaseDelay(string skill)
        {return CardAnimation.For(skill).Release;}
        public void Play(string skill,double now)
        {previous=Image;previousBounds=Bounds;mixedAt=now;score=CardAnimation.For(skill);requested=score.Clip;started=now;}
        public void Select(double now,string pose,double poseSince,bool dragging)
        {
            string name="idle_loop";double time=now;
            if(!dragging && requested!=null && now-started<score.Duration)
            {name=requested;time=score.PoseTime(now-started);}
            else if(!dragging && pose=="failed") {name="hurt";time=Math.Min(clips[name].Count/60.0-.001,now-poseSince);}
            else if(!dragging && pose=="waving") {name="cast";time=Math.Min(clips[name].Count/60.0-.001,now-poseSince);}
            else if(!dragging && requested!="art-overclock" && pose=="running" && now-started>clips[CardClip("battery")].Count/60.0)
            {name="process";time=.55;}
            if(current.Name!=name)
            {previous=Image;previousBounds=Bounds;if(now-mixedAt>.11)mixedAt=now;current=clips[name];}
            index=Math.Max(0,Math.Min(current.Count-1,(int)(Math.Max(0,time)*60)%current.Count));
            if(history.Count==0 || now-history[history.Count-1].Time>.0001)
            {
                history.Add(new NativePoseSample{Time=now,Image=Image,Bounds=Bounds});
                if(history.Count>18)history.RemoveAt(0);
            }
        }
        public NativePoseSample Past(double time)
        {
            for(int i=history.Count-1;i>=0;i--)if(history[i].Time<=time)return history[i];
            return history.Count>0?history[0]:new NativePoseSample{Time=time,Image=Image,Bounds=Bounds};
        }
        public BitmapSource Image
        {
            get
            {
                string key=current.Name+"-"+index;BitmapSource image;
                if(images.TryGetValue(key,out image))return image;
                var sheet=GameArt.Bitmap("native-"+current.Name+"-"+(index/64));
                int w=(int)Bounds.Width,h=(int)Bounds.Height,n=index%64;
                var crop=new CroppedBitmap(sheet,new Int32Rect(n%8*w,n/8*h,w,h));crop.Freeze();images[key]=crop;return crop;
            }
        }
        public Point Anchor(int bone)
        {var p=current.Anchors[index][bone];return new Point(p.X+50,p.Y+104);}
        public bool Hit(Point p)
        {
            if(!Bounds.Contains(p))return false;
            string key=current.Name+"-"+index;byte[] mask;
            int w=(int)Bounds.Width,h=(int)Bounds.Height;
            if(!masks.TryGetValue(key,out mask))
            {
                var image=new FormatConvertedBitmap(Image,PixelFormats.Bgra32,null,0);
                var pixels=new byte[w*h*4];image.CopyPixels(pixels,w*4,0);mask=new byte[w*h];
                for(int i=0;i<mask.Length;i++)mask[i]=pixels[i*4+3];masks[key]=mask;
            }
            return mask[(int)(p.Y-Bounds.Y)*w+(int)(p.X-Bounds.X)]>24;
        }
        public void Draw(DrawingContext dc,double now,bool attached=false)
        {
            double mix=attached?1:Math.Max(0,Math.Min(1,(now-mixedAt)/.12));
            if(previous!=null && mix<1) {dc.PushOpacity(1-mix);dc.DrawImage(previous,previousBounds);dc.Pop();}
            dc.PushOpacity(mix);dc.DrawImage(Image,Bounds);dc.Pop();
        }
    }
}
