using System;
using System.Collections.Generic;

namespace DefectCompanion
{
    public sealed class RefreshingAttachment
    {
        public readonly double Appear,Hold,Fade;
        private double started=-100,full=-100,end=-100,from;
        public double CreatedAt {get;private set;}
        public RefreshingAttachment(double appear,double hold,double fade){Appear=appear;Hold=hold;Fade=fade;CreatedAt=-100;}
        private static double Smooth(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
        public double Alpha(double now)
        {
            if(now<started || now>=end)return 0;
            if(now<full)return from+(1-from)*Smooth((now-started)/Math.Max(.0001,full-started));
            if(now<full+Hold)return 1;
            return 1-Smooth((now-full-Hold)/Fade);
        }
        public void Cast(double now)
        {double alpha=Alpha(now);if(alpha<=0)CreatedAt=now;from=alpha;started=now;full=now+(alpha<1?Appear:0);end=full+Hold+Fade;}
        public void Clear(){started=full=end=CreatedAt=-100;}
    }
    public sealed class ModPlayback
    {
        private sealed class Pending {public PetEvent Item;public double At;}
        private readonly List<Pending> pending=new List<Pending>();
        public readonly RefreshingAttachment Ice=new RefreshingAttachment(.23,2,.6);
        public readonly RefreshingAttachment Charge=new RefreshingAttachment(.3,2,.6);
        public readonly List<double> Walls=new List<double>();
        public readonly List<double> BufferDebris=new List<double>();
        public double HologramAt=-100,TurboAt=-100,BufferAt=-100,BufferHitAt=-100,BufferBreakAt=-100;
        public bool TurboCore,BufferActive;
        public void Card(string skill,double now)
        {
            TurboCore=false;
        }
        public void Event(PetEvent e,double now)
        {
            if(e.Kind=="reboot"){Reset();return;}
            if(e.Kind!="hologram" && e.Kind!="charge" && e.Kind!="focus" && e.Kind!="shield" && !(e.Kind=="channel" && e.Card=="frost"))return;
            if(e.Delay>.00001){pending.Add(new Pending{At=now+e.Delay,Item=new PetEvent(e.Kind,e.Orb,e.Strength){Card=e.Card}});return;}
            Apply(e,now);
        }
        private void Apply(PetEvent e,double at)
        {
            if(e.Kind=="hologram")HologramAt=at;
            if(e.Kind=="channel" && e.Card=="frost")Ice.Cast(at);
            if(e.Kind=="charge")
            {if(e.Card=="turbo"){TurboAt=at;TurboCore=true;}else Charge.Cast(at);}
            if(e.Kind=="focus")Walls.Add(at);
            if(e.Kind=="shield")
            {
                if(e.Card=="buffer")
                {if(!BufferActive){BufferAt=at;BufferHitAt=-100;}BufferActive=true;}
                else if(BufferActive)
                {BufferHitAt=at;if(e.Strength<=1){BufferActive=false;BufferBreakAt=at;BufferDebris.Add(at);}}
            }
        }
        public void Update(double now,int bufferAmount)
        {
            for(int i=0;i<pending.Count;)
            {if(pending[i].At>now){i++;continue;}var item=pending[i];pending.RemoveAt(i);Apply(item.Item,item.At);}
            Walls.RemoveAll(t=>now-t>=1.8);
            BufferDebris.RemoveAll(t=>now-t>=1.2);
            if(BufferActive && bufferAmount<=0)BufferActive=false;
        }
        public void Reset()
        {
            Ice.Clear();Charge.Clear();Walls.Clear();BufferDebris.Clear();pending.Clear();HologramAt=TurboAt=BufferAt=BufferHitAt=BufferBreakAt=-100;BufferActive=TurboCore=false;
        }
        public double HologramOpen(double now)
        {
            double age=now-HologramAt;if(age<0 || age>=3)return 0;
            return age<2.0/3?age/(2.0/3):Math.Max(0,1-(age-2.0/3-1.2820512820512822)/(2.0/3));
        }
    }
}
