using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private Transform characterTransform=Transform.Identity;
        private Point ActionAnchor(int bone){return characterTransform.Transform(native.Anchor(bone));}
        private static double Window(double t,double enter,double leave,double end)
        {return Ease(t/enter)*(1-Ease((t-leave)/(end-leave)));}
        private static Point Around(Point center,double rx,double ry,double angle)
        {return new Point(center.X+Math.Cos(angle)*rx,center.Y+Math.Sin(angle)*ry);}
        private static void Arc(DrawingContext dc,Point center,double rx,double ry,double start,double length,Color color,double opacity,double width)
        {
            var shape=new StreamGeometry();using(var g=shape.Open())
            {
                g.BeginFigure(Around(center,rx,ry,start),false,false);
                for(int i=1;i<=14;i++)g.LineTo(Around(center,rx,ry,start+length*i/14),true,false);
            }
            shape.Freeze();dc.DrawGeometry(null,new Pen(Brush(color,opacity),width),shape);
        }
        private static void Spark(DrawingContext dc,Point p,double size,Color color,double opacity)
        {
            Line(dc,color,opacity,1,new Point(p.X-size,p.Y),new Point(p.X+size,p.Y));
            Line(dc,color,opacity,.8,new Point(p.X,p.Y-size*1.5),new Point(p.X,p.Y+size*1.5));
            Glow(dc,p,size*2.1,color,opacity*.32);
        }
        private void Lens(DrawingContext dc,Point p,double amount,double rotation,Color color,double fade)
        {
            for(int i=0;i<3;i++)
            {
                double a=rotation+i*Math.PI*2/3;
                Arc(dc,p,11+amount*9,22+amount*14,a,1.25,color,fade*(.55+i*.1),1.1);
            }
            Glow(dc,p,17+amount*8,color,fade*.22);
        }
        private void DrawCardAura(DrawingContext dc,double t)
        {
            if(InstalledModArt.Replaces(castingSkill) || IsCardArt(castingSkill))return;
            if(t<0 || t>1.85)return;
            Point core=ActionAnchor(2);double fade=Window(t,.14,1.14,1.85)*(owner.Options.Quiet?.23:1);
            Color cyan=ColorOf(OrbKind.Frost),gold=ColorOf(OrbKind.Plasma);
            if(castingSkill=="buffer")
            {
                double grow=.6+.4*Ease(t/.42);Point center=new Point(core.X,core.Y-7);
                var fill=new RadialGradientBrush(Color.FromArgb(0,80,230,255),Color.FromArgb(37,68,193,241));
                dc.PushOpacity(fade);dc.DrawEllipse(fill,new Pen(Brush(cyan,.68),1),center,67*grow,91*grow);dc.Pop();
                for(int i=0;i<6;i++)Arc(dc,center,71*grow,94*grow,i*Math.PI/3+t*.35,.34,cyan,fade*.65,1.2);
                Stamp(dc,GameArt.Bitmap("fx-power-ring"),new Point(core.X,337),100*grow,18,0,fade*.45);
            }
            else if(castingSkill=="hologram" || castingSkill=="echo")
            {
                for(int i=0;i<2;i++)
                {
                    double age=t-i*.13;if(age<0)continue;
                    double spread=Ease(age/.7)*(i==0?48:73);
                    Rect clone=new Rect(body.X-spread,body.Y-4-i*3,body.Width,body.Height);
                    DrawHologram(dc,clone,age,fade*(i==0?.35:.16));
                }
            }
            else if(castingSkill=="defragment" || castingSkill=="capacitor" || castingSkill=="machine_learning" || castingSkill=="creative_ai")
            {
                for(int i=0;i<3;i++)Arc(dc,core,47+i*10,16+i*4,t*(i%2==0?1.8:-1.6)+i*2,1.4,cyan,fade*.36,1);
            }
            else if(castingSkill=="battery" || castingSkill=="turbo" || castingSkill=="double_energy" || castingSkill=="overclock")
            {
                Stamp(dc,GameArt.Bitmap("fx-power-ring"),new Point(core.X,337),76+Ease(t/.7)*30,15,0,fade*.4);
                Glow(dc,core,47,castingSkill=="battery"?cyan:gold,fade*.12);
            }
        }
        private void DrawHologram(DrawingContext dc,Rect clone,double t,double opacity)
        {
            Color color=Color.FromRgb(68,225,247);
            dc.PushOpacity(opacity*.34);dc.DrawImage(CurrentImage,clone);dc.Pop();
            var mask=new ImageBrush(CurrentImage){Stretch=Stretch.Fill};
            // Apply the scan mask to this local silhouette, not the entire stage.
            dc.PushTransform(new TranslateTransform(clone.X,clone.Y));
            dc.PushOpacityMask(mask);
            dc.DrawRectangle(Brush(color,opacity*.38),null,new Rect(0,0,clone.Width,clone.Height));
            for(int i=0;i<18;i++)
            {
                double y=(i*13+t*54)%clone.Height;
                Line(dc,color,opacity*.62,1,new Point(0,y),new Point(clone.Width,y));
            }
            dc.Pop();dc.Pop();
        }
        private void DrawCardChoreography(DrawingContext dc,double t)
        {
            if(InstalledModArt.Replaces(castingSkill) || IsCardArt(castingSkill))return;
            if(t<0 || t>1.85)return;
            double soft=owner.Options.Quiet?.23:1,fade=Window(t,.12,1.13,1.85)*soft;
            double release=CardAnimation.For(castingSkill).Release;
            Point core=ActionAnchor(2),hand=ActionAnchor(1),head=ActionAnchor(3);
            Color cyan=ColorOf(OrbKind.Frost),gold=ColorOf(OrbKind.Plasma);
            if(castingSkill=="hyperbeam" || castingSkill=="sweeping_beam")
            {
                Point p=castingSkill=="hyperbeam"?core:hand;Color c=castingSkill=="hyperbeam"?cyan:gold;
                if(t<release+.12)
                {
                    double charge=Ease(t/release);Lens(dc,p,charge,t*6,c,fade);
                    for(int i=0;i<7;i++)
                    {
                        double phase=(t*1.6+i/7.0)%1;Point a=Around(p,22+(1-phase)*38,22+(1-phase)*38,i*2.4);
                        Point b=Around(p,15+(1-phase)*28,15+(1-phase)*28,i*2.4);
                        Line(dc,c,fade*phase*.8,1,a,b);
                    }
                    if(charge>.55)Line(dc,c,fade*(charge-.55),.6,p,new Point(405,p.Y));
                }
            }
            else if(castingSkill=="battery" || castingSkill=="turbo" || castingSkill=="double_energy" || castingSkill=="overclock")
            {
                Color c=castingSkill=="battery"?cyan:gold;
                for(int i=0;i<9;i++)
                {
                    double phase=(t*.95+i/9.0)%1;double a=i*2.399+t*.4;
                    double r=12+(1-phase)*(1-phase)*70;
                    Point p=Around(core,r,r*.83,a),tail=Around(core,r+10*(1-phase),r*.83+8*(1-phase),a);
                    Line(dc,c,fade*Math.Sin(phase*Math.PI)*.8,1.2,tail,p);
                    if(i%3==0)Spark(dc,p,1.8,c,fade*Math.Sin(phase*Math.PI));
                }
                Lens(dc,core,Ease(t/.8),-t*2,c,fade*.8);
                Arc(dc,core,42,42,-t*2.1,1.7,c,fade*.6,1.3);
                Arc(dc,core,42,42,-t*2.1+Math.PI,1.1,Colors.White,fade*.5,.8);
                Glow(dc,core,20+3*Math.Sin(t*8),c,fade*.26);
                if(t>.84 && t<1.18)Ring(dc,core,16+(t-.84)*95,c,(1-(t-.84)/.34)*soft*.75,1.5);
            }
            else if(castingSkill=="frost" || castingSkill=="chill")
            {
                for(int i=0;i<7;i++)
                {
                    double a=Math.PI*1.12+i*.38;double v=Ease(Math.Max(0,t-.05)/.65);
                    Point p=Around(new Point(head.X,head.Y+10),26+v*17,18+v*12,a);
                    Stamp(dc,GameArt.Bitmap("fx-frost"),p,4+i%2*3,10+i%2*5,i*31,fade*.73);
                }
                Arc(dc,new Point(head.X,head.Y+9),34,15,Math.PI*.93,Math.PI*1.15,cyan,fade*.6,1);
                Glow(dc,new Point(head.X,head.Y-4),20,cyan,fade*.15);
            }
            else if(castingSkill=="buffer")
            {
                Point center=new Point(core.X,core.Y-7);double grow=.6+.4*Ease(t/.42);
                for(int i=0;i<13;i++)
                {
                    double a=i*2.4+t*.15;Point p=Around(center,Math.Cos(i*.8)*50*grow,69*grow,a);
                    var hex=new StreamGeometry();using(var g=hex.Open())
                    {g.BeginFigure(Around(p,8,9,0),false,true);for(int j=1;j<6;j++)g.LineTo(Around(p,8,9,j*Math.PI/3),true,false);}
                    dc.DrawGeometry(null,new Pen(Brush(cyan,fade*(.18+.18*Math.Sin(t*3+i))),.8),hex);
                }
                Arc(dc,center,67*grow,91*grow,-1.2+t*.4,1.65,Colors.White,fade*.6,1.2);
            }
            else if(castingSkill=="defragment" || castingSkill=="capacitor" || castingSkill=="machine_learning" || castingSkill=="creative_ai")
            {
                for(int i=0;i<12;i++)
                {
                    double phase=(t*.7+i/12.0)%1;double a=i*2.4+t*.8;
                    double r=18+64*(1-phase);Point p=Around(core,r,r*.72,a);
                    double size=3+i%3;
                    dc.PushTransform(new RotateTransform(a*180/Math.PI+45,p.X,p.Y));
                    dc.DrawRectangle(Brush(cyan,fade*Math.Sin(phase*Math.PI)*.48),new Pen(Brush(cyan,fade*.65),.7),new Rect(p.X-size/2,p.Y-size/2,size,size));dc.Pop();
                }
                Arc(dc,core,29,29,t*1.8,2.4,cyan,fade*.7,1.3);
                Arc(dc,core,33,33,-t*2.2,1.6,Colors.White,fade*.5,.8);
                Spark(dc,core,5,cyan,fade*.65);
            }
            else if(castingSkill=="ice_lance" && t<release)
            {
                double v=Ease(t/release);Lens(dc,hand,v,-t*4,cyan,fade*.6);
                Line(dc,Colors.White,fade*v,1.1,new Point(hand.X-5,hand.Y),new Point(hand.X+34*v,hand.Y-4));
                for(int i=0;i<4;i++)Stamp(dc,GameArt.Bitmap("fx-frost"),Around(hand,17*(1-v)+9,17*(1-v)+9,i*Math.PI/2+t*2),4,9,i*45,fade*.7);
            }
            else if(castingSkill=="meteor_strike" && t<release+.1)
            {
                for(int i=0;i<3;i++)
                {Point p=Around(core,35,24,i*Math.PI*2/3-t*2);Spark(dc,p,3,gold,fade*.7);Line(dc,cyan,fade*.22,1,core,p);}
            }
        }
        private void Impact(DrawingContext dc,Point target,double age,Color color,double strength)
        {
            if(age<0 || age>.48)return;double fade=(1-age/.48)*strength;
            Glow(dc,target,18+age*34,color,fade*.42);
            for(int i=0;i<7;i++)
            {
                double a=i*Math.PI*2/7+.2;double r=7+age*48;
                Line(dc,i%2==0?Colors.White:color,fade*.8,i%2==0?1.4:.8,Around(target,r,r,a),Around(target,r+7*(1-age/.48),r+7*(1-age/.48),a));
            }
            Spark(dc,target,6*(1-age/.48),Colors.White,fade*.9);
        }
        private bool DrawEvokePulse(DrawingContext dc,VisualEffect e,double t,double fade)
        {
            if(e.Kind!="evoke" && e.Kind!="evoke-delay")return false;
            if(e.Card=="dualcast"){DrawArtDual(dc,e,t);return true;}
            if(t>.95)return true;
            Point hand=ActionAnchor(1),core=ActionAnchor(2);Color color=ColorOf(e.Orb);
            if(t<.15)
            {
                double u=Ease(t/.15);Point p=new Point(e.Origin.X+(hand.X-e.Origin.X)*u,e.Origin.Y+(hand.Y-e.Origin.Y)*u-Math.Sin(u*Math.PI)*14);
                Stamp(dc,GameArt.Bitmap("orb-"+GameArt.OrbName(e.Orb)),p,24-9*u,24-9*u,0,fade);
                Arc(dc,p,18-6*u,18-6*u,t*8,4.5,color,fade*.7,1);return true;
            }
            if(!e.Launch.HasValue)e.Launch=hand;
            Point source=e.Launch.Value,target=new Point(384,209);double age=t-.15;
            if(e.Orb==OrbKind.Lightning)
            {
                if(age<.20)
                {
                    Bolt(dc,source,target,age,fade*.95);Line(dc,Colors.White,fade*.65,1.1,source,target);
                    Lens(dc,source,1-age/.2,age*6,color,fade*.8);
                    Impact(dc,target,age,color,fade);
                }
            }
            else if(e.Orb==OrbKind.Frost)
            {
                FrostBurst(dc,new Point(core.X,core.Y+10),age,fade*.8,true);
                if(age<.45)Arc(dc,new Point(core.X,core.Y+2),35+age*44,50+age*28,-1.4,2.7,color,fade*(1-age/.45),1.3);
            }
            else if(e.Orb==OrbKind.Dark)
            {
                double u=Ease(age/.30);Point p=new Point(source.X+(target.X-source.X)*u,source.Y+(target.Y-source.Y)*u);
                if(age<.32)
                {
                    Glow(dc,p,25,color,fade*.3);dc.DrawEllipse(Brush(Color.FromRgb(9,10,22),fade),new Pen(Brush(color,fade),1.2),p,10,10);
                    for(int i=0;i<3;i++)Arc(dc,p,15+i*2,10+i*2,age*6+i*2,1.6,color,fade*.7,.8);
                }
                else Impact(dc,target,age-.3,color,fade*.8);
            }
            else
            {
                double u=Ease(age/.55);Lens(dc,core,1-u,-age*4,color,fade*.7);
                for(int i=0;i<3;i++)
                {Point p=new Point(core.X+(191+i*18-core.X)*u,core.Y+(355-core.Y)*u-Math.Sin(u*Math.PI)*26);Spark(dc,p,3.5*(1-u)+1,color,fade);}
            }
            return true;
        }
        private bool DrawAdvancedEvent(DrawingContext dc,VisualEffect e,double t,double fade)
        {
            if(e.Kind=="lance"){DrawInstalledLance(dc,e,t,fade);return true;}
            Point hand=ActionAnchor(1),core=ActionAnchor(2);
            Color cyan=ColorOf(OrbKind.Frost),gold=ColorOf(OrbKind.Plasma);
            if(e.Kind=="hyperbeam" || e.Kind=="beam")
            {
                bool hyper=e.Kind=="hyperbeam";Point start=hyper?core:hand;
                if(t>.68)return true;
                double pulse=Window(t,.07,.39,.68);Color c=hyper?cyan:gold;
                double endY=start.Y+(hyper?-6:Math.Sin(t*5-1.2)*22);
                Point end=new Point(405,endY);double width=(hyper?28:12)*pulse;
                if(width<.1)return true;
                var ribbon=new StreamGeometry();using(var g=ribbon.Open())
                {g.BeginFigure(new Point(start.X,start.Y-width*.28),true,true);g.LineTo(new Point(end.X,end.Y-width*.55),true,false);g.LineTo(new Point(end.X,end.Y+width*.55),true,false);g.LineTo(new Point(start.X,start.Y+width*.28),true,false);}
                var fill=new LinearGradientBrush();fill.StartPoint=new Point(0,0);fill.EndPoint=new Point(0,1);
                fill.GradientStops.Add(new GradientStop(Color.FromArgb(0,c.R,c.G,c.B),0));fill.GradientStops.Add(new GradientStop(c,.22));fill.GradientStops.Add(new GradientStop(Colors.White,.46));fill.GradientStops.Add(new GradientStop(Colors.White,.54));fill.GradientStops.Add(new GradientStop(c,.78));fill.GradientStops.Add(new GradientStop(Color.FromArgb(0,c.R,c.G,c.B),1));
                dc.PushOpacity(fade*.95);dc.DrawGeometry(fill,null,ribbon);dc.Pop();
                Line(dc,Colors.White,fade*pulse*.7,hyper?2:1,start,end);
                Lens(dc,start,pulse,t*5,c,fade*pulse);
                if(hyper)
                {
                    Arc(dc,start,27,37,t*3,2.1,c,fade*pulse*.7,1.4);
                    Arc(dc,start,31,43,-t*4,1.2,Colors.White,fade*pulse*.55,.8);
                }
                for(int i=0;i<5;i++)
                {double v=(t*2.1+i/5.0)%1;Point p=new Point(start.X+(end.X-start.X)*v,start.Y+(end.Y-start.Y)*v);Stamp(dc,GameArt.Bitmap("fx-spark"),p,3,3,0,fade*pulse*.7);}
                Impact(dc,end,t-.06,c,fade*.65);return true;
            }
            if(e.Kind=="lance")
            {
                if(t>.85)return true;
                if(!e.Launch.HasValue)e.Launch=hand;
                Point start=e.Launch.Value;double travel=Ease(t/.32);
                Point tip=new Point(start.X+16+(402-start.X)*travel,start.Y-5-9*travel);
                if(t<.36)
                {
                    Vector direction=tip-start;if(direction.Length<1)direction=new Vector(1,0);direction.Normalize();
                    Vector side=new Vector(-direction.Y,direction.X);Point tail=tip-direction*(24+14*travel);
                    var shape=new StreamGeometry();using(var g=shape.Open())
                    {g.BeginFigure(tail-side*4,true,true);g.LineTo(tip-direction*12-side*6,true,false);g.LineTo(tip+direction*13,true,false);g.LineTo(tip-direction*12+side*6,true,false);g.LineTo(tail+side*4,true,false);}
                    dc.DrawGeometry(new LinearGradientBrush(Color.FromRgb(62,144,211),Colors.White,0),new Pen(Brush(cyan,fade),.8),shape);
                    Line(dc,cyan,fade*.35,3,tip-direction*64,tip-direction*14);Line(dc,Colors.White,fade*.9,1,tail,tip+direction*11);
                    for(int i=0;i<4;i++)Stamp(dc,GameArt.Bitmap("fx-frost"),tip-direction*(14+i*12)+side*Math.Sin(t*8+i)*5,3+i%2*2,7,i*32,fade*.8);
                }
                Impact(dc,new Point(399,start.Y-14),t-.31,cyan,fade*.85);return true;
            }
            if(e.Kind=="meteor")
            {
                Point target=new Point(344,315);double travel=Ease(t/.43);
                Point p=new Point(target.X-78*(1-travel),83+(target.Y-83)*travel);
                if(t<.43)
                {
                    Vector direction=new Vector(78,target.Y-83);direction.Normalize();Vector side=new Vector(-direction.Y,direction.X);
                    var trail=new StreamGeometry();using(var g=trail.Open())
                    {g.BeginFigure(p-direction*110-side*2,true,true);g.LineTo(p-direction*8-side*17,true,false);g.LineTo(p+direction*14,true,false);g.LineTo(p-direction*8+side*17,true,false);}
                    dc.DrawGeometry(new LinearGradientBrush(Color.FromArgb(0,53,169,230),Color.FromRgb(197,255,255),new Point(0,0),new Point(1,1)),new Pen(Brush(gold,fade*.65),1.1),trail);
                    Line(dc,Colors.White,fade*.95,4,p-direction*56,p+direction*2);
                    Glow(dc,p,21,cyan,fade*.35);Spark(dc,p,7,Colors.White,fade*.8);
                    for(int i=0;i<3;i++)Stamp(dc,GameArt.EffectFrame("rock",4,1,i),p-direction*(24+i*16)+side*(i%2==0?1:-1)*(11+i*2),5+i,5+i,t*120+i*20,fade*.8);
                }
                else
                {
                    double age=t-.43;Impact(dc,new Point(target.X,target.Y-8),age,cyan,fade);
                    dc.DrawEllipse(null,new Pen(Brush(gold,fade*Math.Max(0,1-age/.6)*.7),1.5),target,12+age*77,3+age*17);
                    Stamp(dc,GameArt.EffectFrame("smoke",6,1,Math.Min(5,(int)(age/.8*6))),new Point(target.X,target.Y-7-age*14),58+age*20,34,0,fade*.38);
                    for(int i=0;i<6;i++)
                    {double dx=(i-2.5)*37;Point q=new Point(target.X+dx*age,target.Y-(45+i%3*23)*age+105*age*age);Stamp(dc,GameArt.EffectFrame("debris",4,1,i%4),q,5+i%3*2,5+i%3*2,i*32+age*140,fade*.8);}
                }
                return true;
            }
            return false;
        }
    }
}
