using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private sealed class IceFace {public Point[] Points;public Color Color;public double Depth;}
        private static readonly Color[] icePalette={Color.FromRgb(42,67,78),Color.FromRgb(55,87,100),Color.FromRgb(72,114,125),Color.FromRgb(101,149,157),Color.FromRgb(139,182,186),Color.FromRgb(190,212,210)};
        private static double IceRandom(double n){double v=Math.Sin(n*127.1+311.7)*43758.5453;return v-Math.Floor(v);}
        private static List<IceFace> Crystal(double clock,double fracture)
        {
            double roll=.35+clock*1.3;double[] xs={-103,-58,-6},rs={28,52,43};var rings=new Vector3D[3][];
            for(int i=0;i<3;i++){rings[i]=new Vector3D[6];for(int j=0;j<6;j++){double a=roll+j*Math.PI/3;rings[i][j]=new Vector3D(xs[i]+(j%2!=0?6:-4)*(i==0?1:.6),Math.Cos(a)*rs[i],Math.Sin(a)*rs[i]);}}
            var faces=new List<IceFace>();
            Action<Vector3D[],int,int> add=delegate(Vector3D[] vertices,int band,int side){
                double a=roll+(side+.5)*Math.PI/3;double shade=Math.Max(0,Math.Min(1,.6-.5*Math.Cos(a)+.1*Math.Sin(a)));
                int id=faces.Count;var points=new Point[vertices.Length];double depth=0;Point center=new Point();
                for(int i=0;i<vertices.Length;i++){var v=vertices[i];points[i]=new Point(v.X+v.Z*.22,v.Y-v.Z*.08);center.X+=points[i].X/vertices.Length;center.Y+=points[i].Y/vertices.Length;depth+=v.Z/vertices.Length;}
                Color color=icePalette[Math.Min(5,(int)Math.Floor(shade*5+(band==2?.25:0)))];
                if(fracture>=0)
                {
                    double life=.22+IceRandom(id+12)*.17;double spin=(IceRandom(id+44)-.5)*fracture*13;
                    double shrink=1-Ease((fracture-life*.35)/(life*.65));Vector drift=new Vector((140+IceRandom(id)*330)*fracture*1.6,center.Y/42*(100+IceRandom(id+9)*250)*fracture*1.6+160*fracture*fracture);
                    for(int i=0;i<points.Length;i++){Vector p=points[i]-center;points[i]=center+drift+new Vector(p.X*Math.Cos(spin)-p.Y*Math.Sin(spin),p.X*Math.Sin(spin)+p.Y*Math.Cos(spin))*shrink;}
                    color.A=(byte)(255*(1-Ease(fracture/life)));
                }
                faces.Add(new IceFace{Points=points,Color=color,Depth=depth});
            };
            for(int k=0;k<6;k++){int n=(k+1)%6;add(new []{new Vector3D(-121,0,0),rings[0][n],rings[0][k]},0,k);add(new []{rings[0][k],rings[0][n],rings[1][n],rings[1][k]},1,k);add(new []{rings[1][k],rings[1][n],rings[2][n],rings[2][k]},1,k);add(new []{rings[2][k],rings[2][n],new Vector3D(122,0,0)},2,k);}
            faces.Sort((a,b)=>a.Depth.CompareTo(b.Depth));return faces;
        }
        private static readonly double[][] windSpecs={new double[]{-50,75,.3,6.8,1,59,.6,3.9},new double[]{29,67,2.9,7.8,.76,46,.56,3.05},new double[]{-109,94,4.3,5.6,.95,92,.52,4.35},new double[]{1,101,2.4,5.2,.49,72,.65,3.9},new double[]{-51,55,5.1,8.8,.66,83,.6,4.35},new double[]{-118,68,1.5,6.1,.53,46,.55,3.05},new double[]{80,60,5.2,8.2,.7,30,.7,1.8},new double[]{-140,107,3.5,5.1,.65,34,.52,1.8}};
        private void DrawIceWind(DrawingContext dc,Point center,Vector dir,Vector normal,double t,bool front,double fade)
        {
            double impact=t-.3,fracture=t-.48;
            double coast=115*(1-Math.Pow(1-Math.Max(0,Math.Min(1,impact/.42)),3));
            double alpha=fracture<0?1:1-Ease(fracture/.34);if(alpha<=0)return;
            double grow=1+1.3*Ease(t/(.3+.34));
            for(int i=0;i<8;i++)
            {
                var s=windSpecs[i];double phase=s[2]+t*s[3];if((Math.Sin(phase)>0)!=front)continue;
                var path=new StreamGeometry();using(var g=path.Open())
                {
                    for(int j=0;j<=28;j++)
                    {
                        double u=j/28.0,a=phase+u*s[7],depth=Math.Sin(a)*s[1]*grow;
                        double x=s[0]+(u-.5)*s[5]+depth*(s[6]+.22)+coast;
                        double y=Math.Cos(a)*s[1]*grow-depth*.08;Point p=center+dir*x*.20+normal*y*.20;
                        if(j==0)g.BeginFigure(p,false,false);else g.LineTo(p,true,false);
                    }
                }
                dc.DrawGeometry(null,new Pen(Brush(Color.FromRgb(131,176,183),fade*alpha*.15),1.2+s[4]*1.6),path);
                dc.DrawGeometry(null,new Pen(Brush(Color.FromRgb(185,214,212),fade*alpha*.24),.65),path);
            }
        }
        private void DrawInstalledLance(DrawingContext dc,VisualEffect e,double t,double fade)
        {
            if(t>2.15)return;if(!e.Launch.HasValue)e.Launch=ActionAnchor(1);
            Point start=e.Launch.Value,target=new Point(398,start.Y-12);Vector dir=target-start;double distance=dir.Length;dir.Normalize();Vector normal=new Vector(-dir.Y,dir.X);
            double u=Math.Pow(Math.Max(0,Math.Min(1,(t/.3-.1)/.9)),1.28);
            Point initial=start+dir*Math.Min(34,distance*.9);Point tip=initial+(target-initial)*u-new Vector(0,Math.Sin(u*Math.PI)*2.4);
            if(t>=.3)tip=target;Point center=tip-dir*(122*.28);double fracture=t-.48;
            DrawIceWind(dc,center,dir,normal,t,false,fade);
            if(fracture<.4)
            {
                foreach(var face in Crystal(Math.Min(t,.3),fracture))
                {
                    if(face.Color.A==0)continue;var polygon=new StreamGeometry();using(var g=polygon.Open())
                    {for(int j=0;j<face.Points.Length;j++){Point q=face.Points[j];Point p=center+dir*q.X*.28+normal*q.Y*.28;if(j==0)g.BeginFigure(p,true,true);else g.LineTo(p,true,false);}}
                    dc.DrawGeometry(Brush(face.Color,fade),new Pen(Brush(Color.FromRgb(159,195,194),fade*.20),.4),polygon);
                }
            }
            DrawIceWind(dc,center,dir,normal,t,true,fade);
            for(int i=0;i<18;i++)
            {
                double birth=.3*(.12+i/17.0*.88),age=t-birth;if(age<0 || age>1.1)continue;
                double v=Math.Pow(Math.Max(0,Math.Min(1,(birth/.3-.1)/.9)),1.28);
                Point p=initial+(target-initial)*v+normal*((IceRandom(i)-.5)*26)+new Vector(Math.Sin(i)*age*7,age*9);
                int frame=Math.Min(15,(int)(age/1.1*16));Stamp(dc,InstalledModArt.Frame(i%3==0?"ice-mist":"ice-snow",frame),p,i%3==0?42:28,i%3==0?36:28,i*13,fade*(1-age/1.1)*.6);
            }
        }
    }
}
