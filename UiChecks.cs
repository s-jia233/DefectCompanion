using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DefectCompanion
{
    // Integration checks drive this application's own WPF events in an isolated test process.
    public static class UiChecks
    {
        private static readonly List<string> results = new List<string>();
        private static void Check(bool condition, string text)
        { if (!condition) throw new Exception(text); results.Add("PASS " + text); }
        private static IEnumerable<Button> Buttons(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var button = child as Button;
                if (button != null) yield return button;
                foreach (var nested in Buttons(child)) yield return nested;
            }
        }
        private static CardPicker Picker()
        { return Application.Current.Windows.OfType<CardPicker>().FirstOrDefault(); }
        private static bool NavigationHit(CardPicker picker,Button button,double x,double y)
        {
            var p=button.TranslatePoint(new Point(button.ActualWidth*x,button.ActualHeight*y),picker);
            var hit=picker.InputHitTest(p) as DependencyObject;
            while(hit!=null){if(hit==button)return true;hit=VisualTreeHelper.GetParent(hit);}
            return false;
        }
        private static void CheckOrbArc(PetWindow window,bool expanded)
        {
            window.Engine.Use("reboot");window.View.TakeEvents();
            window.Engine.Now=window.Seconds-2;
            if(expanded)window.Engine.Use("capacitor");
            OrbKind[] kinds=expanded?new []{OrbKind.Lightning,OrbKind.Frost,OrbKind.Dark,OrbKind.Plasma,OrbKind.Lightning}:new []{OrbKind.Lightning,OrbKind.Frost,OrbKind.Dark};
            foreach(var kind in kinds)window.Engine.Channel(kind);
            window.Engine.Tick(window.Seconds);window.View.TakeEvents();
            var points=Enumerable.Range(0,kinds.Length).Select(i=>window.View.OrbPosition(i)).ToArray();
            string label=expanded?"Five-slot arc: ":"Three-slot arc: ";
            Check(points[0].Y-points[kinds.Length/2].Y>30 && points[kinds.Length-1].Y-points[kinds.Length/2].Y>30,label+"the crown is visibly above both ends");
            Check(points.All(p=>p.X>26 && p.X<394 && p.Y>26 && p.Y<364) && points.Zip(points.Skip(1),(a,b)=>(a-b).Length).All(d=>d>48),label+"full orb artwork fits the stage without overlapping adjacent click areas");
            Check(points.Select((p,i)=>window.View.OrbAt(p)==i && window.View.IsInteractive(p) && window.View.InputHitTest(p)==window.View).All(hit=>hit),label+"actual WPF hit tests reach every displayed orb");
            Check(window.View.OrbAt(expanded?new Point(334,111):new Point(281,111))==-1,label+"the old horizontal endpoint has no leftover orb hit target");
            window.Engine.TapOrb(0);window.View.TakeEvents();
            Check(window.Engine.Orbs.Count==kinds.Length-1 && window.Engine.Orbs[0].Kind==OrbKind.Frost && window.View.OrbAt(window.View.OrbPosition(0))==0,label+"evoking the rightmost orb preserves logical order and the next clickable slot");
        }
        public static void Run(PetWindow window, string folder)
        {
            Directory.CreateDirectory(folder);
            int stage = 0;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(320) };
            timer.Tick += delegate {
                try
                {
                    window.Engine.Tick(window.Seconds);
                    if (stage == 0)
                    {
                        window.ToggleCards();
                        Check(window.View.MotionSampleCount == 3272, "Six native clips and seventeen card-art gestures expose 3272 aligned 60 Hz frames");
                        Check(HotCoreArt.Ready && HotCoreArt.SampleCount==window.View.MotionSampleCount,"Original red-hot core material follows all 3272 native poses with foreground occlusion");
                        Check(LearningHeadArt.Ready,"Card-palette circuit heads and native clipped traces preload for every action");
                        Check(LearningNeckArt.Ready,"Original neck and rear-pipe circuit material preloads with native foreground occlusion");
                        Check(window.View.NativeFramesReady,"Every native frame and its click mask is prepared before playback");
                        Check(window.View.InstalledModReady,"Pose-matched blue heads, moving clamps, cable clips, Buffer art and wall frames all preload successfully");
                        Check(InstalledModArt.MovingHeadReady,"Charging mounts move with both core and attack head poses instead of repeating the setup pose");
                        Check(NativeAnimation.CardClip("ice_lance")=="art-ice-spear" && NativeAnimation.CardClip("meteor_strike")=="cast" && NativeAnimation.CardClip("battery")=="process","Ice Lance uses the articulated spear grip; Meteor Strike and Charge Battery retain their action families");
                        Check(NativeAnimation.ReleaseDelay("claw")>.2 && NativeAnimation.ReleaseDelay("battery")>NativeAnimation.ReleaseDelay("claw"),"Card releases follow the original attack and core windup timings");
                        var picker = Picker(); Check(picker != null && picker.IsVisible, "Right-click entry opens a card picker");
                        picker.UpdateLayout(); Check(Buttons(picker).Count(b => b.Tag is CardInfo) == 12 && picker.FilteredCount == 36, "Picker paginates thirty-six cards into twelve actionable cards per page");
                        picker.SavePreview(Path.Combine(folder, "card-picker-integration.png"));
                        var next=Buttons(picker).Single(b=>AutomationProperties.GetName(b)=="下一页");
                        foreach(var point in new [] {new Point(.5,.5),new Point(.1,.1),new Point(.9,.1),new Point(.1,.9),new Point(.9,.9)})
                            Check(NavigationHit(picker,next,point.X,point.Y),"Next-page button receives real WPF hit tests at "+point);
                        for(int repeat=0;repeat<12;repeat++)
                        {
                            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            Check(picker.PageIndex==2,"Repeated actual next-button clicks reach the third page: "+repeat);
                            var prior=Buttons(picker).Single(b=>AutomationProperties.GetName(b)=="上一页");
                            prior.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));prior.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                        Check(picker.PageIndex==0,"Rapid repeated button navigation returns safely to page one");
                        picker.MovePage(1); Check(picker.PageIndex == 1 && picker.VisibleCards[0].Skill == "ball_lightning", "Second page exposes the first new card");
                        picker.SavePreview(Path.Combine(folder, "card-picker-page-2.png"));
                        picker.MovePage(50); Check(picker.PageIndex == 2 && picker.VisibleCards.Last().Skill == "overclock", "Last page and forward bounds retain every new card");
                        picker.SavePreview(Path.Combine(folder, "card-picker-page-3.png"));
                        picker.MovePage(-50); Check(picker.PageIndex == 0, "Backward pagination clamps safely");
                        picker.SetCategory("能力"); Check(picker.VisibleCards.All(c => c.Category == "能力") && picker.FilteredCount == 9, "Power filter contains the nine correct power cards");
                        picker.SetQuery("机器"); Check(picker.FilteredCount == 1 && picker.VisibleCards[0].Skill == "machine_learning", "Chinese name search combines with the category filter");
                        picker.SetQuery("no-card-exists"); Check(picker.FilteredCount == 0 && picker.VisibleCards.Length == 0, "An empty search result is safe");
                        picker.SetQuery(""); picker.SetCategory("全部");
                    }
                    else if (stage == 1)
                    {
                        var picker = Picker();
                        picker.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) { RoutedEvent = Mouse.PreviewMouseDownEvent });
                        Check(Picker() == null, "Right mouse button cancels the card picker");
                        Check(window.Engine.Interactions == 0 && window.Engine.Orbs.Count == 0, "Cancel does not play any card");
                    }
                    else if (stage == 2)
                    {
                        window.ToggleCards(); var picker = Picker(); Check(picker != null, "Picker reopens after cancellation");
                        bool reentered = false;
                        picker.Closing += delegate { reentered = true; picker.CancelSelection(); };
                        picker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(picker), Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.KeyDownEvent });
                        Check(Picker() == null, "Escape cancels card selection");
                        Check(reentered, "Reentrant cancellation during Closing is ignored safely");
                        picker.CancelSelection();
                        Check(window.IsVisible, "A second cancellation after Closed leaves the pet alive");
                    }
                    else if (stage == 3)
                    {
                        window.ToggleCards(); var picker = Picker(); picker.UpdateLayout();
                        var button = Buttons(picker).Single(b => AutomationProperties.GetName(b).StartsWith("电击。"));
                        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Check(Picker() == null, "Choosing a card closes the picker");
                        Check(window.Engine.Orbs.Count == 1 && window.Engine.Orbs[0].Kind == OrbKind.Lightning, "Choosing Zap executes its actual pet effect");
                        Check(window.Engine.LastSkill == "zap", "Selected card is remembered for Hologram");
                    }
                    else if (stage == 4)
                    {
                        window.ToggleCards(); var picker = Picker(); picker.UpdateLayout();
                        Buttons(picker).Single(b => AutomationProperties.GetName(b).StartsWith("冷静头脑。")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Check(window.Engine.Orbs.Count == 2 && window.Engine.Orbs[1].Kind == OrbKind.Frost, "Choosing Coolheaded adds the correct orb");
                        Check(!window.View.IsInteractive(new Point(308, 345)), "Removed ellipsis has no remaining invisible hit target");
                        Check(GameArt.Cards.All(c => GameArt.Bitmap("card-" + c.Asset).PixelWidth == 654), "All thirty-six full card images are embedded");
                        Check(GameArt.Cards.All(c => PetEngine.Supports(c.Skill)), "Every catalog card maps to an implemented interaction");
                        foreach (OrbKind kind in Enum.GetValues(typeof(OrbKind))) Check(GameArt.Bitmap("orb-" + GameArt.OrbName(kind)).PixelWidth == 100, "Original orb texture loads: " + kind);
                    }
                    else if (stage >= 5 && stage < 29)
                    {
                        // Render the previous action after its actual animation has advanced.
                        if (stage > 5) Capture(window, folder, GameArt.Cards[stage + 6].Skill);
                        var card = GameArt.Cards[stage + 7];
                        window.ToggleCards(); var picker = Picker(); picker.SetQuery(card.Skill); picker.UpdateLayout();
                        Buttons(picker).Single(b => b.Tag is CardInfo && ((CardInfo)b.Tag).Skill == card.Skill).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Check(window.IsVisible && Picker() == null && window.Engine.LastSkill == card.Skill, "New card can be selected, played and closed safely: " + card.Name);
                    }
                    else if (stage == 29)
                    {
                        Capture(window, folder, "overclock");
                        window.Engine.Animate("running-right",30);
                        double gaitStart = window.Engine.PoseSince;
                        window.Engine.Now += .225; Capture(window, folder, "run-right");
                        Check(window.View.IsInteractive(new Point(220,232)), "Original character remains clickable while dragging right");
                        window.Engine.Animate("running-left",30);
                        Check(window.Engine.PoseSince == gaitStart, "Running turn in the live view preserves its animation clock");
                        window.Engine.Now += .225; Capture(window, folder, "run-left");
                        Check(window.View.IsInteractive(new Point(190,232)), "Original character remains clickable while dragging left");
                        CheckOrbArc(window,false);CheckOrbArc(window,true);
                        window.Engine.Use("reboot");window.View.TakeEvents();
                        foreach(string skill in new [] {"capacitor","defragment","storm","thunder","loop","creative_ai","machine_learning","overclock","buffer","echo"}) window.Use(skill);
                        var powers=window.Engine.PowerStatuses();
                        Check(powers.Count==10 && powers.All(p=>GameArt.Bitmap("power-"+p.Key).PixelWidth==256),"All ten status icons load original game artwork");
                        Check(window.View.PowerAt(new Point(68,373))!=null && window.View.IsInteractive(new Point(68,373)),"The native icon row has a working tooltip and click target");
                        Check(window.View.PowerSummary.Contains("缓冲 2") && window.View.PowerSummary.Contains("回响形态 1"),"Accessible status includes actual power counts");
                        Capture(window,folder,"power-icons");
                        window.Engine.Use("reboot");window.View.TakeEvents();
                        Check(window.View.PowerAt(new Point(68,373))==null,"Removed status icons leave no invisible click targets");
                        results.Add("OK: " + results.Count + " UI integration checks passed");
                        File.WriteAllLines(Path.Combine(folder, "ui-tests.txt"), results);
                        timer.Stop(); StyleChecks.Run(window,folder);
                    }
                    stage++;
                }
                catch (Exception error)
                {
                    results.Add("FAIL " + error); File.WriteAllLines(Path.Combine(folder, "ui-tests.txt"), results);
                    timer.Stop(); Application.Current.Shutdown(1);
                }
            };
            timer.Start();
        }
        internal static void Capture(PetWindow window, string folder, string skill)
        {
            window.View.InvalidateVisual(); window.View.UpdateLayout();
            var bitmap = new RenderTargetBitmap(420, 390, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window.View);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(Path.Combine(folder, "action-" + skill + ".png"))) png.Save(file);
        }
    }
}
