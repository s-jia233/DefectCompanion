using System;
using System.Windows;
using System.Windows.Media;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private static double Ease(double value) { value=Math.Max(0,Math.Min(1,value));return value*value*(3-2*value); }
        private void DrawAbilities(DrawingContext dc,double now)
        {
            double soft=owner.Options.Quiet ? .2 : 1;
            Point core=ActionAnchor(2);
            if(owner.Engine.ShieldHits>0 && !mod.BufferActive && now-mod.BufferBreakAt>1.2 && (castingSkill!="buffer" || now-cardStart>1.85))
                Arc(dc,new Point(core.X,core.Y-6),63,88,-1.17,2.5,ColorOf(OrbKind.Frost),.16*soft,.9);
            if(owner.Engine.Loop && owner.Engine.Orbs.Count>0)
            {
                Point p=OrbPosition(0);
                Stamp(dc,GameArt.Bitmap("fx-power-ring"),p,49,49,now*30,.2*soft);
            }
            if(owner.Engine.Capacity>3)
                for(int i=owner.Engine.Orbs.Count;i<owner.Engine.Capacity;i++) Ring(dc,OrbPosition(i),21,ColorOf(OrbKind.Frost),.13*soft,1);
        }
        private void FrostBurst(DrawingContext dc,Point center,double t,double fade,bool shield)
        {
            if(shield && t<.8)
            {
                double pop=Ease(t/.15);
                Stamp(dc,GameArt.Bitmap("fx-block"),new Point(264,259),43*pop,50*pop,-8,fade*.65);
            }
            for(int i=0;i<5;i++)
            {
                double a=i*Math.PI*2/5+.3;double r=19+Ease(t/.6)*31;
                Point p=new Point(center.X+Math.Cos(a)*r,center.Y+Math.Sin(a)*r*.7);
                Stamp(dc,GameArt.Bitmap("fx-frost"),p,7+i%3*2,10+i%3*3,i*31,fade*.72);
            }
            Glow(dc,center,31,ColorOf(OrbKind.Frost),fade*.1);
        }
        private void DrawCardEffect(DrawingContext dc,VisualEffect e,double t,double fade)
        {
            if(DrawAdvancedEvent(dc,e,t,fade))return;
            Point hand=ActionAnchor(1),core=ActionAnchor(2);string kind=e.Kind;
            Color cyan=ColorOf(OrbKind.Frost),gold=ColorOf(OrbKind.Plasma);
            double u=Ease((t-.1)/.43);
            if(kind=="claw" || kind=="ice-slash")
            {
                if(t>.5)return;
                for(int i=0;i<3;i++)
                {
                    double age=t-i*.055;if(age<0 || age>.28)continue;
                    int f=Math.Min(3,(int)(age/.28*4));
                    Stamp(dc,GameArt.EffectFrame("slash",4,1,f),new Point(hand.X+13+i*8,hand.Y-8+i*8),54+Math.Min(8,e.Strength)*2,13,58,fade);
                }
                if(kind=="ice-slash") FrostBurst(dc,new Point(322,236),t,fade*.65,false);
            }
            else if(kind=="ball" || kind=="barrage")
            {
                if(t>.75)return;
                if(!e.Launch.HasValue)e.Launch=hand;
                Point origin=e.Launch.Value;
                Point target=new Point(383,202+(kind=="barrage"?Math.Sin(e.Start*19)*22:0));
                Point p=new Point(origin.X+(target.X-origin.X)*u,origin.Y+(target.Y-origin.Y)*u-(kind=="ball"?Math.Sin(u*Math.PI)*17:0));
                if(u<1)
                {
                    Stamp(dc,GameArt.Bitmap("orb-"+GameArt.OrbName(e.Orb)),p,kind=="ball"?25:13,kind=="ball"?25:13,t*30,fade);
                    if(e.Orb==OrbKind.Lightning) Bolt(dc,new Point(p.X-29,p.Y+5),p,t,fade*.55);
                    else Stamp(dc,GameArt.Bitmap("fx-"+GameArt.OrbName(e.Orb)),new Point(p.X-16,p.Y+4),8,16,75,fade*.5);
                }
                else if(e.Orb==OrbKind.Lightning) Stamp(dc,GameArt.Sequence(1,Math.Min(8,(int)((t-.53)/.22*9))),target,42,42,0,fade*.7);
                else Stamp(dc,GameArt.Bitmap("fx-"+GameArt.OrbName(e.Orb)),target,18,24,0,fade*.7);
            }
            else if(kind=="thunder" || kind=="storm" || kind=="coil")
            {
                Point p=kind=="storm"?new Point(329,171):new Point(349,230);
                if(t<.42)
                {
                    Bolt(dc,new Point(p.X-12,103),p,t,fade*.9);
                    Stamp(dc,GameArt.Sequence(2,Math.Min(8,(int)(t/.42*9))),p,50,50,0,fade*.75);
                }
            }
            else if(kind=="passive")
            {
                if(e.Orb==OrbKind.Lightning && t<.35) Bolt(dc,e.Origin,new Point(371,217),t,fade*.75);
                else if(e.Orb==OrbKind.Frost) FrostBurst(dc,new Point(245,260),t,fade*.55,true);
                else Stamp(dc,GameArt.Bitmap("fx-"+GameArt.OrbName(e.Orb)),e.Origin,13,24,t*90,fade*.5);
            }
            else if(kind=="glacier" || kind=="shield")
            {
                if(kind=="glacier" || !mod.BufferActive && owner.Seconds-mod.BufferBreakAt>1.2 && castingSkill!="buffer")FrostBurst(dc,new Point(235,265),t,fade,true);
            }
            else if(kind=="snow" || kind=="cooling")
            {
                for(int i=0;i<6;i++)
                {double age=(t+i*.13)%1.2;Point p=new Point(163+i*17+Math.Sin(age*2+i)*5,244+(kind=="snow"?age*35:-age*29));Stamp(dc,GameArt.Bitmap(kind=="snow"?"fx-frost":"fx-spark"),p,3+i%2*2,5+i%2*3,i*27,fade*.45);}
            }
            else if(kind=="dash")
            {
                if(t>.5)return;
                for(int i=0;i<3;i++)
                {Rect copy=new Rect(body.X-10-i*12+u*18,body.Y,body.Width,body.Height);dc.PushOpacity(fade*(.22-i*.06));dc.DrawImage(CurrentImage,copy);dc.Pop();}
                for(int i=0;i<3;i++) Line(dc,cyan,fade*.4,1,new Point(143,207+i*28),new Point(179+u*12,207+i*28));
            }
            else if(kind=="capacity" || kind=="loop" || kind=="focus" || kind=="learn" || kind=="ai" || kind=="echo-ready")
            {
                if(kind=="focus")return;
                if(castingSkill!="defragment" && castingSkill!="capacitor" && castingSkill!="machine_learning" && castingSkill!="creative_ai")
                {
                    Stamp(dc,GameArt.Bitmap("fx-power-ring"),new Point(210,337),78+u*34,14+u*4,0,fade*.55);
                    Stamp(dc,GameArt.Bitmap("fx-power-beam"),new Point(210,288),67,107,0,fade*.32);
                }
                string key=kind=="capacity"?"capacity":kind=="loop"?"loop":kind=="focus"?"focus":kind=="learn"?"machine_learning":kind=="ai"?"creative_ai":"echo_form";
                var powers=owner.Engine.PowerStatuses();int index=powers.FindIndex(a=>a.Key==key);
                if(index>=0 && t<.65)
                {Rect slot=PowerRect(index,powers.Count);double v=Ease(t/.65);Point p=new Point(core.X+(slot.X+13-core.X)*v,core.Y+(slot.Y+13-core.Y)*v);Stamp(dc,GameArt.Bitmap("power-"+key),p,20,20,0,fade*.85);}
            }
            else if(kind=="energy" || kind=="overclock")
            {
                if(castingSkill=="overclock" || castingSkill=="double_energy")return;
                Glow(dc,core,26,gold,fade*.19);
                for(int i=0;i<5;i++){double a=i*Math.PI*2/5+t;double r=27*(1-u);Stamp(dc,GameArt.Bitmap("fx-plasma"),new Point(core.X+Math.Cos(a)*r,core.Y+Math.Sin(a)*r),5,9,i*30,fade*.7);}
            }
            else if(kind=="leap")
                Stamp(dc,GameArt.Bitmap("fx-power-ring"),new Point(210,339),45+t*45,8+t*6,0,fade*.4);
        }
    }
}
