using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private static readonly Color circuitInk=Color.FromRgb(4,33,43),circuitTeal=Color.FromRgb(20,168,169),circuitPale=Color.FromRgb(202,255,235);
        private double learningSince=-100;
        private readonly List<double> learningPulses=new List<double>();
        private sealed class FusionResolution {public double Started;public Orb Orb;}
        private readonly List<FusionResolution> fusions=new List<FusionResolution>();
        private static Point FusionCore {get{return new Point(306,213);}}
        private void KnowledgeCard(string skill,double now)
        {if(skill=="machine_learning")learningSince=now;}
        private void KnowledgeEvent(PetEvent e,double now)
        {
            if(e.Kind=="reboot"){learningSince=-100;learningPulses.Clear();fusions.Clear();return;}
            if(e.Kind=="learn" && e.Card!="machine_learning")learningPulses.Add(now+e.Delay);
            if(e.Kind=="channel" && e.Card=="plasma" && e.Created!=null)fusions.Add(new FusionResolution{Started=now+e.Delay,Orb=e.Created});
        }
        private double LearningBloom(double now)
        {
            double bloom=Window(now-learningSince,.28,1.72,2.65);
            foreach(double at in learningPulses){double t=now-at;if(t>=0)bloom=Math.Max(bloom,Window(t,.15,.50,1.35));}
            return owner.Engine.Learning?bloom:0;
        }
        private void DrawLearningHead(DrawingContext dc,double now)
        {
            if(!owner.Engine.Learning)return;
            double fade=Ease((now-learningSince)/.34),pulse=LearningBloom(now),soft=owner.Options.Quiet?.25:1;
            var bounds=InstalledModArt.ColdHeadClips[native.ClipName].Bounds;
            dc.PushTransform(characterTransform);
            if(native.ClipName=="art-learning")
            {
                dc.PushOpacity(fade);dc.DrawImage(LearningNeckArt.Frame(native.Frame),LearningNeckArt.Bounds);dc.Pop();
                dc.PushOpacity(fade*(.42+.5*pulse)*soft);dc.DrawImage(LearningNeckArt.Frame(native.Frame,true),LearningNeckArt.Bounds);dc.Pop();
            }
            dc.PushOpacity(fade);dc.DrawImage(LearningHeadArt.Frame(native.ClipName,native.Frame),bounds);dc.Pop();
            dc.PushOpacity(fade*(.57+.40*pulse+.025*Math.Sin(now*2.8))*soft);
            dc.DrawImage(LearningHeadArt.Frame(native.ClipName,native.Frame,true),bounds);dc.Pop();dc.Pop();
        }
        private void DrawKnowledgeRear(DrawingContext dc,double t,double now)
        {
            learningPulses.RemoveAll(at=>now-at>2.8);
            fusions.RemoveAll(f=>now-f.Started>2.5 || !owner.Engine.Orbs.Contains(f.Orb));
            double soft=owner.Options.Quiet?.22:1;
            foreach(var fusion in fusions)
            {
                double age=now-fusion.Started;if(age<0 || age>1.70)continue;
                double fade=Window(age,.22,1.20,1.70)*soft;
                Glow(dc,FusionCore,49,Color.FromRgb(88,218,187),fade*.17);
                for(int i=0;i<3;i++)Arc(dc,FusionCore,39+i*7,29+i*8,age*(i==1?-1.1:1.0)+i*2.1,1.15,Color.FromRgb(38,52,104),fade*.7,6-i);
            }
        }
        private static Point KnowledgeCurve(Point a,Point b,Point c,Point d,double u)
        {double v=1-u;return new Point(v*v*v*a.X+3*v*v*u*b.X+3*v*u*u*c.X+u*u*u*d.X,v*v*v*a.Y+3*v*v*u*b.Y+3*v*u*u*c.Y+u*u*u*d.Y);}
        private static Vector KnowledgeNormal(Point a,Point b,Point c,Point d,double u)
        {Vector v=KnowledgeCurve(a,b,c,d,Math.Min(1,u+.005))-KnowledgeCurve(a,b,c,d,Math.Max(0,u-.005));if(v.Length<.001)v=new Vector(1,0);v.Normalize();return new Vector(-v.Y,v.X);}
        private void DrawKnowledgeFront(DrawingContext dc,double t,double now)
        {
            double soft=owner.Options.Quiet?.24:1;
            foreach(var fusion in fusions)
            {
                double age=now-fusion.Started;if(age<0 || age>1.72)continue;
                DrawFusion(dc,age,soft);
            }
        }
        private void FusionRibbon(DrawingContext dc,Point a,Point b,Point c,Point d,double age,int lane,double fade)
        {
            double travel=Ease((age-.10)/.88),start=Math.Max(0,travel-.67),end=travel;
            if(end<=start || fade<=0)return;
            Color color=lane==0?Color.FromRgb(100,222,244):lane==1?Color.FromRgb(247,222,30):Color.FromRgb(166,66,205);
            Color pale=lane==0?Color.FromRgb(208,255,248):lane==1?Color.FromRgb(255,249,192):Color.FromRgb(237,161,239);
            var left=new List<Point>();var right=new List<Point>();int count=22;
            for(int i=0;i<=count;i++)
            {
                double u=start+(end-start)*i/count;Point p=KnowledgeCurve(a,b,c,d,u);Vector n=KnowledgeNormal(a,b,c,d,u);
                double width=(lane==2?13:11)*(Math.Sin(i*Math.PI/count)*.6+.4)*(1-.40*u);
                if(lane==0)width*=i%3==0?1.45:.78;
                if(lane==1)p+=n*(i%2==0?2.8:-2.8)*(1-u);
                left.Add(p+n*width);right.Add(p-n*width);
            }
            var shape=new StreamGeometry();using(var g=shape.Open())
            {g.BeginFigure(left[0],true,true);for(int i=1;i<left.Count;i++)g.LineTo(left[i],true,false);for(int i=right.Count-1;i>=0;i--)g.LineTo(right[i],true,false);}shape.Freeze();
            dc.DrawGeometry(Brush(color,fade),new Pen(Brush(circuitInk,fade*.92),3.1),shape);
            var path=new StreamGeometry();using(var g=path.Open())
            {g.BeginFigure(KnowledgeCurve(a,b,c,d,start),false,false);for(int i=1;i<=18;i++)g.LineTo(KnowledgeCurve(a,b,c,d,start+(end-start)*i/18),true,false);}path.Freeze();
            dc.DrawGeometry(null,new Pen(Brush(pale,fade*.93),lane==2?2.8:3.6),path);
            if(lane==0)
            {
                for(int i=0;i<4;i++)
                {double u=Math.Max(start,Math.Min(end,end-.11*i));Point p=KnowledgeCurve(a,b,c,d,u);Vector n=KnowledgeNormal(a,b,c,d,u);dc.DrawGeometry(Brush(pale,fade*.9),null,Face(p-n*5,p+n*8,p+n*16+new Vector(-4,-9)));}
            }
            if(lane==1)
            {
                Point p=KnowledgeCurve(a,b,c,d,end*.72);Vector n=KnowledgeNormal(a,b,c,d,end*.72);
                Line(dc,pale,fade*.85,2,p,p+n*20+new Vector(8,-6));Line(dc,pale,fade*.8,1.6,p+n*20+new Vector(8,-6),p+n*28+new Vector(0,-17));
            }
        }
        private void DrawFusion(DrawingContext dc,double age,double soft)
        {
            Point center=FusionCore;double fade=Window(age,.16,1.12,1.48)*soft;
            FusionRibbon(dc,new Point(269,126),new Point(254,167),new Point(274,207),center,age,0,fade);
            FusionRibbon(dc,new Point(390,132),new Point(339,126),new Point(325,188),center,age,1,fade);
            FusionRibbon(dc,new Point(386,312),new Point(322,306),new Point(343,238),center,age,2,fade);
            double appear=Ease((age-.37)/.68),collapse=Ease((age-1.19)/.33),radius=(7+25*appear)*(1-.43*collapse);
            double body=Ease((age-.37)/.22)*(1-Ease((age-1.39)/.14));
            if(body>.001)
            {
                Glow(dc,center,radius*1.8,Color.FromRgb(253,237,149),body*.25*soft);
                dc.PushOpacity(body);dc.PushTransform(new RotateTransform(-17+age*18,center.X,center.Y));
                var outline=new StreamGeometry();using(var g=outline.Open())
                {g.BeginFigure(Around(center,radius,radius,0),true,true);for(int i=1;i<72;i++){double a=i*Math.PI/36,r=radius*(1+.08*Math.Sin(a*5)+.035*Math.Sin(a*3));g.LineTo(Around(center,r,r,a),true,false);}}outline.Freeze();
                dc.DrawGeometry(Brush(Color.FromRgb(4,26,36),1),new Pen(Brush(Color.FromRgb(255,238,174),soft),4),outline);
                for(int i=0;i<5;i++)
                {
                    double a=i*2.399,reach=i==0?.0:.49;
                    Point p=Around(center,radius*reach,radius*reach,a);double r=radius*(i==0?.40:.29+(i%2)*.06);
                    dc.DrawEllipse(Brush(Color.FromRgb(35,133,143),1),new Pen(Brush(Color.FromRgb(146,249,224),1),1.6),p,r*1.04,r*1.18);
                    dc.DrawEllipse(Brush(Color.FromRgb(83,229,206),1),null,p+new Vector(-r*.16,-r*.12),r*.81,r*.9);
                    dc.DrawEllipse(Brush(Color.FromRgb(196,255,233),.95),null,p+new Vector(-r*.29,-r*.35),r*.39,r*.43);
                }
                dc.Pop();dc.Pop();
            }
            double impact=Window(age-1.09,.07,.12,.40)*soft;
            if(impact>0)
            {
                Glow(dc,center,43,Color.FromRgb(255,237,159),impact*.45);
                for(int i=0;i<7;i++)
                {double a=i*2.399;Point p=Around(center,34+(age-1.1)*38,29+(age-1.1)*33,a);Stamp(dc,GameArt.Bitmap("fx-plasma"),p,4,9,a*180/Math.PI,impact*.72);}
            }
        }
    }
}
