using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private static readonly Color thunderAmber=Color.FromRgb(178,126,40),thunderYellow=Color.FromRgb(255,202,12),thunderPale=Color.FromRgb(255,245,103),thunderInk=Color.FromRgb(66,18,103);
        private readonly Dictionary<BitmapSource,BitmapSource> thunderSurfaces=new Dictionary<BitmapSource,BitmapSource>();
        // Colour follows the actual Thunder portrait: ochre planes, broad yellow edges,
        // pale highlights and violet shadow. No circular sawtooth outline.
        private BitmapSource ThunderSurface(BitmapSource frame)
        {
            BitmapSource image;if(thunderSurfaces.TryGetValue(frame,out image))return image;
            if(thunderSurfaces.Count>=24)thunderSurfaces.Clear();
            int w=frame.PixelWidth,h=frame.PixelHeight;byte[] data=new byte[w*h*4];frame.CopyPixels(data,w*4,0);
            for(int i=0;i<data.Length;i+=4)
            {
                double light=(data[i]*.15+data[i+1]*.60+data[i+2]*.25)/255;
                bool scarf=data[i+1]>data[i+2]*1.32 && data[i+1]>data[i]*.87 && data[i+2]<115;
                data[i]=(byte)(25+light*67);data[i+1]=(byte)(100+light*147);data[i+2]=(byte)(180+light*75);
                data[i+3]=(byte)(data[i+3]*(scarf?.13:.65));
            }
            image=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,data,w*4);image.Freeze();thunderSurfaces[frame]=image;return image;
        }
        private void PaintedBolt(DrawingContext dc,Point[] path,double width,double alpha,bool gold)
        {
            if(alpha<=.001 || path.Length<2)return;
            Color shadow=gold?thunderInk:Color.FromRgb(37,75,113),edge=gold?thunderYellow:Color.FromRgb(126,198,250),body=gold?thunderAmber:Color.FromRgb(187,229,255),core=gold?thunderPale:Color.FromRgb(248,253,255);
            // Wide faceted sides, irregular thickness and an off-centre highlight.
            for(int layer=0;layer<4;layer++)
            {
                var face=new Point[path.Length*2];
                for(int i=0;i<path.Length;i++)
                {
                    Vector tangent=path[Math.Min(path.Length-1,i+1)]-path[Math.Max(0,i-1)];
                    Vector normal=new Vector(-tangent.Y,tangent.X);if(normal.Length<.001)normal=new Vector(1,0);normal.Normalize();
                    double u=i/(path.Length-1.0),taper=(i==0 || i==path.Length-1)?.035:(.33+.67*Math.Pow(Math.Sin(Math.PI*u),.35))*(i%3==0?1.12:.88);
                    double half=width*taper*(layer==0?.71:layer==1?.53:layer==2?.32:.085);
                    double offset=layer==2?width*.11:layer==3?-width*.13:0;
                    face[i]=path[i]+normal*(half+offset);face[face.Length-1-i]=path[i]+normal*(-half+offset);
                }
                dc.DrawGeometry(Brush(layer==0?shadow:layer==1?edge:layer==2?body:core,alpha*(layer==0?.53:layer==3?.95:1)),null,Face(face));
            }
        }
        private void NativeThunderBolt(DrawingContext dc,Point start,Point end,double width,double age,double alpha)
        {
            if(alpha<=.001)return;Vector delta=end-start;
            string variant=age<.09?"01b":age<.19?"03":"04";
            Stamp(dc,GameArt.Bitmap("fx-thunder-bolt-"+variant),new Point((start.X+end.X)*.5,(start.Y+end.Y)*.5),width,delta.Length,Math.Atan2(delta.Y,delta.X)*180/Math.PI-90,alpha);
        }
        private void DrawThunderPortraitRear(DrawingContext dc,double t,double soft)
        {
            double grow=Ease(t/.36),fade=Window(t,.06,1.20,1.94)*soft;
            if(fade<=.001)return;Point core=ActionAnchor(2);
            double beat=Math.Max(0,t-.36)%.42,surge=1-Ease(beat/.30);
            Glow(dc,new Point(core.X,core.Y-15),92,thunderYellow,fade*.18);
            dc.DrawGeometry(Brush(thunderInk,fade*(.22+surge*.12)),null,Face(
                core+new Vector(-68,50),core+new Vector(-88,5),core+new Vector(-76,-30),core+new Vector(-91,-69),core+new Vector(-49,-61),
                core+new Vector(-32,-110),core+new Vector(1,-84),core+new Vector(32,-114),core+new Vector(61,-74),core+new Vector(86,-97),
                core+new Vector(77,-33),core+new Vector(92,-9),core+new Vector(70,35),core+new Vector(45,63)));
            // Unequal rising columns, tied to the torso rather than a regular perimeter.
            double[][] columns={
                new double[]{-42,61,-65,32,-52,13,-73,-12,-55,-7,-69,-53,-46,-35,-39,-91},
                new double[]{-17,47,-40,3,-24,-5,-42,-40,-20,-31,-28,-87,-10,-66,-3,-111},
                new double[]{34,63,56,32,43,14,69,-12,51,-11,70,-58,51,-47,62,-99},
                new double[]{7,30,28,6,22,-15,45,-43,33,-34,39,-83,23,-70,27,-116}};
            dc.PushTransform(new ScaleTransform(.68+.32*grow,.38+.62*grow,core.X,core.Y+50));
            // Broad painted energy planes fill the gaps between the individual arcs.
            // Their separate shapes leave the face open and avoid a closed perimeter ring.
            dc.DrawGeometry(Brush(thunderAmber,fade*(.70+.18*surge)),new Pen(Brush(thunderYellow,fade*.92),3.1),Face(
                core+new Vector(-22,62),core+new Vector(-67,44),core+new Vector(-81,7),core+new Vector(-66,-4),
                core+new Vector(-91,-31),core+new Vector(-61,-27),core+new Vector(-63,-64),core+new Vector(-39,-48),
                core+new Vector(-39,-96),core+new Vector(-18,-69),core+new Vector(-10,-21)));
            dc.DrawGeometry(Brush(thunderAmber,fade*(.72+.18*surge)),new Pen(Brush(thunderYellow,fade*.94),3.1),Face(
                core+new Vector(29,54),core+new Vector(65,31),core+new Vector(53,4),core+new Vector(81,-16),
                core+new Vector(60,-18),core+new Vector(79,-65),core+new Vector(54,-51),core+new Vector(58,-103),
                core+new Vector(36,-73),core+new Vector(29,-34)));
            for(int lane=0;lane<columns.Length;lane++)
            {
                var values=columns[lane];var points=new Point[values.Length/2];
                double lanePulse=1-Ease((Math.Max(0,t-.36-lane*.025)%.42)/.30);
                for(int i=0;i<points.Length;i++)points[i]=new Point(core.X+values[i*2]*(.94+.09*lanePulse)+(i==0?0:Math.Sin(t*6+lane+i*.7)*1.1),core.Y+values[i*2+1]*(.86+.14*lanePulse));
                PaintedBolt(dc,points,(lane%2==0?31:24)*(.78+.32*lanePulse),fade*(lane%2==0?.94:.78),true);
            }
            dc.Pop();
            if(t>=.35 && t<1.58)
            {
                double beatAge=(t-.35)%.42,alpha=(1-Ease(beatAge/.30))*fade;
                Point shoulder=new Point(core.X,core.Y-49);
                NativeThunderBolt(dc,new Point(shoulder.X-40,shoulder.Y-26),ActionAnchor(0),39,beatAge,alpha*.80);
                NativeThunderBolt(dc,new Point(shoulder.X+37,shoulder.Y-40),ActionAnchor(1),46,beatAge+.03,alpha*.84);
            }
        }
        private void DrawThunderPortraitFront(DrawingContext dc,double t,double soft)
        {
            double fade=Window(t,.13,1.10,1.94)*soft;if(fade<=.001)return;
            dc.PushTransform(characterTransform);dc.PushOpacity(fade*.30);dc.DrawImage(ThunderSurface(native.Image),native.Bounds);dc.Pop();dc.Pop();
            double beat=Math.Max(0,t-.36)%.48,pulse=(1-Ease(beat/.26))*fade;
            for(int side=0;side<2;side++)
            {
                Point hand=ActionAnchor(side);Glow(dc,hand,24,thunderYellow,pulse*.46);
                if(t>.33)
                {
                    Point from=hand+new Vector(side==0?-12:12,-17),to=hand+new Vector(side==0?9:-6,12);
                    NativeThunderBolt(dc,from,to,23,beat,pulse*.8);
                }
            }
        }
        private void DrawThunderProc(DrawingContext dc,VisualEffect e,double t)
        {
            if(t<0 || t>.62)return;double soft=owner.Options.Quiet?.24:1;
            double fade=Ease(t/.025)*(1-Ease((t-.19)/.38))*soft;
            Point end=new Point(343,294),start=new Point(315,115);
            Vector delta=end-start;var points=new Point[]{start,start+delta*.17+new Vector(12,0),start+delta*.32+new Vector(-8,0),start+delta*.48+new Vector(13,0),start+delta*.64+new Vector(-8,0),start+delta*.82+new Vector(10,0),end};
            PaintedBolt(dc,points,14,fade*.93,true);
            NativeThunderBolt(dc,start,end,62,t,fade);
            for(int side=-1;side<=1;side+=2)
            {Point p=points[side<0?2:4];PaintedBolt(dc,new Point[]{p,p+new Vector(side*13,7),p+new Vector(side*9,19),p+new Vector(side*24,31)},4.5,fade*.8,true);}
            Glow(dc,end,36,thunderYellow,fade*.4);
            if(t>.09 && t<.44)
            {
                double hit=t-.09,u=Ease(hit/.35);
                Stamp(dc,GameArt.Bitmap("fx-thunder-flare"),end,40+u*44,22+u*20,-8,(1-u)*soft*.8);
            }
        }
    }
}
