using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DefectCompanion
{
    // Isolated, real-time screenshots of each revised effect without previous-card residue.
    public static class StyleChecks
    {
        public static void Run(PetWindow window,string folder)
        {
            string[] cards={"machine_learning","plasma","rocket","dualcast","thunder","storm","turbo","tempest","glacier","rainbow","echo","hyperbeam","battery","frost","buffer","claw","ball_lightning","ice_lance","overclock"};
            double[] shots={.14,.30,.46,.68,1.02,1.50,2.25,3.10};int card=0,shot=0;double started=window.Seconds;bool powers=false;
            Action<string> begin=delegate(string skill) {
                window.Engine.Use("reboot");window.View.TakeEvents();
                if(skill=="hologram" || skill=="dualcast") {window.Engine.Use("zap");window.Engine.Events.Clear();}
                window.Use(skill);
            };
            begin(cards[0]);
            var timer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(40)};
            timer.Tick+=delegate {
                try
                {
                    window.Engine.Tick(window.Seconds);window.View.TakeEvents();window.View.InvalidateVisual();
                    double t=window.Seconds-started;
                    if(!powers && shot<shots.Length && t>=shots[shot]) UiChecks.Capture(window,folder,"style-"+cards[card]+"-"+shot++);
                    if(!powers && t>=3.35)
                    {
                        card++;shot=0;started=window.Seconds;
                        if(card<cards.Length) begin(cards[card]);
                        else
                        {
                            powers=true;window.Engine.Use("reboot");window.View.TakeEvents();
                            foreach(string power in new [] {"capacitor","defragment","storm","thunder","loop","creative_ai","machine_learning","overclock","buffer","echo"}) window.Use(power);
                        }
                    }
                    if(powers && t>=1.65)
                    {
                        UiChecks.Capture(window,folder,"style-power-bar");
                        File.WriteAllText(Path.Combine(folder,"style-checks.txt"),"Captured nineteen card effects at eight moments (152 frames), including learning head and fusion, plus the native power-icon row.");
                        timer.Stop();window.Close();
                    }
                }
                catch(Exception error)
                {File.WriteAllText(Path.Combine(folder,"style-checks.txt"),"FAIL "+error);timer.Stop();Application.Current.Shutdown(1);}
            };
            timer.Start();
        }
    }
}
