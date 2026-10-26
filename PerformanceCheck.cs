using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace DefectCompanion
{
    // Counts actual OnRender calls in a live transparent WPF window, not timer callbacks.
    public static class PerformanceCheck
    {
        private sealed class Sample
        {
            public readonly List<double> Times = new List<double>();
            public double CpuStart = -1, CpuEnd;
            public void Record(double now)
            {
                Times.Add(now);
                if (Times.Count == 1) CpuStart = CpuSeconds();
                CpuEnd = CpuSeconds();
            }
            public string Json()
            {
                double span = Times.Count > 1 ? Times.Last() - Times[0] : 0;
                double[] gaps = Times.Zip(Times.Skip(1), (a,b) => (b-a)*1000).OrderBy(x => x).ToArray();
                return "{\"frames\":" + Times.Count + ",\"fps\":" + Number(span > 0 ? (Times.Count-1)/span : 0) +
                    ",\"p95_frame_ms\":" + Number(gaps.Length > 0 ? gaps[(int)((gaps.Length-1)*.95)] : 0) +
                    ",\"cpu_percent_one_core\":" + Number(span > 0 ? (CpuEnd-CpuStart)/span*100 : 0) + "}";
            }
        }
        private static double CpuSeconds() { using (var p = Process.GetCurrentProcess()) return p.TotalProcessorTime.TotalSeconds; }
        private static string Number(double value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        public static void Run(PetWindow window, string folder)
        {
            Directory.CreateDirectory(folder);
            var idle=new Sample();var effects=new Sample();var echoIdle=new Sample();var spear=new Sample();var overclock=new Sample();var learning=new Sample();var fusion=new Sample();
            double epoch=window.Seconds,nextCast=8,nextSpear=22,nextFusion=40;int attack=0;bool echoStarted=false,hotStarted=false,learningStarted=false;
            string[] attacks={"overclock","ice_lance","rocket","dualcast","claw","ball_lightning","storm","thunder","turbo","tempest","rainbow","hyperbeam"};
            window.Use("rainbow");
            window.View.RenderedFrame=delegate(double now){
                double t=now-epoch;
                if(t>=2 && t<8)idle.Record(now);if(t>=8 && t<16)effects.Record(now);
                if(t>=18 && t<22)echoIdle.Record(now);if(t>=22 && t<28)spear.Record(now);if(t>=28 && t<35)overclock.Record(now);
                if(t>=35 && t<40)learning.Record(now);if(t>=40 && t<46)fusion.Record(now);
            };
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(100)};
            timer.Tick+=delegate {
                double t=window.Seconds-epoch;
                if(t>=nextCast && t<16){window.Use(attacks[attack++%attacks.Length]);nextCast=t+.65;}
                if(t>=16 && !echoStarted){echoStarted=true;window.Use("reboot");window.Use("rainbow");window.Use("echo");}
                if(t>=22 && t<28 && t>=nextSpear){window.Use("reboot");window.Use("frost");window.Use("ice_lance");nextSpear=t+2.6;}
                if(t>=28 && !hotStarted){hotStarted=true;window.Use("reboot");window.Use("overclock");}
                if(t>=35 && !learningStarted){learningStarted=true;window.Use("reboot");window.Use("machine_learning");}
                if(t>=40 && t<46 && t>=nextFusion){window.Use("plasma");nextFusion=t+2.6;}
                if(t<46)return;
                timer.Stop();window.View.RenderedFrame=null;long memory;using(var p=Process.GetCurrentProcess())memory=p.PrivateMemorySize64;
                string report="{\n\"target_fps\":60,\n\"measurement\":\"actual WPF OnRender calls / visible transparent window / two second warmup\","+
                    "\n\"idle_with_three_orbs\":"+idle.Json()+",\n\"rapid_card_effects\":"+effects.Json()+",\n\"echo_form_idle\":"+echoIdle.Json()+
                    ",\n\"held_ice_spear\":"+spear.Json()+",\n\"overclock_full_cycle\":"+overclock.Json()+",\n\"machine_learning\":"+learning.Json()+",\n\"fusion_sequence\":"+fusion.Json()+",\n\"private_memory_mb\":"+Number(memory/1048576.0)+",\n\"motion_samples\":"+window.View.MotionSampleCount+"\n}";
                File.WriteAllText(Path.Combine(folder,"frame-performance.json"),report);window.Close();
            };
            timer.Start();
        }
    }
}
