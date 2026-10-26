using System;
using System.Windows;
using System.Windows.Media;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private static readonly Color stormGold=Color.FromRgb(255,207,41),iceMint=Color.FromRgb(91,216,222);
        private static int RainbowLane(OrbKind kind){return kind==OrbKind.Lightning?0:kind==OrbKind.Frost?1:2;}
        private static Point RainbowEnd(OrbKind kind){int lane=RainbowLane(kind);return new Point(296+lane*18,166-lane*17);}
        private void DrawElementalRear(DrawingContext dc,double time,double now)
        {
            foreach(var e in effects)
            {
                double t=now-e.Start;if(t<0)continue;
                if(e.Card=="rainbow" && e.Kind=="channel")DrawRainbowSweep(dc,e,t,true);
                if(e.Card=="glacier" && e.Kind=="glacier")DrawGlacier(dc,t,true);
            }
            if(castingSkill=="tempest" && time>=0 && time<2.3)DrawTempestCore(dc,time,true);
        }
        private void DrawElementalFront(DrawingContext dc,double time)
        {if(castingSkill=="tempest" && time>=0 && time<2.3)DrawTempestCore(dc,time,false);}
        private bool DrawElementalEvent(DrawingContext dc,VisualEffect e,double t)
        {
            if(e.Card=="rainbow" && e.Kind=="channel"){DrawRainbowSweep(dc,e,t,false);return true;}
            if(e.Card=="glacier" && e.Kind=="glacier"){DrawGlacier(dc,t,false);return true;}
            if(e.Card=="tempest" && e.Kind=="thunder"){DrawTempestBolt(dc,e,t);return true;}
            if((e.Card=="glacier" || e.Card=="tempest") && e.Kind=="channel")return true;
            return false;
        }
        private void GoldenBolt(DrawingContext dc,Point source,Point target,double clock,double alpha)
        {
            Vector delta=target-source,n=new Vector(-delta.Y,delta.X);n.Normalize();
            var points=new Point[10];points[0]=source;points[9]=target;
            int step=(int)(clock*22);
            for(int i=1;i<9;i++)
            {double v=i/9.0;points[i]=source+delta*v+n*(Math.Sin(i*19.4+step*2.3)*10*(i%2==0?1:.45));}
            var path=CablePath(points);
            dc.DrawGeometry(null,new Pen(Brush(stormGold,alpha*.25),7),path);
            dc.DrawGeometry(null,new Pen(Brush(stormGold,alpha),2.8),path);
            dc.DrawGeometry(null,new Pen(Brush(Color.FromRgb(255,255,216),alpha),.9),path);
            for(int i=3;i<8;i+=3)
            {Point end=points[i]+n*(i==3?19:-23)+delta*.11;Line(dc,stormGold,alpha*.8,1.2,points[i],end);}
        }
        private void DrawTempestCore(DrawingContext dc,double t,bool rear)
        {
            double soft=owner.Options.Quiet?.25:1,fade=Window(t,.05,1.48,2.25)*soft;
            Point core=ActionAnchor(2);double charge=Ease(t/.54);
            if(rear)
            {
                Glow(dc,core,47+charge*13,stormGold,fade*.27);
                for(int i=0;i<13;i++)
                {
                    double phase=(t*.8+i*.173)%1;double x=146+(i*61%151)+phase*18,y=116+phase*187;
                    Line(dc,Color.FromRgb(80,208,225),fade*.22,.8,new Point(x-7,y-15),new Point(x,y));
                }
            }
            for(int i=0;i<3;i++)
            {
                double angle=t*2.4+i*Math.PI*2/3;
                var path=new StreamGeometry();
                using(var g=path.Open())
                {
                    Point a=Around(core,40+charge*16,57,angle),b=Around(core,43+charge*19,62,angle+1.05),c=Around(core,38+charge*18,51,angle+2.05);
                    g.BeginFigure(a,false,false);g.QuadraticBezierTo(b,c,true,false);
                }
                if((i%2==0)==rear)
                {
                    dc.DrawGeometry(null,new Pen(Brush(Color.FromRgb(20,136,170),fade*.66),12),path);
                    dc.DrawGeometry(null,new Pen(Brush(Color.FromRgb(59,215,221),fade*.85),6),path);
                    dc.DrawGeometry(null,new Pen(Brush(Color.FromRgb(193,242,102),fade*.45),1.2),path);
                }
            }
            if(!rear)
            {
                double pulse=.88+.12*Math.Sin(t*19);
                Glow(dc,core,29,stormGold,fade*.43);
                dc.DrawEllipse(Brush(stormGold,fade*.28),new Pen(Brush(stormGold,fade*.9),2.5),core,(14+charge*8)*pulse,(14+charge*8)*pulse);
                Spark(dc,core,(5+charge*7)*pulse,Color.FromRgb(255,255,221),fade);
                if(t<1.6)
                {
                    GoldenBolt(dc,ActionAnchor(0),core,t,fade*.72);
                    GoldenBolt(dc,ActionAnchor(1),core,t+.07,fade*.72);
                }
            }
        }
        private void DrawTempestBolt(DrawingContext dc,VisualEffect e,double t)
        {
            if(t>.55)return;double fade=(1-Ease((t-.12)/.43))*(owner.Options.Quiet?.25:1);
            int lane=(int)(e.Start*100)%3;
            Point target=new Point(321+lane*29,272+lane%2*12),source=new Point(target.X-36,102);
            GoldenBolt(dc,source,target,t,fade);
            Glow(dc,target,30,stormGold,fade*.35);Impact(dc,target,t,stormGold,fade);
            for(int i=0;i<4;i++)
            {double a=i*1.7+t*3;Point p=Around(target,12+t*50,8+t*26,a);Spark(dc,p,1.5,stormGold,fade*.7);}
        }
        private static StreamGeometry Face(params Point[] points)
        {
            var shape=new StreamGeometry();using(var g=shape.Open())
            {g.BeginFigure(points[0],true,true);for(int i=1;i<points.Length;i++)g.LineTo(points[i],true,false);}shape.Freeze();return shape;
        }
        private void IceSpire(DrawingContext dc,double x,double floor,double height,double width,double grow,double age,double alpha)
        {
            double fracture=Ease((age-1.55)/.55);
            dc.PushTransform(new TranslateTransform(x,floor));
            dc.PushTransform(new ScaleTransform(1,grow));
            double drift=fracture*(x<230?-12:17);
            dc.PushTransform(new TranslateTransform(drift,fracture*18));
            Point bottom=new Point(-width*.55,0),right=new Point(width*.6,-height*.26),tip=new Point(width*.09,-height),left=new Point(-width*.49,-height*.49),mid=new Point(width*.03,-height*.39);
            dc.DrawGeometry(Brush(Color.FromRgb(10,51,74),alpha),new Pen(Brush(iceMint,alpha*.75),1.2),Face(bottom,right,tip,left));
            dc.DrawGeometry(Brush(Color.FromRgb(22,132,158),alpha),new Pen(Brush(iceMint,alpha*.8),.8),Face(bottom,mid,tip,left));
            dc.DrawGeometry(Brush(Color.FromRgb(65,192,199),alpha*.87),new Pen(Brush(Color.FromRgb(139,239,223),alpha*.9),1),Face(mid,right,tip));
            dc.DrawGeometry(Brush(Color.FromRgb(38,99,135),alpha),null,Face(bottom,right,mid));
            Line(dc,Color.FromRgb(128,237,228),alpha*.8,1.1,left,mid);
            Line(dc,Color.FromRgb(128,237,228),alpha*.8,1.1,mid,new Point(-width*.15,-height*.14));
            Point ridge=new Point(width*.02,-height*.91);
            dc.DrawGeometry(Brush(Color.FromRgb(222,239,241),alpha),null,Face(tip,new Point(-width*.10,-height*.81),left,new Point(-width*.44,-height*.40),new Point(-width*.25,-height*.51),new Point(-width*.14,-height*.51),ridge,new Point(width*.19,-height*.77),new Point(width*.22,-height*.87)));
            if(fracture>0)
                for(int i=0;i<3;i++)
                {Point p=new Point((i-1)*width*.3+drift*(i-1),-height*.45-fracture*(15+i*12));dc.DrawGeometry(Brush(iceMint,alpha*.8),null,Face(p,new Point(p.X+5,p.Y-12),new Point(p.X+10,p.Y+3)));}
            dc.Pop();dc.Pop();dc.Pop();
        }
        private void DrawGlacier(DrawingContext dc,double t,bool rear)
        {
            if(t>2.10)return;
            double alpha=(1-Ease((t-1.55)/.55))*(owner.Options.Quiet?.46:1);
            if(rear)
            {
                IceSpire(dc,169,336,104,52,Ease(t/.38),t,alpha);
                IceSpire(dc,277,336,181,76,Ease((t-.07)/.40),t,alpha);
                IceSpire(dc,325,336,112,60,Ease((t-.15)/.36),t,alpha);
            }
            else
            {
                IceSpire(dc,285,336,70,44,Ease((t-.12)/.38),t,alpha*.78);
                IceSpire(dc,149,336,58,33,Ease((t-.1)/.4),t,alpha*.86);
                for(int i=0;i<4;i++)
                    Stamp(dc,InstalledModArt.Frame("ice-mist",(int)(t*13+i*3)%16),new Point(151+i*51,329),88,30,0,alpha*.2);
                if(t>.3 && t<1.1)Spark(dc,new Point(282,159),3,Colors.White,alpha*(1-(t-.3)/.8));
            }
        }
        private void DrawRainbowSweep(DrawingContext dc,VisualEffect e,double t,bool rear)
        {
            double age=t-.18;if(age<0 || age>.78)return;
            int lane=RainbowLane(e.Orb);Color color=lane==0?Color.FromRgb(255,221,75):lane==1?Color.FromRgb(104,222,255):artViolet;
            Point start=new Point(153,292-lane*11),b=new Point(107,171-lane*8),c=new Point(221,104-lane*10),end=RainbowEnd(e.Orb);
            double u=Ease(age/.72),fade=(1-Ease((age-.69)/.09))*(owner.Options.Quiet?.3:1);
            Point p=Cubic(start,b,c,end,u);
            if(rear)
            {
                double tail=Math.Max(0,u-.68);var path=new StreamGeometry();using(var g=path.Open())
                {g.BeginFigure(Cubic(start,b,c,end,tail),false,false);for(int i=1;i<=28;i++)g.LineTo(Cubic(start,b,c,end,tail+(u-tail)*i/28.0),true,false);}
                dc.DrawGeometry(null,new Pen(Brush(color,fade*.19),24),path);
                dc.DrawGeometry(null,new Pen(Brush(color,fade*.82),11),path);
                dc.DrawGeometry(null,new Pen(Brush(Colors.White,fade*.72),2),path);
            }
            else
            {
                Glow(dc,p,28,color,fade*.45);
                Stamp(dc,GameArt.Bitmap("orb-"+GameArt.OrbName(e.Orb)),p,38,38,-age*22,fade);
                for(int i=0;i<3;i++){Point q=Around(p,23,23,age*4+i*2.1);Spark(dc,q,1.3,color,fade*.75);}
            }
        }
    }
}
