using System;
using System.IO;
using System.Windows.Threading;

namespace DefectCompanion
{
    // Deterministic captures exercise the same renderer and card rules as the desktop app.
    // The clock override belongs exclusively to this explicitly selected export process.
    public static class ShowcaseExport
    {
        public static void Run(PetWindow window,string folder)
        {
            string[] cards={"orb-arc-three","orb-arc-five","machine_learning","plasma","battery-learning","frost-learning","learning-battery","learning-frost","learning-proc","fusion-echo","fusion-full","overclock-learning","ice_lance","overclock","ice-echo","ice-frost","overclock-battery","claw","ball_lightning","claw-combo","ball-echo","claw-frost","rocket","dualcast","thunder","storm","turbo","tempest","glacier","rainbow","dark","echo","hyperbeam","dualcast-frost","dualcast-dark","dualcast-plasma","frost","battery","thunder-proc"};
            int card=0,frame=0;Directory.CreateDirectory(folder);
            var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(10)};
            timer.Tick+=delegate {
                try
                {
                    for(int batch=0;batch<4 && card<cards.Length;batch++)
                    {
                        window.PreviewTime=100+card*10+frame/30.0;
                        string skill=cards[card].StartsWith("dualcast-")?"dualcast":cards[card]=="thunder-proc"?"thunder":cards[card].StartsWith("claw-")?"claw":cards[card]=="ball-echo"?"ball_lightning":cards[card].StartsWith("ice-")?"ice_lance":cards[card]=="overclock-battery"?"overclock":cards[card];
                        if(cards[card].StartsWith("learning-"))skill="machine_learning";
                        if(cards[card]=="battery-learning" || cards[card]=="frost-learning")skill="machine_learning";
                        if(cards[card].StartsWith("fusion-"))skill="plasma";
                        if(cards[card]=="overclock-learning")skill="overclock";
                        if(cards[card].StartsWith("orb-arc-"))skill="rainbow";
                        if(frame==0)
                        {
                            window.Engine.Now=window.Seconds;window.Engine.Use("reboot");window.View.TakeEvents();
                            if(skill=="dualcast")
                            {window.Engine.Use(cards[card]=="dualcast-frost"?"frost":cards[card]=="dualcast-dark"?"dark":cards[card]=="dualcast-plasma"?"plasma":"zap");window.Engine.Events.Clear();}
                            if(cards[card]=="claw-combo"){for(int n=0;n<6;n++)window.Engine.Use("claw");window.Engine.Events.Clear();}
                            if(cards[card]=="ball-echo")window.Use("echo");
                            if(cards[card]=="claw-frost")window.Use("frost");
                            if(cards[card]=="ice-echo"){window.Use("capacitor");window.Use("echo");}
                            if(cards[card]=="ice-frost")window.Use("frost");
                            if(cards[card]=="fusion-echo")window.Use("echo");
                            if(cards[card]=="fusion-full")window.Use("rainbow");
                            if(cards[card]=="overclock-learning")window.Use("machine_learning");
                            if(cards[card]=="battery-learning")window.Use("battery");
                            if(cards[card]=="frost-learning")window.Use("frost");
                            if(cards[card]=="orb-arc-five")window.Use("capacitor");
                            window.Use(skill);
                        }
                        if(cards[card]=="buffer" && (frame==50 || frame==70 || frame==90))window.Use("zap");
                        if(cards[card]=="defragment" && (frame==12 || frame==24))window.Use("defragment");
                        if(cards[card]=="hologram" && frame==30)window.Use("frost");
                        if(cards[card]=="echo" && frame==60)window.Use("rocket");
                        if(cards[card]=="thunder-proc" && frame==60){window.Use("zap");window.Use("dualcast");}
                        if(cards[card]=="overclock-battery" && frame==24)window.Use("battery");
                        if(cards[card]=="learning-battery" && frame==24)window.Use("battery");
                        if(cards[card]=="learning-frost" && frame==24)window.Use("frost");
                        if(cards[card]=="learning-proc" && (frame==27 || frame==45 || frame==63 || frame==81))window.Use("leap");
                        if(cards[card]=="orb-arc-five" && frame==30)window.Use("zap");
                        if(cards[card]=="orb-arc-five" && frame==60)window.Use("plasma");
                        window.Engine.Tick(window.Seconds);window.View.TakeEvents();
                        UiChecks.Capture(window,folder,cards[card]+"-"+frame.ToString("D3"));
                        int length=cards[card].StartsWith("overclock")?210:skill=="machine_learning" || cards[card].StartsWith("orb-arc-")?150:skill=="plasma"?120:105;
                        frame++;if(frame==length){frame=0;card++;}
                    }
                    if(card==cards.Length){timer.Stop();File.WriteAllText(Path.Combine(folder,"showcase.txt"),"4815 deterministic renderer frames: Three-slot and five-slot orb arcs, channel arrival, shared evoke/passive origins; original neck and two rear pipes in Machine Learning; charging/frost attachments; Fusion/Echo/full-slot cases; all prior gauge, spear and orb scenes retained. 30 fps export; desktop targets 60 fps.");window.Close();}
                }
                catch(Exception error){timer.Stop();File.WriteAllText(Path.Combine(folder,"showcase.txt"),"FAIL "+error);window.Close();}
            };
            timer.Start();
        }
    }
}
