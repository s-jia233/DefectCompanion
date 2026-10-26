using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private readonly ModPlayback mod=new ModPlayback();
        private readonly Dictionary<BitmapSource,BitmapSource> hologramColors=new Dictionary<BitmapSource,BitmapSource>();
        private readonly Dictionary<Orb,Point> channelOrigins=new Dictionary<Orb,Point>();
        private ModHeadPose HeadBinding {get {return InstalledModArt.HeadPoses[native.ClipName][native.Frame];}}
        public bool InstalledModReady {get {return InstalledModArt.Ready;}}
        private static StreamGeometry CablePath(Point[] points)
        {
            var path=new StreamGeometry();using(var g=path.Open()){g.BeginFigure(points[0],false,false);for(int i=1;i<points.Length;i++)g.LineTo(points[i],true,false);}path.Freeze();return path;
        }
        private void DrawInstalledRear(DrawingContext dc,double now)
        {
            mod.Update(now,owner.Engine.ShieldHits);
            DrawBufferSign(dc,now);
            foreach(double started in mod.Walls)
            {
                double age=now-started;if(age<0 || age>=1.8)continue;
                dc.DrawImage(InstalledModArt.Frame("wall",(int)(age*60)),new Rect(79,94,320,267));
            }
            double charge=mod.Charge.Alpha(now);
            if(charge>0)
            {
                var pose=HeadBinding;dc.PushTransform(characterTransform);dc.PushOpacity(charge);
                for(int side=0;side<2;side++)
                {
                    Point mount=side==0?pose.RedMount:pose.BlueMount;
                    var offsets=InstalledModArt.CableRests[side];var bend=new Point[offsets.Length];
                    double cs=Math.Cos(pose.Angle),sn=Math.Sin(pose.Angle);
                    for(int i=0;i<bend.Length;i++){var p=offsets[i];bend[i]=new Point(mount.X+p.X*cs-p.Y*sn,mount.Y+p.X*sn+p.Y*cs);}
                    DrawCable(dc,bend,side);
                    Vector direction=bend[bend.Length-1]-bend[bend.Length-2];direction.Normalize();
                    var shape=InstalledModArt.Cables[side];double phase=(now-mod.Charge.CreatedAt)%2*60;
                    int f=(int)phase,n=(f+1)%120;double mix=phase-f;
                    Point anchor=bend[bend.Length-1];double ratio=.085/.3;
                    double drop=Math.Max(.15,Math.Min(2.5,(336.4-anchor.Y-ratio*4.3)/(shape.Drop*ratio)));
                    var cable=new Point[53];
                    Point q=shape.Frames[f][1],r=shape.Frames[n][1];Vector first=new Vector((q.X+(r.X-q.X)*mix)*ratio,(q.Y+(r.Y-q.Y)*mix)*drop*ratio);
                    Vector correction=direction*(810+side*20)/52*ratio-first;
                    for(int i=0;i<53;i++)
                    {
                        q=shape.Frames[f][i];r=shape.Frames[n][i];double k=Math.Max(0,1-(i-1)/9.0);k*=k;
                        cable[i]=anchor+new Vector((q.X+(r.X-q.X)*mix)*ratio,(q.Y+(r.Y-q.Y)*mix)*drop*ratio)+correction*k;
                        cable[i].Y=Math.Min(336.4,cable[i].Y);
                    }
                    cable[0]=anchor;cable[1]=anchor+direction*(810+side*20)/52*ratio;DrawCable(dc,cable,side);
                }
                dc.Pop();dc.Pop();
            }
            DrawCoolingSnow(dc,now);
            // The head and cowl occlude the rear clamps at their actual contact edge.
            DrawChargingClamps(dc,now);
            double open=mod.HologramOpen(now);
            if(open>0)DrawInstalledHologram(dc,now,open);
        }
        private void DrawCable(DrawingContext dc,Point[] points,int side)
        {
            var path=CablePath(points);Color color=side==0?Color.FromRgb(111,47,47):Color.FromRgb(45,83,100);
            dc.DrawGeometry(null,new Pen(Brush(Color.FromRgb(19,27,31),.95),3.1),path);
            var tile=new ImageBrush(GameArt.Bitmap("mod-charge-"+(side==0?"red":"blue")+"-cable")){TileMode=TileMode.Tile,ViewportUnits=BrushMappingMode.Absolute,Viewport=new Rect(0,0,24,3.4),Stretch=Stretch.Fill};
            dc.DrawGeometry(null,new Pen(tile,2.15),path);dc.DrawGeometry(null,new Pen(Brush(color,.32),.6),path);
        }
        private void DrawInstalledFront(DrawingContext dc,double now)
        {
            double ice=mod.Ice.Alpha(now);
            if(ice>0)
            {
                NativeClip clip=InstalledModArt.ColdHeadClips[native.ClipName];
                dc.PushTransform(characterTransform);dc.PushOpacity(ice);
                dc.DrawImage(InstalledModArt.Frame("cold-head-"+native.ClipName,native.Frame),clip.Bounds);dc.Pop();dc.Pop();
            }
            double charge=mod.Charge.Alpha(now);
            if(charge>0)
            {
                Point p=characterTransform.Transform(HeadBinding.Arc);
                if(charge>0)
                {
                    double phase=(now-mod.Charge.CreatedAt)*14;
                    for(int i=0;i<2;i++)
                        Stamp(dc,GameArt.Sequence(i+1,(int)(phase+i*3)%9),new Point(p.X-7+i*14,p.Y-4),39,37,i==0?-24:27,(owner.Options.Quiet?.18:.65));
                }
            }
        }
        private void DrawChargingClamps(DrawingContext dc,double now)
        {
            double charge=mod.Charge.Alpha(now);if(charge<=0)return;
            var p=HeadBinding;dc.PushTransform(characterTransform);dc.PushOpacity(charge);
            for(int side=0;side<2;side++)
            {
                Point origin=side==0?p.Red:p.Blue;
                var texture=GameArt.Bitmap("mod-charge-"+(side==0?"red":"blue")+"-clamp");
                dc.PushTransform(new RotateTransform(p.Angle*180/Math.PI,origin.X,origin.Y));
                dc.DrawImage(texture,new Rect(origin.X,origin.Y,texture.PixelWidth*.085/1.2,texture.PixelHeight*.085/1.2));dc.Pop();
            }
            dc.Pop();dc.Pop();
        }
        private void DrawCoolingSnow(DrawingContext dc,double now)
        {
            double cold=mod.Ice.Alpha(now);if(cold<=0)return;
            Point head=ActionAnchor(3);int count=owner.Options.Quiet?10:27;
            double elapsed=now-mod.Ice.CreatedAt;
            for(int i=0;i<count;i++)
            {
                double phase=(elapsed*(.28+(i%4)*.035)+i*.381966)%1;
                double x=head.X-84+(i*61%167)+Math.Sin(elapsed*1.1+i*2.7)*10;
                double y=103+phase*183;
                double size=1.05+(i%4)*.38;
                double alpha=cold*Math.Min(1,phase*9)*Math.Min(1,(1-phase)*4)*(.5+(i%3)*.15);
                var pen=new Pen(Brush(Color.FromRgb(212,244,255),alpha),.8);
                dc.DrawLine(pen,new Point(x-size,y),new Point(x+size,y));
                dc.DrawLine(pen,new Point(x,y-size),new Point(x,y+size));
                if(i%3==0)
                {
                    dc.DrawLine(pen,new Point(x-size*.7,y-size*.7),new Point(x+size*.7,y+size*.7));
                    dc.DrawLine(pen,new Point(x-size*.7,y+size*.7),new Point(x+size*.7,y-size*.7));
                }
            }
        }
        private void DrawBufferSign(DrawingContext dc,double now)
        {
            bool broken=!mod.BufferActive && mod.BufferDebris.Count>0;
            if(!mod.BufferActive && !broken)return;
            double age=now-mod.BufferAt,fracture=now-mod.BufferBreakAt;
            // Source sign placement: authored foot/anchor, 88% size, and native body height.
            const double scale=.085*(2138.0555647211145/325.76)*.1992811;
            Point foot=new Point(143,337);double k=Ease(age/.32);
            double angle=(1.16-1.215*k)*180/Math.PI;
            if(age>=.32 && age<.43)angle=(-.055+.073*Ease((age-.32)/.11))*180/Math.PI;
            if(age>=.43)angle=.018*Math.Exp(-10*Math.Pow(age-.43,2)/(age-.43+.05))*Math.Cos((age-.43)*Math.PI*2/.18)*180/Math.PI;
            double hitAge=now-mod.BufferHitAt;
            if(hitAge>0 && hitAge<.4)angle-=4.3*Math.Sin(hitAge/.4*Math.PI)*Math.Exp(-hitAge*5);
            double alpha=Ease(age/.055)*(broken?1-Ease((fracture-.78)/.32):1);
            var transform=new TransformGroup();
            transform.Children.Add(new TranslateTransform(-670,-2232));transform.Children.Add(new ScaleTransform(scale*(.8+.2*k),scale*(.54+.46*k)));
            transform.Children.Add(new RotateTransform(broken?Math.Min(1,fracture/.48)*-87:angle));
            transform.Children.Add(new TranslateTransform(foot.X+46*(1-k)*.88*scale,foot.Y));
            dc.PushOpacity(alpha);dc.PushTransform(transform);
            if(!broken || fracture<.025)
                dc.DrawImage(GameArt.Bitmap("mod-buffer-sign"),InstalledModArt.BufferRects["sign"]);
            else
            {
                dc.DrawImage(GameArt.Bitmap("mod-buffer-pole"),InstalledModArt.BufferRects["pole"]);
            }
            dc.Pop();dc.Pop();
            foreach(double brokenAt in mod.BufferDebris)
            {
                double tail=now-brokenAt;if(tail<.025 || tail>=1.2)continue;
                double flight=tail-.025;
                double[] vx={275,-176,-691},spin={-1.769,-2.546,1.487};
                for(int i=0;i<3;i++)
                {
                    Rect box=InstalledModArt.BufferRects["fragment-"+i];
                    var center=new Point(foot.X+(box.X+box.Width/2-670)*scale+vx[i]*scale*flight,foot.Y+(box.Y+box.Height/2-2232)*scale-50*flight+230*flight*flight);
                    center.Y=Math.Min(337-box.Height*scale*.2,center.Y);
                    Stamp(dc,GameArt.Bitmap("mod-buffer-fragment-"+i),center,box.Width*scale,box.Height*scale,spin[i]*flight*180/Math.PI,1-Ease((tail-.78)/.32));
                }
            }
            if(mod.BufferActive)
                Stamp(dc,GameArt.Bitmap("mod-buffer-light-field"),new Point(155,234),141,165,0,(.11+.025*Math.Sin(now*3))*Ease(age/.5));
        }
        private BitmapSource HologramColor(BitmapSource source)
        {
            BitmapSource result;if(hologramColors.TryGetValue(source,out result))return result;
            if(hologramColors.Count>=128)hologramColors.Clear();
            int w=source.PixelWidth,h=source.PixelHeight;var bytes=new byte[w*h*4];
            var rgba=new FormatConvertedBitmap(source,PixelFormats.Bgra32,null,0);rgba.CopyPixels(bytes,w*4,0);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                int i=(y*w+x)*4;double l=(bytes[i+2]*.25+bytes[i+1]*.60+bytes[i]*.15)/255;
                double k=Ease((l-.05)/.47),v=Ease((l-.48)/.46);double band=(x%5<2)?.66:1;
                bytes[i]=(byte)Math.Min(255,(33+135*k+87*v)*band);bytes[i+1]=(byte)Math.Min(255,(14+88*k+138*v)*band);bytes[i+2]=(byte)Math.Min(255,(3+2*k+112*v)*band);bytes[i+3]=(byte)(bytes[i+3]*.82);
            }
            result=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,bytes,w*4);result.Freeze();hologramColors[source]=result;return result;
        }
        private void DrawInstalledHologram(DrawingContext dc,double now,double open)
        {
            var sample=native.Past(now-.12);Rect rect=characterTransform.TransformBounds(sample.Bounds);rect.Offset(-48,-2);
            var image=HologramColor(sample.Image);var mask=new GeometryGroup();double middle=rect.X+rect.Width*.5;
            int count=(int)Math.Ceiling(rect.Width/5);int half=(int)Math.Ceiling((2+(count*.5-2)*Ease(open)));
            for(int col=-half;col<=half;col++)
            {
                double width=open<.3?1.3:Math.Min(5,1.3+3.7*Ease((open-.25)/.6));
                double height=open>.72?rect.Height:rect.Height*Math.Min(1,open*4+.08)*(1-.04*(Math.Abs(col)%5));
                mask.Children.Add(new RectangleGeometry(new Rect(middle+col*5-width*.5,rect.Y+(rect.Height-height)*.5,width,Math.Max(1,height))));
            }
            dc.PushClip(mask);dc.PushOpacity(owner.Options.Quiet?.3:.9);dc.DrawImage(image,rect);dc.Pop();dc.Pop();
        }
    }
}
