using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private readonly Dictionary<Orb,double> artOrbBirth=new Dictionary<Orb,double>();
        private sealed class EnergyFrame {public BitmapSource Color,Edge;}
        private readonly Dictionary<BitmapSource,EnergyFrame> energyFrames=new Dictionary<BitmapSource,EnergyFrame>();
        private double artEchoAt=-100,artEchoSpent=-100;
        private static readonly Color artViolet=Color.FromRgb(177,77,255),artCyan=Color.FromRgb(76,224,255);
        private static bool IsCardArt(string skill)
        {return skill=="dark" || skill=="echo" || skill=="rocket" || skill=="hyperbeam" || skill=="dualcast" || skill=="tempest" || skill=="glacier" || skill=="rainbow" || skill=="thunder" || skill=="storm" || skill=="turbo" || skill=="claw" || skill=="ball_lightning" || skill=="ice_lance" || skill=="overclock" || skill=="machine_learning" || skill=="plasma";}
        private double OrbVisualBirth(Orb orb)
        {double at;return artOrbBirth.TryGetValue(orb,out at)?at:orb.Born;}
        private void CleanupArtOrbs()
        {
            var remove=new List<Orb>();foreach(var orb in artOrbBirth.Keys)if(!owner.Engine.Orbs.Contains(orb))remove.Add(orb);
            foreach(var orb in remove)artOrbBirth.Remove(orb);
        }
        private void ArtEvent(PetEvent e,double now)
        {
            KnowledgeEvent(e,now);
            if(e.Kind=="reboot"){artOrbBirth.Clear();artEchoAt=artEchoSpent=-100;return;}
            if(e.Kind=="echo-ready")artEchoAt=now;
            if(e.Kind=="echo")artEchoSpent=now+e.Delay;
            if(e.Kind=="channel" && e.Card=="ball_lightning" && e.Created!=null)
            {artOrbBirth[e.Created]=now+e.Delay+1.10;channelOrigins[e.Created]=BallImpact;}
            if(e.Kind=="channel" && e.Card=="ice_lance" && e.Created!=null)
            {artOrbBirth[e.Created]=now+e.Delay+1.28;channelOrigins[e.Created]=new Point(399,203);}
            if(e.Kind=="channel" && e.Card=="dark" && e.Created!=null)
            {artOrbBirth[e.Created]=now+e.Delay+.98;channelOrigins[e.Created]=new Point(225,221);}
            if(e.Kind=="channel" && e.Card=="plasma" && e.Created!=null)
            {artOrbBirth[e.Created]=now+e.Delay+1.52;channelOrigins[e.Created]=FusionCore;}
            if(e.Kind=="channel" && e.Created!=null && (e.Card=="rainbow" || e.Card=="tempest" || e.Card=="glacier"))
            {
                double delay=e.Card=="rainbow"?.90:e.Card=="tempest"?.52:.44;
                artOrbBirth[e.Created]=now+e.Delay+delay;
                channelOrigins[e.Created]=e.Card=="rainbow"?RainbowEnd(e.Orb):e.Card=="tempest"?new Point(207,251):new Point(278,222);
            }
        }
        private EnergyFrame Energy(BitmapSource source)
        {
            EnergyFrame result;if(energyFrames.TryGetValue(source,out result))return result;
            if(energyFrames.Count>=32)energyFrames.Clear();
            int w=source.PixelWidth,h=source.PixelHeight;var rgba=new byte[w*h*4];source.CopyPixels(rgba,w*4,0);
            var color=new byte[rgba.Length];var edge=new byte[rgba.Length];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                int i=(y*w+x)*4;int b=rgba[i],g=rgba[i+1],r=rgba[i+2],a=rgba[i+3];
                if(a==0)continue;
                double light=(r*.25+g*.6+b*.15)/255;
                bool scarf=g>r*1.32 && g>b*.87 && r<115;
                color[i]=(byte)Math.Min(255,90+light*165);color[i+1]=(byte)Math.Min(255,38+light*207);color[i+2]=(byte)Math.Min(255,9+light*137);
                color[i+3]=(byte)(a*(scarf?.10:.96));
                bool outline=x==0 || y==0 || x==w-1 || y==h-1;
                if(!outline)outline=rgba[i-4+3]<50 || rgba[i+4+3]<50 || rgba[i-w*4+3]<50 || rgba[i+w*4+3]<50;
                if(outline){edge[i]=255;edge[i+1]=231;edge[i+2]=128;edge[i+3]=(byte)a;}
            }
            result=new EnergyFrame();result.Color=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,color,w*4);result.Color.Freeze();
            result.Edge=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,edge,w*4);result.Edge.Freeze();energyFrames[source]=result;return result;
        }
        private void DrawArtRear(DrawingContext dc,double t,double now)
        {
            foreach(var e in effects)
                if(e.Card=="dualcast" && (e.Kind=="evoke" || e.Kind=="evoke-delay") && now>=e.Start)DrawArtDualLayer(dc,e,now-e.Start,true);
            double active=owner.Engine.EchoReady?Ease((now-artEchoAt)/.48):1-Ease((now-artEchoSpent)/.65);
            if(active<=0 || now<artEchoSpent)return;
            double soft=owner.Options.Quiet?.24:1;Point core=ActionAnchor(2);
            for(int i=0;i<5;i++)
            {
                double phase=(now*.65+i*.2)%1;
                Point p=new Point(core.X+(i%2==0?-1:1)*(38+phase*24),core.Y+27-phase*95);
                var path=new StreamGeometry();using(var g=path.Open())
                {g.BeginFigure(new Point(p.X,p.Y+25),false,false);g.BezierTo(new Point(p.X-14,p.Y+10),new Point(p.X+11,p.Y+3),new Point(p.X,p.Y-17),true,false);}
                dc.DrawGeometry(null,new Pen(Brush(artCyan,active*soft*Math.Sin(phase*Math.PI)*.7),1.4),path);
            }
        }
        private void DrawArtFront(DrawingContext dc,double t,double now)
        {
            double soft=owner.Options.Quiet?.26:1;
            double active=owner.Engine.EchoReady?Ease((now-artEchoAt)/.48):1-Ease((now-artEchoSpent)/.65);
            if(active>0)
            {
                var image=Energy(native.Image);dc.PushTransform(characterTransform);
                dc.PushOpacity(active*(.84+.08*Math.Sin(now*4)));dc.DrawImage(image.Color,native.Bounds);dc.Pop();
                dc.PushOpacity(active*soft*.85);dc.DrawImage(image.Edge,native.Bounds);dc.Pop();dc.Pop();
            }
            if(t<0 || t>CardAnimation.For(castingSkill).Duration)return;
            DrawHandCardFront(dc,t,soft);
            Point core=ActionAnchor(2),hand=ActionAnchor(1);
            if(castingSkill=="hyperbeam" && t<.64)
            {
                double charge=Ease(t/.58),fade=Window(t,.07,.55,.70)*soft;
                for(int i=0;i<12;i++)
                {
                    double phase=(t*1.6+i/12.0)%1;double a=i*2.399;
                    double r=8+(1-phase)*(1-phase)*53;
                    Line(dc,artCyan,fade*phase*.8,1.1,Around(core,r+7,r+7,a),Around(core,r,r,a));
                }
                for(int i=0;i<3;i++)Arc(dc,core,7+(1-charge)*(18+i*7),11+(1-charge)*(18+i*7),t*4+i*2,2.7,artCyan,fade*.7,1.3);
                Glow(dc,core,12+charge*14,artCyan,fade*.4);Spark(dc,core,2+charge*5,Colors.White,fade*.9);
            }
        }
        private void DrawArtEchoTrace(DrawingContext dc,double t)
        {
            double fade=Math.Sin(Math.Min(1,t/1.3)*Math.PI)*(owner.Options.Quiet?.14:.48);
            for(int i=1;i<=2;i++)
            {
                var sample=native.Past(owner.Seconds-.12*i);var energy=Energy(sample.Image);
                Rect rect=characterTransform.TransformBounds(sample.Bounds);rect.Offset(-i*(12+14*t),-i*2);
                dc.PushOpacity(fade/i);dc.DrawImage(energy.Color,rect);dc.DrawImage(energy.Edge,rect);dc.Pop();
            }
        }
        private bool DrawArtEvent(DrawingContext dc,VisualEffect e,double t)
        {
            if(e.Card=="plasma" && e.Kind=="channel")return true;
            if(DrawHandCardEvent(dc,e,t))return true;
            double soft=owner.Options.Quiet?.26:1;
            if(e.Kind=="echo-ready")return true;
            if(e.Kind=="channel" && e.Card=="dark"){DrawArtDark(dc,e,t,soft);return true;}
            if(e.Kind=="rocket"){DrawArtRocket(dc,e,t,soft);return true;}
            if(e.Kind=="hyperbeam"){DrawArtBeam(dc,e,t,soft);return true;}
            return false;
        }
        private void DrawArtDark(DrawingContext dc,VisualEffect e,double t,double soft)
        {
            if(t>.99)return;
            Point a=ActionAnchor(0),b=ActionAnchor(1);Point cup=new Point((a.X+b.X)*.5,(a.Y+b.Y)*.5-23);
            if(!e.Launch.HasValue || castingSkill=="dark" && t<.72)e.Launch=cup;
            cup=e.Launch.Value;double size=22*Ease(t/.72),fade=Ease(t/.18)*(1-Ease((t-.91)/.08));
            if(size<.1)return;
            Glow(dc,cup,size+21,artViolet,fade*.4*soft);
            dc.DrawEllipse(Brush(Color.FromRgb(27,19,47),fade),new Pen(Brush(artViolet,fade),1.6),cup,size,size);
            for(int i=0;i<3;i++)
            {
                double start=t*2.2+i*Math.PI*2/3;
                Arc(dc,cup,size*.74,size*.72,start,1.8,i%2==0?Color.FromRgb(119,106,229):Color.FromRgb(205,131,255),fade*.95,Math.Max(1,size*.19));
            }
            for(int i=0;i<8;i++)
            {
                double angle=i*2.399+t*.9;Point p=Around(cup,size*(.55+.33*Math.Sin(i*3.1)),size*.75,angle);
                Spark(dc,p,.9+i%3*.5,Color.FromRgb(233,222,255),fade*(.5+.3*Math.Sin(t*3+i))*soft);
            }
            Glow(dc,a,19,artViolet,fade*.25*soft);Glow(dc,b,19,artViolet,fade*.25*soft);
        }
        private void DrawArtRocket(DrawingContext dc,VisualEffect e,double t,double soft)
        {
            if(t>.83)return;
            if(!e.Launch.HasValue)e.Launch=ActionAnchor(1);
            Point start=e.Launch.Value,target=new Point(386,start.Y-12),p;double size;
            if(t<.23)
            {double u=1-Math.Pow(1-t/.23,3);p=new Point(start.X+(target.X-start.X)*u,start.Y+(target.Y-start.Y)*u);size=31+u*26;}
            else if(t<.31){p=target;size=57-3*Ease((t-.23)/.08);}
            else
            {double u=Ease((t-.31)/.52);Point wrist=ActionAnchor(1);p=new Point(target.X+(wrist.X+7-target.X)*u,target.Y+(wrist.Y-1-target.Y)*u);size=54-25*u;}
            if(t<.28)
            {
                Vector direction=target-start;direction.Normalize();Vector n=new Vector(-direction.Y,direction.X);
                double length=22+Math.Min(1,t/.07)*53;
                dc.DrawGeometry(Brush(Color.FromRgb(214,237,234),.26*soft),null,Face(p+n*9,p-direction*length+n*18,p-direction*(length*.82)-n*15,p-n*9));
                Line(dc,Color.FromRgb(243,249,238),.85*soft,2.7,p-direction*length*.8,p);
                for(int i=0;i<3;i++)Line(dc,Color.FromRgb(226,241,235),.5*soft,1,new Point(start.X-7-i*10,start.Y-10+i*10),new Point(p.X-15,start.Y-10+i*10));
            }
            var fist=GameArt.Bitmap("cast-fist");
            Stamp(dc,fist,p,size,size,-8,1);
            double hit=t-.23;Impact(dc,target,hit,Color.FromRgb(234,234,200),soft*(1+.1*Math.Min(3,e.Strength-1)));
            if(hit>=0 && hit<.23)
            {double spread=Ease(hit/.23);Arc(dc,target,4+spread*17,12+spread*32,-1.4,2.8,Color.FromRgb(241,247,232),(1-spread)*soft,2);}
        }
        private void DrawArtBeam(DrawingContext dc,VisualEffect e,double t,double soft)
        {
            if(t>1.12)return;
            Point source=ActionAnchor(2),target=new Point(417,302);Vector delta=target-source;double length=delta.Length;
            double angle=Math.Atan2(delta.Y,delta.X)*180/Math.PI;
            double power=Ease(t/.085)*(1-Ease((t-.82)/.30));
            if(power<=0)return;
            dc.PushTransform(new TranslateTransform(source.X,source.Y));dc.PushTransform(new RotateTransform(angle));
            for(int layer=0;layer<3;layer++)
            {
                double w=(layer==0?35:layer==1?21:8)*power;
                var shape=new StreamGeometry();using(var g=shape.Open())
                {g.BeginFigure(new Point(0,-w*.13),true,true);g.LineTo(new Point(length,-w),true,false);g.LineTo(new Point(length,w),true,false);g.LineTo(new Point(0,w*.13),true,false);}
                dc.DrawGeometry(Brush(layer==0?Color.FromRgb(55,162,244):layer==1?artCyan:Colors.White,(layer==0?.36:layer==1?.78:.96)*power*soft),null,shape);
            }
            for(int i=0;i<4;i++)
            {
                double u=(t*1.55+i*.25)%1;double x=12+u*(length-12),radius=9+u*17;
                Arc(dc,new Point(x,0),radius*.32,radius,t*4+i,4.1,Colors.White,(1-u*.5)*power*soft,1.9);
            }
            dc.Pop();dc.Pop();
            Glow(dc,source,29,artCyan,power*.4*soft);
            for(int i=0;i<9;i++)
            {
                double a=i*Math.PI*2/9+t*.3;double reach=17+(i%3)*6;
                Point p=Around(target,reach*power,reach*power,a);
                Line(dc,i%2==0?Colors.White:artCyan,power*soft,2.3,target,p);
            }
        }
        private static Point Cubic(Point a,Point b,Point c,Point d,double t)
        {double u=1-t;return new Point(a.X*u*u*u+3*b.X*u*u*t+3*c.X*u*t*t+d.X*t*t*t,a.Y*u*u*u+3*b.Y*u*u*t+3*c.Y*u*t*t+d.Y*t*t*t);}
        private void DrawArtDual(DrawingContext dc,VisualEffect e,double t)
        {DrawArtDualLayer(dc,e,t,false);}
        private void DrawArtDualLayer(DrawingContext dc,VisualEffect e,double t,bool rear)
        {
            DrawModDual(dc,e,t,rear);
        }
    }
}
