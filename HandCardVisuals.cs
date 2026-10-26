using System;
using System.Windows;
using System.Windows.Media;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private static Point BallImpact {get{return new Point(369,247);}}
        private static readonly Color clawIce=Color.FromRgb(132,213,246),clawInk=Color.FromRgb(6,30,53),clawHeat=Color.FromRgb(231,47,4),ballGold=Color.FromRgb(245,230,0),ballPale=Color.FromRgb(255,249,187);
        private void DrawHandCardFront(DrawingContext dc,double t,double soft)
        {
            if(castingSkill=="claw" && t<.31)
            {
                Point wrist=ActionAnchor(1);double power=Window(t,.07,.24,.31)*soft;
                Glow(dc,wrist,16,clawIce,power*.22);
                for(int i=0;i<3;i++)
                {Point p=wrist+new Vector(4+i*5,-8-i*3);Line(dc,clawIce,power*.8,1.4,p,p+new Vector(5,-7));}
            }
            if(castingSkill=="ball_lightning" && t<.72)
            {
                double charge=Ease((t-.08)/.60),fade=Ease(t/.12)*soft;Point palm=ActionAnchor(1)+new Vector(6,-27);
                if(charge<=0)return;
                DrawHeldBall(dc,palm,5+charge*24,t,fade);
                for(int i=0;i<7;i++)
                {
                    double u=(t*1.3+i*.143)%1,a=i*2.399;
                    Point p=Around(palm,31+(1-u)*23,28+(1-u)*22,a);
                    Point q=Around(palm,28+(1-u)*21,25+(1-u)*20,a);
                    Line(dc,ballPale,fade*charge*u*.72,1.2,p,q);
                }
                Point wrist=ActionAnchor(1);
                PaintedBolt(dc,new Point[]{wrist+new Vector(-8,-1),wrist+new Vector(-13,-14),palm+new Vector(-16,11)},3.7,fade*charge*.8,true);
                PaintedBolt(dc,new Point[]{wrist+new Vector(10,0),wrist+new Vector(16,-11),palm+new Vector(18,10)},3.4,fade*charge*.8,true);
            }
        }
        private bool DrawHandCardEvent(DrawingContext dc,VisualEffect e,double t)
        {
            double soft=owner.Options.Quiet?.25:1;
            if(e.Card=="claw" && e.Kind=="claw"){DrawMechanicalClaw(dc,e,t,soft);return true;}
            if(e.Card=="ball_lightning" && e.Kind=="ball"){DrawBallLightning(dc,e,t,soft);return true;}
            if(e.Card=="ball_lightning" && e.Kind=="channel")return true;
            return false;
        }
        private void DrawMechanicalClaw(DrawingContext dc,VisualEffect e,double t,double soft)
        {
            if(t<0 || t>.76)return;
            if(!e.Launch.HasValue)e.Launch=ActionAnchor(1);
            Point wrist=e.Launch.Value;double combo=Math.Min(10,e.Strength),head=Ease(t/.17);
            for(int tine=0;tine<3;tine++)
            {
                Point a=wrist+new Vector(-10+tine*13,-12+tine*2),d=new Point(338+tine*23,292-tine*17);
                Point b=a+new Vector(55,-24+tine*4),c=d+new Vector(-1,-43);
                double fade=(1-Ease((t-.20-tine*.012)/.41))*soft;
                if(t<.21)ClawRibbon(dc,a,b,c,d,Math.Max(0,head-.66),head,(12+combo*.40),fade);
                if(t>=.12)
                {
                    double cut=Ease((t-.12)/.07);
                    ClawRibbon(dc,a,b,c,d,.34,.34+.66*cut,(10+combo*.33),fade*.90);
                }
                if(t>=.15 && t<.58)
                {
                    double age=t-.15,reach=Ease(age/.43);
                    Point chip=d+new Vector(4+reach*(5+tine*4),reach*13+age*age*45);
                    Line(dc,Color.FromRgb(255,164,75),fade*.8,2,chip,chip+new Vector(2,4+tine));
                }
            }
            double hit=t-.15;
            if(hit>=0 && hit<.26)
            {
                double fade=(1-Ease(hit/.26))*soft;Point p=new Point(347,266);
                Glow(dc,p,25,clawHeat,fade*.32);
                Stamp(dc,GameArt.Bitmap("fx-flare"),p,29,12,-38,fade*.72);
            }
        }
        private void ClawRibbon(DrawingContext dc,Point a,Point b,Point c,Point d,double tail,double head,double width,double fade)
        {
            if(head-tail<.001 || fade<=0)return;const int n=30;
            Color[] colors={clawIce,clawInk,clawHeat,Color.FromRgb(255,193,120)};
            double[] widths={1,.77,.39,.12};
            for(int layer=0;layer<4;layer++)
            {
                var polygon=new Point[n*2];
                for(int i=0;i<n;i++)
                {
                    double u=i/(n-1.0),q=tail+(head-tail)*u;Point p=Cubic(a,b,c,d,q);
                    Vector tangent=Cubic(a,b,c,d,Math.Min(1,q+.002))-Cubic(a,b,c,d,Math.Max(0,q-.002));
                    Vector normal=new Vector(-tangent.Y,tangent.X);if(normal.Length<.001)normal=new Vector(1,0);normal.Normalize();
                    double taper=Math.Pow(Math.Sin(u*Math.PI),.78),w=width*widths[layer]*taper*Math.Min(1,(head-tail)*3.8);
                    if(layer<2)w*=1+.055*Math.Sin(u*25)+.025*Math.Sin(u*43);
                    double hotOffset=layer>=2?width*.13*taper:0;
                    polygon[i]=p+normal*(w+hotOffset);polygon[polygon.Length-1-i]=p+normal*(-w+hotOffset);
                }
                dc.DrawGeometry(Brush(colors[layer],fade*(layer==3?.87:1)),null,Face(polygon));
            }
        }
        private void DrawHeldBall(DrawingContext dc,Point p,double radius,double clock,double fade)
        {
            if(radius<.5 || fade<=0)return;
            Glow(dc,p,radius*1.7,ballGold,fade*.35);
            Stamp(dc,GameArt.Bitmap("orb-lightning"),p,radius*2.10,radius*2.10,clock*20,fade);
            dc.PushClip(new EllipseGeometry(p,radius*.90,radius*.90));
            dc.PushTransform(new RotateTransform(clock*23,p.X,p.Y));
            Point[][] veins={
                new Point[]{p+new Vector(-radius*.58,-radius*.81),p+new Vector(-radius*.12,-radius*.40),p+new Vector(-radius*.27,-radius*.12),p+new Vector(radius*.11,radius*.20),p+new Vector(-radius*.06,radius*.91)},
                new Point[]{p+new Vector(-radius*.87,radius*.35),p+new Vector(-radius*.44,radius*.05),p+new Vector(-radius*.27,-radius*.12),p+new Vector(radius*.35,-radius*.50),p+new Vector(radius*.64,-radius*.80)},
                new Point[]{p+new Vector(radius*.11,radius*.20),p+new Vector(radius*.38,radius*.17),p+new Vector(radius*.50,radius*.44),p+new Vector(radius*.87,radius*.42)}};
            foreach(var vein in veins)BallFilament(dc,vein,Math.Max(2,radius*.13),fade*.96);
            dc.Pop();dc.Pop();
            if(radius>16)
            {
                Point[] branch={p+new Vector(radius*.7,-radius*.6),p+new Vector(radius+7,-radius*.7-6),p+new Vector(radius+3,-radius-9),p+new Vector(radius+15,-radius-17)};
                PaintedBolt(dc,branch,3.7,fade*.78,true);
                PaintedBolt(dc,new Point[]{p+new Vector(-radius*.7,-radius*.45),p+new Vector(-radius-8,-radius*.5),p+new Vector(-radius-4,-radius*.5-12),p+new Vector(-radius-16,-radius*.5-18)},3.2,fade*.78,true);
            }
        }
        private void BallFilament(DrawingContext dc,Point[] points,double width,double fade)
        {
            var path=new StreamGeometry();using(var g=path.Open())
            {g.BeginFigure(points[0],false,false);for(int i=1;i<points.Length;i++)g.LineTo(points[i],true,false);}path.Freeze();
            var glow=new Pen(Brush(ballGold,fade*.3),width*2.4);glow.LineJoin=PenLineJoin.Round;
            var current=new Pen(Brush(ballPale,fade),width);current.LineJoin=PenLineJoin.Round;
            dc.DrawGeometry(null,glow,path);dc.DrawGeometry(null,current,path);dc.DrawGeometry(null,new Pen(Brush(Colors.White,fade*.96),width*.48),path);
        }
        private void DrawBallLightning(DrawingContext dc,VisualEffect e,double t,double soft)
        {
            if(t<0 || t>.86)return;
            if(!e.Launch.HasValue)e.Launch=ActionAnchor(1)+new Vector(6,-27);
            Point start=e.Launch.Value,target=BallImpact;
            Point b=start+new Vector(29,-3),c=target+new Vector(-14,-23);
            double travel=1-Math.Pow(1-Math.Min(1,t/.28),2);
            if(t<.30)
            {
                double tail=Math.Max(0,travel-.8);Point p=Cubic(start,b,c,target,travel);
                BallWake(dc,start,b,c,target,tail,travel,soft*(1-Ease((t-.24)/.06)));
                DrawHeldBall(dc,p,29+travel*4,t+.72,soft);
            }
            double hit=t-.28;
            if(hit<0)return;
            double fade=(1-Ease((hit-.10)/.42))*soft;
            Glow(dc,target,37+Ease(hit/.43)*20,ballGold,fade*.48);
            if(hit<.16)
            {
                double expand=Ease(hit/.16);dc.DrawEllipse(Brush(ballPale,fade*(1-expand)*.88),null,target,27+expand*12,27+expand*12);
            }
            if(hit<.48)
            {
                Point[][] branches={
                    new Point[]{target,target+new Vector(14,-17),target+new Vector(8,-34),target+new Vector(37,-62)},
                    new Point[]{target,target+new Vector(-21,12),target+new Vector(-31,7),target+new Vector(-50,40)},
                    new Point[]{target,target+new Vector(-11,-25),target+new Vector(2,-42),target+new Vector(-16,-74)},
                    new Point[]{target,target+new Vector(-7,29),target+new Vector(12,45),target+new Vector(4,67)}};
                foreach(var branch in branches)PaintedBolt(dc,branch,8+Math.Min(3,e.Strength)*.8,fade,true);
                Stamp(dc,GameArt.Bitmap("fx-thunder-flare"),target,74,38,0,fade*(1-Ease(hit/.48))*.83);
            }
        }
        private void BallWake(DrawingContext dc,Point a,Point b,Point c,Point d,double tail,double head,double fade)
        {
            if(head-tail<.001)return;const int n=24;
            for(int layer=0;layer<3;layer++)
            {
                var shape=new Point[n*2];
                for(int i=0;i<n;i++)
                {
                    double u=i/(n-1.0),q=tail+(head-tail)*u;Point p=Cubic(a,b,c,d,q);
                    Vector v=Cubic(a,b,c,d,Math.Min(1,q+.002))-Cubic(a,b,c,d,Math.Max(0,q-.002));Vector normal=new Vector(-v.Y,v.X);
                    if(normal.Length<.001)normal=new Vector(0,1);normal.Normalize();
                    double w=(layer==0?23:layer==1?14:4)*Math.Pow(u,.8)*Math.Pow(Math.Sin(u*Math.PI),.35);
                    shape[i]=p+normal*w;shape[shape.Length-1-i]=p-normal*w;
                }
                dc.DrawGeometry(Brush(layer==0?Color.FromRgb(172,161,12):layer==1?ballGold:ballPale,fade*(layer==0?.4:layer==1?.7:.95)),null,Face(shape));
            }
        }
    }
}
