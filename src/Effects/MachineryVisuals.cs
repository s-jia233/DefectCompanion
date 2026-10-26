using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private Point HeatCore {get{return characterTransform.Transform(HotCoreArt.Center(native.ClipName,native.Frame));}}
        private void DrawHotCoreMaterial(DrawingContext dc,double now)
        {
            if(!owner.Engine.IsOverclocked || !HotCoreArt.Visible(native.ClipName,native.Frame))return;
            double remaining=owner.Engine.OverclockUntil-owner.Engine.Now,age=5-remaining;
            double material=Ease(age/.30)*Ease(remaining/.65),soft=owner.Options.Quiet?.24:1;
            double pulse=.055+.055*(.5+.5*Math.Sin(age*4.6));Point core=HeatCore;
            Glow(dc,core,27,Color.FromRgb(235,39,20),material*soft*.18);
            dc.PushTransform(characterTransform);
            dc.PushOpacity(material);dc.DrawImage(HotCoreArt.Frame(native.ClipName,native.Frame),HotCoreArt.Bounds(native.ClipName));dc.Pop();
            dc.PushOpacity(material*pulse);dc.DrawImage(HotCoreArt.Frame(native.ClipName,native.Frame,true),HotCoreArt.Bounds(native.ClipName));dc.Pop();
            dc.Pop();
            Glow(dc,core,10,Color.FromRgb(255,185,35),material*pulse*soft*.4);
        }
        private static readonly Color spearDark=Color.FromRgb(37,78,104),spearBlue=Color.FromRgb(67,149,174),spearIce=Color.FromRgb(139,229,237),spearWhite=Color.FromRgb(213,252,255),heatRed=Color.FromRgb(188,48,18),heatOrange=Color.FromRgb(255,130,16),heatPale=Color.FromRgb(255,227,154);
        private void DrawMachineryRear(DrawingContext dc,double t,double now)
        {
            double soft=owner.Options.Quiet?.24:1;
            if(castingSkill=="overclock" && t>=0 && t<2.10)DrawPressureMeter(dc,t,soft);
            if(owner.Engine.IsOverclocked)
            {
                double remaining=owner.Engine.OverclockUntil-owner.Engine.Now,age=5-remaining;
                Point head=ActionAnchor(3);double fade=Ease(age/.35)*Ease(remaining/.48)*soft;
                for(int side=-1;side<=1;side+=2)
                {
                    double clock=(age+(side<0?.0:.32))%1.12;
                    Steam(dc,head+new Vector(side*27,15),new Vector(side*.8,-.7),clock,11,fade*.96);
                }
            }
        }
        private void DrawMachineryFront(DrawingContext dc,double t,double now)
        {
            double soft=owner.Options.Quiet?.24:1;
            if(castingSkill=="ice_lance" && t>=0 && t<2.30 && native.ClipName=="art-ice-spear")DrawHeldIceSpear(dc,t,soft);
            if(owner.Engine.IsOverclocked)
            {
                double remaining=owner.Engine.OverclockUntil-owner.Engine.Now,age=5-remaining;
                double fade=Ease(age/.42)*Ease(remaining/.48)*soft;Point core=HeatCore;
                if(castingSkill=="overclock" && t<1.45)
                {
                    double pulse=.65+.35*Math.Sin(age*8);
                    PaintedBolt(dc,new Point[]{ActionAnchor(0),core+new Vector(-23,14),core},4.5,fade*pulse*.72,true);
                    PaintedBolt(dc,new Point[]{ActionAnchor(1),core+new Vector(23,-5),core},4.5,fade*pulse*.72,true);
                }
            }
        }
        private void DrawPressureMeter(DrawingContext dc,double t,double soft)
        {
            Point c=ActionAnchor(2)+new Vector(-14,-109);double fade=Window(t,.12,1.24,2.10)*soft,grow=.90*(.65+.35*Ease(t/.42));
            dc.PushTransform(new ScaleTransform(grow,grow,c.X,c.Y));
            Arc(dc,c,99,94,-3.08,3.12,Color.FromRgb(39,23,33),fade*.96,22);
            Arc(dc,c,103,98,-3.08,3.12,Color.FromRgb(153,92,33),fade*.83,3);
            for(int i=0;i<12;i++)
            {
                double a=-3.02+i*.255;Color color=i<6?Color.FromRgb(235,167,30):i<9?heatOrange:heatRed;
                Arc(dc,c,96,91,a,.207,color,fade,10);
                Line(dc,heatPale,fade*.54,1.2,Around(c,83,78,a),Around(c,89,84,a));
            }
            double angle=-2.88+2.53*Ease(t/.58)+.025*Math.Sin(t*17)*Ease(t/.5);
            Point tip=Around(c,93,88,angle);Vector d=tip-c,n=new Vector(-d.Y,d.X);n.Normalize();
            dc.DrawGeometry(Brush(Color.FromRgb(41,18,16),fade),null,Face(c-n*8,c+d*.12-n*10,tip+new Vector(2,2),c+d*.12+n*10));
            dc.DrawGeometry(Brush(heatPale,fade),null,Face(c-n*5,c+d*.16-n*6,tip,c+d*.16+n*6));
            dc.DrawEllipse(Brush(heatRed,fade),new Pen(Brush(heatOrange,fade),2),c,10,10);
            for(int i=0;i<6;i++)
            {
                double a=-2.95+i*.53;Point p=Around(c,108,102,a);
                p.X=Math.Max(124,p.X);
                double size=(.75+.25*Math.Sin(t*6+i))*Ease(t/.38);
                HeatTongue(dc,p,size,a+Math.PI/2,t+i,fade);
            }
            dc.Pop();
        }
        private void HeatTongue(DrawingContext dc,Point p,double size,double angle,double clock,double fade)
        {
            dc.PushTransform(new RotateTransform(angle*180/Math.PI,p.X,p.Y));
            dc.PushTransform(new ScaleTransform(size,size,p.X,p.Y));
            double sway=Math.Sin(clock*3)*3;
            var shape=new StreamGeometry();using(var g=shape.Open())
            {
                g.BeginFigure(p+new Vector(-9,14),true,true);
                g.BezierTo(p+new Vector(-21,-5),p+new Vector(-2,-8),p+new Vector(-7+sway,-34),true,false);
                g.BezierTo(p+new Vector(5,-28),p+new Vector(10,-16),p+new Vector(4,-8),true,false);
                g.BezierTo(p+new Vector(13,-9),p+new Vector(13,-24),p+new Vector(17,-18),true,false);
                g.BezierTo(p+new Vector(21,-1),p+new Vector(9,2),p+new Vector(7,14),true,false);
            }shape.Freeze();
            dc.DrawGeometry(Brush(heatRed,fade*.9),null,shape);
            dc.PushTransform(new ScaleTransform(.68,.74,p.X,p.Y));dc.DrawGeometry(Brush(heatOrange,fade*.95),null,shape);
            dc.PushTransform(new ScaleTransform(.58,.64,p.X,p.Y));dc.DrawGeometry(Brush(Color.FromRgb(255,190,48),fade*.85),null,shape);dc.Pop();dc.Pop();
            dc.Pop();dc.Pop();
        }
        private void DrawHeldIceSpear(DrawingContext dc,double t,double soft)
        {
            Point rear=ActionAnchor(0),front=ActionAnchor(1);Vector dir=front-rear;if(dir.Length<1)dir=new Vector(1,-.3);dir.Normalize();
            Vector normal=new Vector(-dir.Y,dir.X);double form=Ease((t-.10)/.47),melt=1-Ease((t-1.65)/.43),alpha=form*melt;
            Point butt=rear-dir*34,baseBlade=front+dir*60;
            const double reach=114;
            Point tip=front+dir*reach;Point middle=front+dir*81;
            if(alpha>.001)
            {
                // The shaft reaches its socket before the head appears, and
                // ends inside the socket rather than poking through the point.
                double shaftForm=Ease((t-.08)/.28);
                Point end=butt+(baseBlade+dir*5-butt)*shaftForm;
                dc.DrawGeometry(Brush(spearDark,alpha),null,Face(butt-normal*4,end-normal*4,end+normal*4,butt+normal*4));
                dc.DrawGeometry(Brush(spearBlue,alpha),null,Face(butt-normal*3,end-normal*3,end+normal, butt+normal));
                Line(dc,spearWhite,alpha*.85,1.3,butt-normal*2,end-normal*2);
                for(int i=0;i<5;i++)
                {Point p=butt+dir*(22+i*24);Line(dc,spearIce,alpha*.7,1,p-normal*3,p+dir*7+normal*3);}
                double blade=alpha*Ease((t-.36)/.20)*(1-Ease((t-1.23)/.38));
                if(blade>.001)
                {
                    dc.DrawGeometry(Brush(spearDark,blade),new Pen(Brush(spearWhite,blade*.8),1.2),Face(baseBlade-normal*9,middle-normal*14,tip,middle+normal*15,baseBlade+normal*7));
                    dc.DrawGeometry(Brush(spearBlue,blade),null,Face(baseBlade-normal*9,middle-normal*14,tip,middle+normal*2,baseBlade));
                    dc.DrawGeometry(Brush(spearIce,blade),null,Face(baseBlade,middle-normal*3,tip,middle+normal*15,baseBlade+normal*7));
                    dc.DrawGeometry(Brush(spearWhite,blade*.9),null,Face(middle-normal*3,tip,middle+normal*2,baseBlade+dir*7));
                    Line(dc,spearWhite,blade*.85,1.4,baseBlade+dir*4-normal*4,middle-normal*9);
                    dc.DrawGeometry(Brush(spearBlue,blade),null,Face(baseBlade-dir*5-normal*5,baseBlade+dir*5-normal*7,baseBlade+dir*5+normal*7,baseBlade-dir*5+normal*5));
                    Line(dc,spearIce,blade*.8,1,baseBlade-dir*4-normal*3,baseBlade+dir*4-normal*4);
                }
                double wind=Window(t,.25,1.18,1.62)*soft;
                if(t>.72 && t<1.65)
                {
                    Point c=front+dir*64;
                    for(int lane=0;lane<3;lane++)
                    {
                        double phase=t*6+lane*2.094;
                        var band=new Point[38];
                        for(int i=0;i<19;i++)
                        {
                            double u=i/18.0,a=phase+u*2.4,width=Math.Sin(u*Math.PI)*(3+lane);
                            Point p=c+dir*((u-.5)*58)+normal*(Math.Cos(a)*(29+lane*7));
                            band[i]=p+normal*width;band[band.Length-1-i]=p-normal*width;
                        }
                        dc.DrawGeometry(Brush(lane==1?spearWhite:spearIce,wind*(lane==1?.78:.48)),null,Face(band));
                    }
                }
            }
            // Original fists cover the ice shaft at both grip points.
            var fist=GameArt.Bitmap("cast-fist");double angle=Math.Atan2(dir.Y,dir.X)*180/Math.PI;
            Stamp(dc,fist,rear+new Vector(1,0),22,24,angle+18,1);
            Stamp(dc,fist,front+new Vector(3,-1),24,25,angle+10,1);
        }
        private bool DrawMachineryEvent(DrawingContext dc,VisualEffect e,double t)
        {
            double soft=owner.Options.Quiet?.24:1;
            if(e.Card=="overclock" && e.Kind=="overclock")return true;
            if(e.Card=="overclock" && e.Kind=="cooling")
            {
                Point head=ActionAnchor(3);
                Steam(dc,head+new Vector(-25,14),new Vector(-.8,-.7),t,10,soft);
                Steam(dc,head+new Vector(27,13),new Vector(.8,-.7),t,10,soft);return true;
            }
            if(e.Card=="ice_lance" && e.Kind=="channel")return true;
            if(e.Card!="ice_lance" || e.Kind!="lance")return false;
            if(t<0 || t>1.15)return true;
            if(!e.Launch.HasValue || t<.19)
            {Vector dir=ActionAnchor(1)-ActionAnchor(0);if(dir.Length<1)dir=new Vector(1,-.3);dir.Normalize();Point hand=ActionAnchor(1);e.Launch=hand+dir*114;}
            double hit=t-.19;if(hit<0)return true;Point target=e.Launch.Value;
            double fade=(1-Ease((hit-.12)/.65))*soft;
            Glow(dc,target,30,spearIce,fade*.3);
            if(hit<.24)Stamp(dc,GameArt.Bitmap("fx-flare"),target,49,30,-17,fade*(1-hit/.24));
            for(int i=0;i<12;i++)
            {
                double age=hit-i*.009;if(age<.04 || age>.76)continue;
                double a=i*2.399,reach=(1-Math.Exp(-age/.16))*(19+i%4*10);
                Point p=target+new Vector(Math.Cos(a)*reach-18*age,Math.Sin(a)*reach*.64+50*age*age);
                double size=(3+i%3*2)*Ease((age-.04)/.055)*(1-Ease((age-.23)/.53));
                dc.PushTransform(new RotateTransform(i*29+age*110,p.X,p.Y));
                dc.DrawGeometry(Brush(i%3==0?spearWhite:i%3==1?spearIce:spearBlue,fade),null,Face(p+new Vector(-size,size*.4),p+new Vector(size*.9,-size*.8),p+new Vector(size*.45,size*.7)));dc.Pop();
            }
            return true;
        }
    }
}
