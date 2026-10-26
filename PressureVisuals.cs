using System;
using System.Windows;
using System.Windows.Media;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private void Steam(DrawingContext dc,Point origin,Vector direction,double clock,int count,double strength)
        {
            if(clock<0 || clock>.85)return;
            direction.Normalize();Vector normal=new Vector(-direction.Y,direction.X);
            for(int i=0;i<count;i++)
            {
                double age=clock-i*.024;if(age<0 || age>.62)continue;
                double u=age/.62,side=(i%2==0?1:-1),fade=(1-u)*(1-u)*strength;
                Point p=origin+direction*(7+u*(52+i%3*10))+normal*(side*(5+u*19)+Math.Sin(i*2.2)*4);
                double radius=3+u*(10+i%3*3);
                Glow(dc,p,radius,Color.FromRgb(231,245,240),fade*.52);
                dc.DrawEllipse(Brush(Color.FromRgb(213,231,229),fade*.14),null,p,radius*.63,radius*.42);
            }
        }
        private void DrawPressureRear(DrawingContext dc,double t,double now)
        {
            double soft=owner.Options.Quiet?.24:1;
            foreach(var e in effects)
                if(e.Kind=="rocket" && now>=e.Start && now-e.Start<.78)
                    Steam(dc,e.Launch ?? ActionAnchor(1),new Vector(-1,.04),now-e.Start,14,soft);
            if(t<0 || t>CardAnimation.For(castingSkill).Duration)return;
            if(castingSkill=="thunder")DrawThunderPortraitRear(dc,t,soft);
            if(castingSkill=="storm")DrawStormCloud(dc,t,soft);
            if(castingSkill=="turbo")
            {
                double fade=Window(t,.02,.46,1.22)*soft;
                for(int i=2;i>=1;i--)
                {
                    var sample=native.Past(now-i*.075);Rect rect=characterTransform.TransformBounds(sample.Bounds);rect.Offset(-i*16,i*2);
                    var energy=Energy(sample.Image);dc.PushOpacity(fade*(i==1?.22:.11));dc.DrawImage(sample.Image,rect);dc.DrawImage(energy.Edge,rect);dc.Pop();
                }
                Steam(dc,new Point(155,308),new Vector(-1,-.13),Math.Max(0,t-.08),17,soft*.9);
                for(int i=0;i<5;i++)
                {double y=181+i*24;Line(dc,artCyan,fade*(.5-i*.05),i%2==0?2:1,new Point(125-i*6,y),new Point(188-i*3,y-3));}
            }
        }
        private void DrawPressureFront(DrawingContext dc,double t)
        {
            if(t<0 || t>=CardAnimation.For(castingSkill).Duration)return;
            double soft=owner.Options.Quiet?.24:1;var fist=GameArt.Bitmap("cast-fist");
            if(castingSkill=="rocket" && native.ClipName=="art-rocket")
            {
                Point wrist=ActionAnchor(1);
                if(t<.22 || t>=1.05)Stamp(dc,fist,new Point(wrist.X+7,wrist.Y-1),29,29,-8,1);
                else
                {
                    dc.DrawEllipse(Brush(Color.FromRgb(24,37,40),1),new Pen(Brush(Color.FromRgb(143,116,63),1),1),wrist,3.4,4.2);
                    if(t<.48)
                    {
                        double u=(t-.22)/.26;Arc(dc,wrist,4+u*13,10+u*18,0,Math.PI*2,Color.FromRgb(231,245,240),(1-u)*soft,1.2);
                    }
                }
                if(t<.22 && t>.08)Glow(dc,wrist,11,Color.FromRgb(241,225,177),(t-.08)*.9*soft);
            }
            if((castingSkill=="thunder" || castingSkill=="turbo") && native.ClipName=="art-"+castingSkill)
            {
                Point a=ActionAnchor(0),b=ActionAnchor(1);
                Stamp(dc,fist,new Point(a.X-3,a.Y+1),24,24,10,1);Stamp(dc,fist,new Point(b.X+5,b.Y-2),26,26,-10,1);
            }
            if(castingSkill=="thunder")DrawThunderPortraitFront(dc,t,soft);
            if(castingSkill=="storm")DrawStormFront(dc,t,soft);
            if(castingSkill=="turbo")
            {
                double fade=Window(t,.02,.48,1.12)*soft;
                dc.PushTransform(characterTransform);dc.PushOpacity(fade*.75);dc.DrawImage(Energy(native.Image).Edge,native.Bounds);dc.Pop();dc.Pop();
                Point head=ActionAnchor(3);Line(dc,artCyan,fade*.65,1.7,new Point(head.X-38,head.Y-5),new Point(head.X+26,head.Y-8));
            }
        }
        private bool DrawPressureEvent(DrawingContext dc,VisualEffect e,double t)
        {if(e.Kind=="thunder"){DrawThunderProc(dc,e,t);return true;}return e.Kind=="thunder-ready" || e.Kind=="storm" && e.Card=="storm" || e.Kind=="charge" && e.Card=="turbo";}
    }
}
