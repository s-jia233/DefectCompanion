using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed class ModHeadPose
    {
        public readonly Point Red,Blue,RedMount,BlueMount,Arc;
        public readonly double Angle;
        public ModHeadPose(double rx,double ry,double rmx,double rmy,double bx,double by,double bmx,double bmy,double ax,double ay,double angle)
        {Red=new Point(rx,ry);Blue=new Point(bx,by);RedMount=new Point(rmx,rmy);BlueMount=new Point(bmx,bmy);Arc=new Point(ax,ay);Angle=angle;}
    }
    public sealed class CableShape
    {
        public float Length,Radius,Drop;
        public Point[][] Frames;
    }
    public static partial class InstalledModArt
    {
        public static readonly Dictionary<string,ModHeadPose[]> HeadPoses=new Dictionary<string,ModHeadPose[]>();
        public static readonly Dictionary<string,NativeClip> IceClips=new Dictionary<string,NativeClip>();
        public static readonly Dictionary<string,NativeClip> ColdHeadClips=new Dictionary<string,NativeClip>();
        public static readonly Dictionary<string,Rect> BufferRects=new Dictionary<string,Rect>();
        private static readonly Dictionary<string,BitmapSource[]> clips=new Dictionary<string,BitmapSource[]>();
        public static readonly CableShape[] Cables=new CableShape[5];
        static partial void Bind();
        static partial void BindBuffer();
        static partial void BindColdHead();
        static partial void BindCardPoses();
        static InstalledModArt()
        {
            Bind();BindBuffer();BindColdHead();BindCardPoses();
            foreach(var clip in ColdHeadClips.Values)Load("cold-head-"+clip.Name,clip.Count,(int)clip.Bounds.Width,(int)clip.Bounds.Height);
            Load("wall",108,320,267);
            Load("ice-snow",16,128,128,4);Load("ice-mist",16,128,128,4);
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("assets.mod-cable-prefabs.bin"))
            using(var reader=new BinaryReader(stream))
            {
                if(reader.ReadInt32()!=1129337393 || reader.ReadInt32()!=5 || reader.ReadInt32()!=3 || reader.ReadInt32()!=120 || reader.ReadInt32()!=53)throw new InvalidDataException("电线动作资源不完整");
                for(int k=0;k<5;k++)
                {
                    var s=new CableShape{Length=reader.ReadSingle(),Radius=reader.ReadSingle(),Drop=reader.ReadSingle()};reader.ReadSingle();reader.ReadSingle();s.Frames=new Point[360][];
                    for(int t=0;t<360;t++){var row=new Point[53];for(int i=0;i<53;i++)row[i]=new Point(reader.ReadSingle(),reader.ReadSingle());s.Frames[t]=row;}Cables[k]=s;
                }
            }
        }
        public static bool Ready {get {return clips.Count==26 && HeadPoses.Count==23 && ColdHeadClips.Count==23;}}
        public static bool MovingHeadReady {get {return Ready && (HeadPoses["process"][0].Red-HeadPoses["process"][35].Red).Length>1 && (HeadPoses["attack"][0].Blue-HeadPoses["attack"][35].Blue).Length>1;}}
        public static bool Replaces(string skill)
        {return skill=="frost" || skill=="battery" || skill=="buffer" || skill=="defragment" || skill=="hologram" || skill=="turbo" || skill=="ice_lance" || skill=="dualcast" || skill=="meteor_strike";}
        public static BitmapSource Frame(string name,int index)
        {var frames=clips[name];return frames[Math.Max(0,Math.Min(frames.Length-1,index))];}
        private static void Load(string name,int count,int w,int h,int columns=8)
        {
            var frames=new BitmapSource[count];
            for(int start=0;start<count;start+=64)
            {
                BitmapSource sheet;
                using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("assets.mod-"+name+"-"+(start/64)+".png"))
                {var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();sheet=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);}
                for(int n=0;n<64 && start+n<count;n++)
                {var data=new byte[w*h*4];sheet.CopyPixels(new Int32Rect(n%columns*w,n/columns*h,w,h),data,w*4,0);var image=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,data,w*4);image.Freeze();frames[start+n]=image;}
            }
            clips[name]=frames;
        }
    }
}
