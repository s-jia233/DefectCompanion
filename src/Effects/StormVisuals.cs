using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        // Palette and broad brush planes follow the installed art-storm portrait.
        private static readonly Color stormInk=Color.FromRgb(17,13,37),stormPlum=Color.FromRgb(49,28,70),stormPurple=Color.FromRgb(91,49,120),stormLilac=Color.FromRgb(162,78,177),stormHot=Color.FromRgb(211,0,255);
        private readonly Dictionary<BitmapSource,BitmapSource> stormSurfaces=new Dictionary<BitmapSource,BitmapSource>();
        private static double StormBeat(double age)
        {return age<0 || age>.43?0:Ease(age/.035)*(1-Ease((age-.13)/.30));}
        private static double StormStrength(double t)
        {return Math.Max(StormBeat(t-.54),Math.Max(StormBeat(t-1.12),StormBeat(t-1.70)));}
        private static Point StormEye {get{return new Point(270,153);}}
        private static Point CloudPoint(Point eye,double r,double angle,double yOffset)
        {return new Point(eye.X+Math.Cos(angle)*r,eye.Y+Math.Sin(angle)*r*.56+yOffset);}

        private void DrawStormCloud(DrawingContext dc,double t,double soft)
        {
            if(t<0 || t>2.5)return;
            double grow=Ease(t/.49),fade=Window(t,.015,1.86,2.48)*soft,flash=StormStrength(t);
            Point eye=StormEye,center=new Point(262,175);
            // A wide, dense silhouette, rather than a small cloud perched on the head.
            dc.DrawEllipse(Brush(stormInk,fade*.28),null,new Point(251,332),104*grow,17*grow);
            dc.PushTransform(new ScaleTransform(.35+.65*grow,.22+.78*grow,eye.X,eye.Y));
            var silhouette=new Point[44];
            for(int i=0;i<silhouette.Length;i++)
            {
                double a=i*Math.PI*2/silhouette.Length;
                double r=1+.055*Math.Sin(i*1.31)+.025*Math.Cos(i*2.7);
                silhouette[i]=new Point(center.X+Math.Cos(a)*134*r,center.Y+Math.Sin(a)*73*r);
            }
            dc.DrawGeometry(Brush(stormInk,fade*.97),null,Face(silhouette));
            // Unequal broad rotating cloud tongues have separate dark undersides,
            // mid-tone facets and lilac illuminated faces. All are behind the body.
            for(int layer=0;layer<3;layer++)
            {
                double turn=(layer==0?0:layer==1?2.18:4.47)-t*.24;
                StormCloudBand(dc,eye,turn,1.0,stormPlum,fade*.98,0);
                StormCloudBand(dc,eye,turn+.055,.78,stormPurple,fade*(.90+flash*.08),1);
                StormCloudBand(dc,eye,turn+.115,.32,stormLilac,fade*(.66+flash*.22),2);
                // Broken shadow cuts interrupt the spiral instead of drawing
                // uniform concentric bands. These broad shapes follow the portrait.
                double cut=turn+1.75;
                dc.DrawGeometry(Brush(stormInk,fade*.74),null,Face(
                    CloudPoint(eye,64,cut,10),CloudPoint(eye,89,cut+.08,12),CloudPoint(eye,119,cut+.19,14),
                    CloudPoint(eye,108,cut+.36,14),CloudPoint(eye,87,cut+.27,12),CloudPoint(eye,82,cut+.48,10),CloudPoint(eye,66,cut+.28,8)));
            }
            // Inner rolling wall and the black storm eye give the vortex depth.
            dc.DrawEllipse(Brush(stormInk,fade*.95),null,eye,26,16);
            for(int i=0;i<3;i++)
            {
                double a=i*2.094-t*.54;
                Point[] cloud={CloudPoint(eye,18,a,0),CloudPoint(eye,36,a+.33,0),CloudPoint(eye,33,a+.88,0),CloudPoint(eye,15,a+1.22,0),CloudPoint(eye,23,a+.65,0)};
                dc.DrawGeometry(Brush(i==1?stormLilac:stormPurple,fade*.88),null,Face(cloud));
            }
            Glow(dc,eye,29+flash*15,stormHot,fade*(.15+flash*.48));
            if(t>.35)
            {
                double charge=Ease((t-.35)/.19);
                dc.DrawGeometry(Brush(Color.FromRgb(228,174,237),fade*(.55+flash*.45)*charge),null,Face(
                    eye+new Vector(-12,-4),eye+new Vector(-2,-10),eye+new Vector(7,-7),eye+new Vector(15,-12),
                    eye+new Vector(12,1),eye+new Vector(3,7),eye+new Vector(-4,2),eye+new Vector(-10,6)));
                dc.DrawGeometry(Brush(Colors.White,fade*flash),null,Face(eye+new Vector(-8,-1),eye+new Vector(3,-6),eye+new Vector(9,-4),eye+new Vector(5,2),eye+new Vector(-3,4)));
            }
            dc.Pop();
            // A fork across the top stays behind the head and hands.
            for(int beat=0;beat<3;beat++)
            {
                double age=t-(.54+beat*.58),pulse=StormBeat(age)*fade;
                if(pulse<.001)continue;
                var path=new Point[]{eye, new Point(249,139),new Point(239,145),new Point(222,124),new Point(201,130),new Point(188,117),new Point(160,127),new Point(143,119)};
                DrawStormLightning(dc,path,6.5,pulse*.88);
            }
        }
        private void StormCloudBand(DrawingContext dc,Point eye,double turn,double widthScale,Color color,double alpha,int facet)
        {
            const int n=48;var shape=new Point[n*2];
            for(int i=0;i<n;i++)
            {
                double u=i/(n-1.0),a=turn+u*3.78,r=22+u*91;
                // Broad, gently uneven painted cuts, rather than repeating teeth.
                double width=(23+u*32)*Math.Pow(Math.Sin(Math.PI*u),.42)*widthScale*(.88+.12*Math.Sin(u*13.7+turn));
                if(facet==2)width*=u<.12 || u>.80?0:Math.Pow(Math.Sin((u-.12)/.68*Math.PI),.6);
                double edge=(Math.Sin(i*.73)+Math.Sin(i*1.49)*.4)*1.35;
                double offset=facet==2?-8:facet==1?-2:0;
                shape[i]=CloudPoint(eye,r+width*.55+offset+edge,a,u*17);
                shape[shape.Length-1-i]=CloudPoint(eye,r-width*.53+offset+edge*.4,a,u*17);
            }
            dc.DrawGeometry(Brush(color,alpha),null,Face(shape));
        }

        private void DrawStormLightning(DrawingContext dc,Point[] path,double width,double alpha)
        {
            if(alpha<=.001)return;
            Color[] colors={stormHot,stormHot,stormInk,Color.FromRgb(252,245,255)};
            double[] sizes={1.85,1.37,1.05,.66};
            double[] opacities={.20,.88,1,1};
            for(int layer=0;layer<4;layer++)
            {
                var shape=new Point[path.Length*2];
                for(int i=0;i<path.Length;i++)
                {
                    Vector tangent=path[Math.Min(path.Length-1,i+1)]-path[Math.Max(0,i-1)];
                    Vector normal=new Vector(-tangent.Y,tangent.X);if(normal.Length<.001)normal=new Vector(1,0);normal.Normalize();
                    double u=i/(path.Length-1.0),profile=(i==path.Length-1?.07:.60+.40*Math.Sin(Math.PI*u))*(i%3==1?1.15:.93);
                    double half=width*sizes[layer]*profile*.5;
                    shape[i]=path[i]+normal*half;shape[shape.Length-1-i]=path[i]-normal*half;
                }
                dc.DrawGeometry(Brush(colors[layer],alpha*opacities[layer]),null,Face(shape));
            }
        }
        private BitmapSource StormSurface(BitmapSource frame)
        {
            BitmapSource image;if(stormSurfaces.TryGetValue(frame,out image))return image;
            if(stormSurfaces.Count>=24)stormSurfaces.Clear();
            int w=frame.PixelWidth,h=frame.PixelHeight;var data=new byte[w*h*4];frame.CopyPixels(data,w*4,0);
            for(int i=0;i<data.Length;i+=4)
            {
                double light=(data[i]*.15+data[i+1]*.60+data[i+2]*.25)/255;
                bool blue=data[i]>data[i+2]*1.25 && data[i+1]>data[i+2]*1.1;
                data[i]=(byte)(194+light*61);data[i+1]=(byte)(65+light*133);data[i+2]=(byte)(138+light*117);
                data[i+3]=(byte)(data[i+3]*(blue?.16:.63));
            }
            image=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,data,w*4);image.Freeze();stormSurfaces[frame]=image;return image;
        }
        private void DrawStormFront(DrawingContext dc,double t,double soft)
        {
            if(t<0 || t>2.5)return;
            double fade=Window(t,.015,1.86,2.48)*soft,flash=StormStrength(t);
            if(fade<=.001)return;
            dc.PushTransform(characterTransform);dc.PushOpacity(flash*fade*.37);dc.DrawImage(StormSurface(native.Image),native.Bounds);dc.Pop();dc.Pop();
            Point eye=StormEye;
            // The strongest branches stay to either side of the face. This clip also
            // protects it while returning from the raised-arm pose or being dragged.
            var clip=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(129,97,282,244)),new EllipseGeometry(ActionAnchor(3),29,33));clip.Freeze();dc.PushClip(clip);
            for(int beat=0;beat<3;beat++)
            {
                double age=t-(.54+beat*.58),pulse=StormBeat(age)*fade;
                if(pulse>.001)
                {
                    Point[] main=beat%2==0?
                        new Point[]{eye,new Point(283,172),new Point(273,185),new Point(304,203),new Point(292,223),new Point(328,245),new Point(319,264),new Point(357,300)}:
                        new Point[]{eye,new Point(282,176),new Point(301,181),new Point(290,204),new Point(321,224),new Point(307,241),new Point(345,263),new Point(371,294)};
                    DrawStormLightning(dc,main,23+beat*2,pulse);
                    Point fork=main[3];
                    DrawStormLightning(dc,new Point[]{fork,fork+new Vector(23,-5),fork+new Vector(28,10),fork+new Vector(50,4),fork+new Vector(61,21),fork+new Vector(82,19)},8.5,pulse*.95);
                    Point low=main[5];
                    DrawStormLightning(dc,new Point[]{low,low+new Vector(23,-2),low+new Vector(18,13),low+new Vector(40,25),low+new Vector(45,43)},7,pulse*.87);
                    StormGroundHit(dc,main[main.Length-1],age-.08,pulse);
                }
                double leftAge=age-.10,leftPulse=StormBeat(leftAge)*fade;
                if(leftPulse>.001)
                {
                    Point[] left={new Point(171,155),new Point(158,177),new Point(171,190),new Point(147,213),new Point(160,228),new Point(144,253),new Point(151,292)};
                    DrawStormLightning(dc,left,16,leftPulse*.94);
                    DrawStormLightning(dc,new Point[]{left[3],new Point(139,223),new Point(145,241),new Point(133,257)},6,leftPulse*.83);
                    StormGroundHit(dc,left[left.Length-1],leftAge-.08,leftPulse*.72);
                }
            }
            dc.Pop();
        }
        private void StormGroundHit(DrawingContext dc,Point end,double age,double alpha)
        {
            if(age<0 || age>.39)return;double u=Ease(age/.39);
            Glow(dc,end,25+u*22,stormHot,alpha*.42);
            Stamp(dc,GameArt.Bitmap("fx-flare"),end,54+u*31,24+u*13,-6,alpha*(1-u)*.91);
            // Short ground crawlers extend the impact, without a radial starburst.
            DrawStormLightning(dc,new Point[]{end,end+new Vector(-15,3),end+new Vector(-24,-3),end+new Vector(-39,6)},6,alpha*(1-u)*.8);
        }
    }
}
