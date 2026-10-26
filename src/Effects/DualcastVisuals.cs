using System;
using System.Windows;
using System.Windows.Media;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        // Desktop projection of local MoreDefectAnimations v0.17.1 Multicast effects.
        // Dualcast uses the smooth trail branch. Fractured flame is Quadcast-only.
        private void DrawModDual(DrawingContext dc,VisualEffect e,double t,bool rear)
        {
            if(t<0 || t>1.05)return;
            double soft=owner.Options.Quiet?.26:1;
            if(!e.Launch.HasValue)e.Launch=e.Orb==OrbKind.Frost?ActionAnchor(2):e.Origin;
            int ordinal=e.Kind=="evoke"?0:1;
            Point source=e.Launch.Value;
            if(e.Orb==OrbKind.Frost){DualFrost(dc,source,t,ordinal,rear,soft);return;}
            if(e.Orb==OrbKind.Lightning)
            {if(!rear)DualLightning(dc,source,t,ordinal,soft);return;}
            bool plasma=e.Orb==OrbKind.Plasma;
            double duration=plasma?.4:.46;
            Point target=plasma?new Point(209,355):new Point(377,258);
            Point b,c;
            if(plasma)
            {
                Vector delta=target-source;
                b=ordinal==0?source+new Vector(delta.X*.09,-Math.Abs(delta.Y)*.48):source+new Vector(Math.Abs(delta.X)*.1125,Math.Abs(delta.Y)*.49);
                c=ordinal==0?target+new Vector(-Math.Abs(delta.X)*.1,-Math.Abs(delta.Y)*1.37):target+new Vector(Math.Abs(delta.X)*.392,-Math.Abs(delta.Y)*.17);
            }
            else
            {b=new Point(source.X+35,source.Y-(ordinal==0?100:51));c=new Point(target.X-52,target.Y-(ordinal==0?86:40));}
            double head=Math.Min(1,t/duration),tail=Math.Max(0,head-(plasma?17.0/60:2.0/15)/duration);
            double fade=Math.Pow(Math.Max(0,1-Math.Max(0,t-duration)/.25),1.7)*soft;
            if(rear)
            {
                if(head>tail && fade>.001)DualSmoothTrail(dc,source,b,c,target,tail,head,t,fade,plasma);
                return;
            }
            if(t<duration)
            {
                Point p=Cubic(source,b,c,target,head);
                Color body=plasma?Color.FromRgb(51,232,222):Color.FromRgb(104,74,255);
                Glow(dc,p,plasma?19:25,body,fade*.5);
                if(plasma)
                {dc.DrawEllipse(Brush(Color.FromRgb(255,243,91),fade*.85),null,p,4.5,4.5);}
                else
                {
                    Stamp(dc,GameArt.Bitmap("orb-dark"),p,30,30,0,fade);
                    Arc(dc,p,13,13,t*4,3.4,Color.FromRgb(255,69,219),fade*.65,1.6);
                }
            }
            else
            {
                double hit=t-duration,alpha=(1-Ease(hit/.25))*soft;
                Glow(dc,target,8+Ease(hit/.22)*(plasma?19:31),plasma?Color.FromRgb(255,243,91):artViolet,alpha*.58);
                if(plasma)
                {Arc(dc,target,8+hit*45,4+hit*20,-Math.PI,Math.PI,Color.FromRgb(255,243,91),alpha,2.2);}
                else
                {dc.DrawEllipse(Brush(Color.FromRgb(12,8,25),alpha*.9),null,target,14*(1-Ease(hit/.25)),18*(1-Ease(hit/.25)));}
            }
        }

        private void DualSmoothTrail(DrawingContext dc,Point a,Point b,Point c,Point d,double tail,double head,double age,double fade,bool plasma)
        {
            // Smooth envelope and longitudinal colour ramp replace all teeth and shards.
            const int count=49;var centers=new Point[count];var normals=new Vector[count];
            for(int i=0;i<count;i++)
            {
                double u=tail+(head-tail)*i/(count-1.0);centers[i]=Cubic(a,b,c,d,u);
                Vector v=Cubic(a,b,c,d,Math.Min(1,u+.002))-Cubic(a,b,c,d,Math.Max(0,u-.002));
                normals[i]=new Vector(-v.Y,v.X);if(normals[i].Length<.001)normals[i]=new Vector(0,1);normals[i].Normalize();
            }
            Color body=plasma?Color.FromRgb(51,232,222):Color.FromRgb(104,74,255);
            Color rim=plasma?Color.FromRgb(255,243,91):Color.FromRgb(239,118,255);
            for(int layer=0;layer<4;layer++)
            {
                double size=(plasma?11:14)*(layer==0?1.30:layer==1?1:layer==2?.78:.19);
                var points=new Point[count*2];
                for(int i=0;i<count;i++)
                {
                    double u=i/(count-1.0),width=size*Math.Pow(u,.75)*Math.Min(1,(1-u)*9);
                    double wave=layer==3?Math.Sin(u*9.4248+age*10.8)*(plasma?.345:.167)*size:0;
                    points[i]=centers[i]+normals[i]*(wave+width);
                    points[count*2-1-i]=centers[i]+normals[i]*(wave-width);
                }
                var fill=new LinearGradientBrush();fill.StartPoint=new Point(0,0);fill.EndPoint=new Point(1,0);
                Color col=layer==0?body:layer==1?rim:layer==2?body:rim;
                fill.GradientStops.Add(new GradientStop(Color.FromArgb(0,col.R,col.G,col.B),0));
                fill.GradientStops.Add(new GradientStop(col,.3));fill.GradientStops.Add(new GradientStop(col,1));
                fill.Freeze();dc.PushOpacity(fade*(layer==0?.16:layer==1?.86:layer==2?.96:.80));dc.DrawGeometry(fill,null,Face(points));dc.Pop();
            }
        }

        private static readonly double[][] dualLightningShape={
            new double[]{0,74,.07,105,.18,62,.24,80,.28,137,.35,99,.40,122,.51,53,.56,85,.64,43,.73,73,.83,25,.91,36,1,0},
            new double[]{0,85,.10,110,.17,66,.26,47,.30,105,.34,79,.40,139,.51,74,.58,86,.63,41,.71,76,.80,31,.91,47,1,0}};
        private void DualLightning(DrawingContext dc,Point source,double t,int ordinal,double soft)
        {
            if(t>.66)return;
            Point end=new Point(ordinal==0?329:362,ordinal==0?294:287);
            Point top=new Point(ordinal==0?300:335,ordinal==0?119:114);
            double alpha=Ease(t/.035)*(1-Ease((t-.19)/.34))*soft;
            double reveal=Ease(t/.085);double[] shape=dualLightningShape[ordinal];
            int sign=ordinal==0?-1:1;var points=new System.Collections.Generic.List<Point>();
            for(int i=0;i<shape.Length/2;i++)
            {
                double u=shape[i*2];if(u>reveal+.015)break;
                double bend=(shape[i*2+1]-shape[1]*(1-u))*.32*sign;
                points.Add(new Point(top.X+(end.X-top.X)*u+bend,top.Y+(end.Y-top.Y)*u));
            }
            if(points.Count>1)
            {
                PaintedBolt(dc,points.ToArray(),11.5,alpha*.92,false);
                for(int j=0;j<3;j++)
                {
                    int k=new int[]{3,7,10}[j];if(k>=points.Count-1)continue;
                    Point p=points[k];double side=j==1?-sign:sign;
                    double x=Math.Min(390,Math.Max(285,p.X+side*24));
                    PaintedBolt(dc,new Point[]{p,p+new Vector(side*11,8),new Point(x-5*side,p.Y+19),new Point(x,p.Y+33)},3.8,alpha*.83,false);
                }
            }
            if(t<.39)
            {
                Vector delta=end-top;double strength=1-Ease((t-.17)/.22);
                string variant=t<.10?"01b":t<.23?"03":"04";
                Stamp(dc,GameArt.Bitmap("bolt-"+variant),new Point((top.X+end.X)*.5,(top.Y+end.Y)*.5),ordinal==0?58:65,delta.Length,Math.Atan2(delta.Y,delta.X)*180/Math.PI-90,alpha*strength*.85);
            }
            // The source is now a short visible connection to the actual orb,
            // instead of an upward bolt leaving the top of the desktop window.
            if(t<.23)
            {
                double fade=(1-Ease((t-.08)/.15))*soft;Point bend=new Point((source.X+top.X)*.5+9,Math.Max(107,(source.Y+top.Y)*.5));
                PaintedBolt(dc,new Point[]{source,bend,top},5.8,fade*.9,false);
                Glow(dc,source,22,Color.FromRgb(175,225,255),fade*.55);
                if(t<.08)Stamp(dc,GameArt.Bitmap("orb-lightning"),source,24-14*Ease(t/.08),24-14*Ease(t/.08),0,fade);
            }
            double hit=t-.085;
            if(hit>=0 && hit<.41)
            {
                double u=Ease(hit/.41),fade=(1-u)*soft;
                Glow(dc,end,22+u*34,Color.FromRgb(167,220,255),fade*.58);
                Stamp(dc,GameArt.Bitmap("fx-flare"),end,48+u*48,27+u*22,ordinal==0?-8:9,fade*.85);
                if(hit<.25)Stamp(dc,GameArt.Sequence(2,Math.Min(8,(int)(hit/.25*9))),end,70,62,ordinal==0?-12:10,fade*.85);
                for(int side=-1;side<=1;side+=2)
                {
                    Point p=end+new Vector(side*7,-8);
                    PaintedBolt(dc,new Point[]{p,p+new Vector(side*12,-15),p+new Vector(side*20,-9),p+new Vector(side*29,-23)},3.1,fade*.8,false);
                }
            }
        }

        // The six Dualcast frost bands (the mod omits full-array indices 3 and 6).
        private static readonly double[][] dualFrostBands={
            new double[]{0,52,84,.45,5.7,.68,54,.16,2.85,0,1},
            new double[]{2,79,80,3.2,5.2,.58,48,.13,2.6,0,.86},
            new double[]{1,18,88,4.5,5.9,.65,58,.15,2.95,0,.5},
            new double[]{2,149,49,2.1,5.5,.42,24,.1,1.9,0,.44},
            new double[]{1,85,108,5.15,6.1,.51,79,.08,1.5,-14,.48},
            new double[]{3,-52,43,3.9,5.6,.33,19,.1,1.6,0,.36}};
        private static readonly double[][] dualFrostProfiles={
            new double[]{0,0,.13,.54,.35,1,.63,.91,.83,.5,1,0},
            new double[]{0,0,.22,.25,.58,.68,.77,1,.88,.46,1,0},
            new double[]{0,0,.22,.72,.39,1,.53,.65,.69,.87,.87,.56,1,0},
            new double[]{0,0,.19,.43,.44,1,.65,.84,.83,.3,1,0}};
        private static double DualProfile(int shape,double u)
        {
            var p=dualFrostProfiles[shape];for(int i=2;i<p.Length;i+=2)if(u<=p[i])return p[i-1]+(p[i+1]-p[i-1])*(u-p[i-2])/(p[i]-p[i-2]);return 0;
        }
        private void DualFrost(DrawingContext dc,Point origin,double age,int ordinal,bool rear,double soft)
        {
            if(age>=.8)return;double fade=Ease(age/.065)*(1-Ease((age-.36)/.44))*soft;
            double drift=Math.Max(0,age-.2)*28,amount=.9+Ease(age/.065)*.1;
            double[] widths={17,9,16,8};const double scale=.66;
            foreach(var s in dualFrostBands)
            {
                int shape=(int)s[0];
                // Feathered strips, split by depth exactly as the source renderer.
                for(int feather=0;feather<3;feather++)for(int j=0;j<64;j++)
                {
                    double u=j/64.0,v=(j+1)/64.0,m=(u+v)*.5;
                    double theta=s[3]+ordinal*.28+s[8]*m+(.16+age*1.65)*s[4];
                    if((Math.Sin(theta)>0)!=rear)continue;
                    var points=new Point[4];
                    for(int corner=0;corner<4;corner++)
                    {
                        double q=corner<2?u:v,side=corner==0 || corner==3?1:-1;
                        double angle=s[3]+ordinal*.28+s[8]*q+(.16+age*1.65)*s[4];
                        double width=widths[shape]*s[5]*1.9*DualProfile(shape,q)*amount*(1.22-feather*.25);
                        double r=(s[2]+drift)*amount+side*width;
                        double z=Math.Sin(angle)*r*.85;
                        double y=s[1]+(q-.5)*s[6]+z*(s[7]+.22)-drift*.45;
                        double x=Math.Cos(angle)*r-z*.08;
                        points[corner]=new Point(origin.X+(x*1.02+s[9])*scale,origin.Y+(-y*1.12-age*8)*scale+22);
                    }
                    dc.DrawGeometry(Brush(Color.FromRgb(180,205,206),fade*s[10]*(feather==0?.08:feather==1?.12:.20)),null,Face(points));
                }
            }
            if(!rear && age<.46)
            {
                // Small dissolving orb pieces at the slot; no radial needles around the body.
                double fadeOrb=(1-Ease((age-.14)/.32))*soft;
                for(int i=0;i<6;i++)
                {
                    double a=i*Math.PI/3+.3,r=(1-Math.Exp(-age/.12))*23;
                    Point p=new Point(origin.X+Math.Cos(a)*r,origin.Y-66+Math.Sin(a)*r+age*age*40);
                    dc.DrawEllipse(Brush(Color.FromRgb(202,240,246),fadeOrb*.65),null,p,1.2+i%2,2+i%2);
                }
            }
        }
    }
}
