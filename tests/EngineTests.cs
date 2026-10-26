using System;
using System.Linq;
using DefectCompanion;

public static class EngineTests
{
    private static int passed;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); passed++; Console.WriteLine("PASS " + message); }
    public static int Main()
    {
        try
        {
            var engine = new PetEngine();
            engine.Use("zap");
            Check(engine.Orbs.Count == 1 && engine.Orbs[0].Kind == OrbKind.Lightning, "Zap channels one persistent lightning orb");
            engine.Events.Clear(); engine.Use("dualcast");
            Check(engine.Orbs.Count == 0 && engine.Events.Count == 2 && engine.Events[1].Kind == "evoke-delay", "Dualcast consumes ONE orb and emits two timed pulses");
            engine.Events.Clear(); engine.Use("dualcast");
            Check(engine.Events.Count == 0 && engine.Orbs.Count == 0, "Empty Dualcast is safe");
            engine.Use("rainbow");
            Check(engine.Orbs.Select(o => o.Kind).SequenceEqual(new [] { OrbKind.Lightning, OrbKind.Frost, OrbKind.Dark }), "Rainbow preserves channel order");
            engine.Events.Clear(); engine.Use("dualcast");
            Check(engine.Orbs.Count == 2 && engine.Orbs[0].Kind == OrbKind.Frost && engine.Events[0].Orb == OrbKind.Lightning, "Dualcast evokes oldest orb, displayed at the rightmost slot");
            Check(engine.Events[1].Delay - engine.Events[0].Delay == .24, "Double evoke pulses have a distinct 240ms separation");
            engine.Use("reboot"); engine.Use("rainbow");
            engine.Events.Clear(); engine.Use("plasma");
            Check(engine.Orbs.Count == 3 && engine.Orbs[0].Kind == OrbKind.Frost && engine.Events[0].Kind == "evoke", "Fourth channel evokes oldest and preserves three-slot capacity");
            engine.Use("reboot"); engine.Charge(.3);
            Check(engine.Energy == 0, "Short press cannot charge");
            engine.Charge(.7); engine.Charge(2); engine.Charge(20);
            Check(engine.Energy == 3, "Charge gain is capped at three");
            engine.Use("zap"); engine.Events.Clear(); engine.Use("dualcast");
            Check(engine.Energy == 0 && engine.Events.Count == 2 && engine.Events.All(e => e.Strength == 4), "Both Dualcast pulses retain charge strength; energy consumed once");
            engine.Use("reboot"); engine.Use("dark"); engine.Events.Clear(); engine.Tick(100); engine.TapOrb(0);
            Check(engine.Events[0].Strength == 4 && engine.Orbs.Count == 0, "Dark orb accumulates bounded strength over time");
            engine.Use("plasma"); engine.TapOrb(0);
            Check(engine.Energy == 1, "Plasma evoke restores energy");
            engine.Charge(2); engine.Use("plasma"); engine.TapOrb(0);
            Check(engine.Energy == 1, "Charged plasma spends stored charge before restoring one energy");
            engine.Use("reboot"); engine.Use("echo"); engine.Use("frost");
            Check(engine.Orbs.Count == 2 && !engine.EchoReady && engine.Orbs.All(o => o.Kind == OrbKind.Frost), "Echo repeats the next skill once");
            engine.Use("hologram");
            Check(engine.Orbs.Count == 3 && engine.LastSkill == "frost", "Hologram replays remembered skill without remembering itself");
            engine.Use("reboot"); engine.Use("zap"); engine.Use("echo"); engine.Use("hologram");
            Check(engine.Orbs.Count == 3 && !engine.EchoReady && engine.LastSkill == "zap", "Echo can duplicate Hologram without recursive replay");
            engine.Use("reboot"); engine.Use("hologram");
            Check(engine.LastSkill == null && engine.Orbs.Count == 0 && engine.Energy == 0 && !engine.EchoReady, "Reboot clears combat resources and replay history");
            engine.Use("zap"); engine.TapOrb(9); engine.TapOrb(-1);
            Check(engine.Orbs.Count == 1, "Stale orb input cannot remove a different slot");
            engine.Use("rocket"); engine.Tick(200);
            Check(engine.Pose == "idle", "Finite actions return to idle");
            engine.Use("reboot"); engine.Use("battery");
            Check(engine.Energy == 2 && engine.LastSkill == "battery", "Charge Battery can be selected as a remembered card");
            engine.Use("hologram");
            Check(engine.Energy == 3, "Hologram can replay the charge card without exceeding energy capacity");
            engine.Use("reboot"); engine.Use("capacitor"); engine.Use("capacitor"); engine.Use("chill");
            Check(engine.Capacity == 5 && engine.Orbs.Count == 5 && engine.Orbs.All(o => o.Kind == OrbKind.Frost), "Capacitor caps at five slots and Chill fills every empty slot");
            engine.Use("zap");
            Check(engine.Orbs.Count == 5 && engine.Orbs.Last().Kind == OrbKind.Lightning, "Sixth orb evokes the oldest without exceeding expanded capacity");
            engine.Use("reboot"); engine.Use("ball_lightning"); engine.Use("cold_snap");
            Check(engine.Orbs.Select(o => o.Kind).SequenceEqual(new [] { OrbKind.Lightning, OrbKind.Frost }), "Ball Lightning and Cold Snap create the corresponding persistent orbs");
            engine.Events.Clear(); engine.Use("tesla_coil");
            Check(engine.Orbs.Count == 2 && engine.Events.Count(e => e.Kind == "passive") == 1, "Tesla Coil triggers only lightning and retains every orb");
            engine.Use("loop"); double loopStart = engine.Now; engine.Events.Clear(); engine.Tick(loopStart + 5.9);
            Check(engine.Events.Count == 0, "Loop waits for its full six second interval");
            engine.Tick(loopStart + 6); engine.Tick(loopStart + 1000);
            Check(engine.Events.Count(e => e.Kind == "passive") == 2 && engine.Orbs.Count == 2, "Loop retains its orb and emits one bounded pulse after a long pause");
            engine.Use("reboot"); engine.Use("storm"); engine.Use("defragment");
            Check(engine.Focus == 1 && engine.Orbs.Count == 1, "Storm channels lightning after a subsequent power card");
            engine.Use("thunder"); engine.Events.Clear(); engine.TapOrb(0);
            Check(engine.Events.Any(e => e.Kind == "thunder") && engine.Events.First().Strength == 2, "Focus strengthens evocation and Thunder appends a distinct lightning strike");
            for (int i = 0; i < 20; i++) engine.Use("defragment");
            Check(engine.Focus == 5 && engine.Orbs.Count <= engine.Capacity, "Repeated power cards keep Focus and orb capacity bounded");
            engine.Use("reboot"); engine.Use("buffer"); engine.Use("leap"); engine.Use("leap"); engine.Use("leap");
            Check(engine.ShieldHits == 0 && engine.Events.Count(e => e.Kind == "shield") == 4, "Buffer protects exactly the next three card interactions");
            engine.Use("buffer"); engine.Tick(engine.Now + 16);
            Check(engine.ShieldHits == 0, "Unused buffer shield expires");
            engine.Use("reboot"); engine.Use("glacier");
            Check(engine.Orbs.Count == 2 && engine.ShieldHits == 3, "Glacier channels two frost orbs and creates a shield");
            engine.Use("reboot"); engine.Use("claw"); int firstClaw = engine.ClawCombo;
            for (int i = 0; i < 25; i++) engine.Use("claw");
            Check(firstClaw == 1 && engine.ClawCombo == 10, "Claw grows with repeated casts and caps its visual intensity");
            engine.Use("reboot"); engine.Use("rainbow"); engine.Events.Clear(); engine.Use("barrage");
            Check(engine.Events.Count(e => e.Kind == "barrage") == 3 && engine.Orbs.Count == 3, "Barrage fires once per orb and retains its ammunition orbs");
            engine.Use("reboot"); engine.Use("meteor_strike");
            Check(engine.Orbs.Count == 3 && engine.Orbs.All(o => o.Kind == OrbKind.Plasma) && engine.Events.Count(e => e.Kind == "meteor") == 1, "Meteor Strike uses one native-style impact and retains three plasma orbs");
            engine.Use("reboot"); engine.Use("ice_lance");
            Check(engine.Orbs.Count == 2 && engine.Events.Any(e => e.Kind == "lance"), "Ice Lance emits its own projectile and two frost orbs");
            engine.Use("reboot"); engine.Use("double_energy");
            Check(engine.Energy == 1, "Double Energy gives one energy when empty");
            engine.Use("turbo"); engine.Use("double_energy");
            Check(engine.Energy == 3, "Turbo and Double Energy never exceed three stored energy");
            engine.Use("tempest");
            Check(engine.Energy == 0 && engine.Orbs.Count == 3 && engine.Orbs.All(o => o.Kind == OrbKind.Lightning), "Tempest consumes stored energy and channels its lightning sequence");
            engine.Use("reboot"); engine.Use("machine_learning"); engine.Use("leap"); engine.Use("leap"); engine.Use("leap");
            Check(engine.Focus == 0, "Machine Learning does not count its activation card");
            engine.Use("leap");
            Check(engine.Focus == 1, "Machine Learning increases Focus on the fourth subsequent card");
            engine.Use("overclock"); double overclockStart = engine.Now;
            Check(engine.Energy == 3 && engine.IsOverclocked, "Overclock fills energy and activates animation acceleration");
            engine.Tick(overclockStart + 5);
            Check(!engine.IsOverclocked, "Overclock ends after five seconds");
            bool safeAI = true;
            for (int i = 0; i < 100; i++) { engine.Use("creative_ai"); safeAI &= PetEngine.Supports(engine.GeneratedSkill) && engine.GeneratedSkill != "creative_ai" && engine.GeneratedSkill != "reboot"; engine.Events.Clear(); }
            Check(safeAI, "One hundred Creative AI draws all select safe nonrecursive playable cards");
            engine.Use("capacitor"); engine.Use("storm"); engine.Use("thunder"); engine.Use("loop"); engine.Use("buffer"); engine.Use("overclock"); engine.Use("reboot");
            Check(engine.Capacity == 3 && engine.Focus == 0 && !engine.Storm && !engine.Thunder && !engine.Loop && !engine.Learning && !engine.IsOverclocked && engine.ShieldHits == 0 && engine.ClawCombo == 0 && engine.GeneratedSkill == null, "Reboot resets every expanded ability and status");
            var pacer = new FramePacer(); int displayFrames = 0; bool noDuplicates = true;
            for (int i = 0; i < 120; i++) { double stamp = i / 120.0; if (pacer.Due(stamp)) displayFrames++; noDuplicates &= !pacer.Due(stamp); }
            Check(noDuplicates, "Repeated composition timestamps never double-update across 120 samples");
            Check(displayFrames == 60, "A 120 Hz compositor produces exactly sixty application frames per second");
            pacer.Reset(); displayFrames = 0;
            for (int i = 0; i < 144; i++) if (pacer.Due(i / 144.0)) displayFrames++;
            Check(displayFrames == 60, "A 144 Hz compositor keeps the sixty-frame limit without timing drift");
            Check(pacer.Due(30) && !pacer.Due(30), "Returning after a long pause produces one frame and no catch-up burst");
            Check(AnimationTiming.Sample(0,6,.125,false) == 4, "Idle plays an intermediate motion sample halfway between original poses");
            Check(AnimationTiming.Sample(0,6,1.5,false) == 0, "Idle preserves the original 1.5 second loop duration");
            Check(AnimationTiming.Sample(3,4,10,false) == 24 && AnimationTiming.Sample(4,5,10,false) == 32, "Finite wave and jump animations hold the final original keyframe");
            Check(AnimationTiming.Sample(7,6,.25,true) == 24, "Overclock still accelerates interpolated motion by the existing 1.5 factor");
            var direction = new DragDirection(); direction.Begin(100,1);
            Check(direction.Move(150) == 1 && direction.Move(142) == 1, "Small pointer reversal preserves running direction");
            Check(direction.Move(134) == -1, "A deliberate reversal turns left even while right of the original press point");
            Check(direction.Move(138) == -1, "Tiny pointer jitter cannot immediately flip the character back");
            engine.Use("reboot"); engine.Now = 500; engine.Animate("running-right",30);
            double gaitStart = engine.PoseSince; engine.Now = 500.3; engine.Animate("running-left",30);
            Check(engine.PoseSince == gaitStart && engine.Pose == "running-left", "Turning preserves the running gait phase");
            engine.Now = 500.5; engine.Animate("running-left",30);
            Check(engine.PoseSince == gaitStart, "Continuous drag updates do not restart the gait");
            engine.Use("leap"); engine.Now += .2; engine.Use("leap");
            Check(engine.PoseSince == engine.Now, "A newly played card restarts its own action clock");
            Check(AnimationTiming.Sample(1,8,.9,false) == 0 && AnimationTiming.Sample(2,8,.9,false) == 0, "Both run directions share the same continuous 0.9 second loop");
            engine.Tick(600); Check(engine.Pose == "idle" && engine.PoseSince == 600, "Completing an action starts a fresh idle timeline");
            var powers=new PetEngine();
            Check(powers.PowerStatuses().Count==0,"A new pet has no inactive or decorative power icons");
            foreach(string card in new [] {"capacitor","defragment","storm","thunder","loop","creative_ai","machine_learning","overclock","buffer","echo"}) powers.Use(card);
            var statuses=powers.PowerStatuses();
            Check(statuses.Count==10 && statuses.Select(p=>p.Key).Distinct().Count()==10 && statuses.All(p=>p.Amount>0),"Every active power and timed status has one distinct icon with a positive amount");
            Check(statuses.Single(p=>p.Key=="capacity").Amount==2 && statuses.Single(p=>p.Key=="buffer").Amount==2,"Capacity and Buffer badges reflect actual extra slots and remaining charges");
            powers.Use("leap");
            Check(!powers.PowerStatuses().Any(p=>p.Key=="echo_form") && powers.PowerStatuses().Single(p=>p.Key=="buffer").Amount==1,"Echo icon is consumed and Buffer count falls after playing a card");
            powers.Tick(6);
            Check(!powers.PowerStatuses().Any(p=>p.Key=="energy_next_turn"),"Overclock icon disappears when its countdown expires");
            powers.Use("reboot");Check(powers.PowerStatuses().Count==0 && !powers.CreativeAI,"Reboot removes every active power icon including Creative AI");
            var shieldOnly=new PetEngine();shieldOnly.Use("buffer");shieldOnly.Tick(16);
            Check(!shieldOnly.PowerStatuses().Any(p=>p.Key=="buffer"),"An expired unused shield leaves no stale Buffer icon");
            var aiPower=new PetEngine();aiPower.Tick(100);aiPower.Use("creative_ai");aiPower.Events.Clear();aiPower.Tick(111.9);
            Check(!aiPower.Events.Any(p=>p.Kind=="ai-card"),"Creative AI waits twelve seconds before its automatic cast");
            aiPower.Tick(112);aiPower.Tick(1000);
            Check(aiPower.Events.Count(p=>p.Kind=="ai-card")==2 && aiPower.Events.Where(p=>p.Kind=="ai-card").All(p=>PetEngine.Supports(p.Card)),"Creative AI generates playable card events and never catches up with an unbounded burst");
            foreach(string skill in new [] {"claw","ice_lance","hyperbeam","battery","buffer","hologram","dualcast","meteor_strike","turbo","dark","echo","rocket","tempest","glacier","rainbow","thunder","storm","ball_lightning","overclock"})
            {
                var score=CardAnimation.For(skill);double prior=-1;bool continuous=true;
                for(int i=0;i<=600;i++)
                {double pose=score.PoseTime(score.Duration*i/600);if(Double.IsNaN(pose) || pose<prior-1e-8 || pose>score.EndPose+1e-8 || (prior>=0 && pose-prior>.04))continuous=false;prior=pose;}
                Check(continuous,"Card motion stays monotonic and continuous through release and recovery: "+skill);
            }
            var ice=CardAnimation.For("ice_lance");var slash=CardAnimation.For("claw");var beam=CardAnimation.For("hyperbeam");
            var turboScore=CardAnimation.For("turbo");
            Check(Math.Abs(turboScore.Duration-47.0/30)<1e-8 && Math.Abs(turboScore.PoseTime(.81)-.81)<1e-8,"Turbo's new dash preserves the original 47/30-second visual clock");
            Check(ice.HoldUntil-ice.Release>slash.HoldUntil-slash.Release && beam.HoldUntil>ice.HoldUntil,"Ice Lance holds its arm longer than Claw, while Hyperbeam sustains its core pose");
            Check(Math.Abs(ice.PoseTime(ice.Duration)-ice.EndPose)<1e-8 && Math.Abs(beam.PoseTime(beam.Duration)-beam.EndPose)<1e-8,"Finite card timelines reach their native recovery pose before returning to idle");
            var attachments=new ModPlayback();attachments.Charge.Cast(10);
            Check(new [] {"dark","echo","rocket","hyperbeam","dualcast","tempest","glacier","rainbow","thunder","storm","turbo","claw","ball_lightning","ice_lance","overclock"}.Select(s=>CardAnimation.For(s).Clip).Distinct().Count()==15,"The fifteen card illustrations have distinct articulated gesture clips");
            Check(CardAnimation.For("rocket").Release<.25 && CardAnimation.For("rocket").Duration>1.3,"Rocket Punch has a short pressure windup and preserves impact and retrieval time");
            var thunderArt=new PetEngine();thunderArt.Use("thunder");
            Check(thunderArt.Thunder && thunderArt.Events.Any(e=>e.Kind=="thunder-ready") && !thunderArt.Events.Any(e=>e.Kind=="thunder"),"Thunder's initial power aura is separate from actual evocation lightning strikes");
            var stormArt=new PetEngine();stormArt.Use("battery");stormArt.Events.Clear();stormArt.Use("tempest");
            Check(stormArt.Energy==0 && stormArt.Orbs.Count==3 && stormArt.Orbs.All(o=>o.Kind==OrbKind.Lightning) && stormArt.Events.Count(e=>e.Kind=="thunder" && e.Card=="tempest")==3,"Tempest consumes the stored energy and retains three actual channels and thunder strikes");
            var glacierArt=new PetEngine();glacierArt.Use("glacier");
            Check(glacierArt.Orbs.Count==2 && glacierArt.Orbs.All(o=>o.Kind==OrbKind.Frost) && glacierArt.ShieldHits==3,"Glacier retains its two Frost orbs and three defensive uses");
            var rainbowArt=new PetEngine();rainbowArt.Use("rainbow");
            Check(rainbowArt.Orbs.Select(o=>o.Kind).SequenceEqual(new [] {OrbKind.Lightning,OrbKind.Frost,OrbKind.Dark}) && Math.Abs(rainbowArt.Orbs[2].Born-.28)<1e-8,"Rainbow's colored sweep preserves the three real orb types and logical channel timing");
            foreach(var kind in new [] {OrbKind.Lightning,OrbKind.Frost,OrbKind.Dark,OrbKind.Plasma})
            {
                var release=new PetEngine();release.Channel(kind);release.Use("dualcast");var pulses=release.Events.Where(e=>e.Kind=="evoke" || e.Kind=="evoke-delay").ToArray();
                Check(pulses.Length==2 && pulses.All(e=>e.Orb==kind && e.Card=="dualcast") && Math.Abs(pulses[1].Delay-pulses[0].Delay-.24)<1e-8,"Card-art Dualcast retains the real orb type and 0.24-second repeat: "+kind);
            }
            var darkArt=new PetEngine();darkArt.Channel(OrbKind.Lightning);darkArt.Channel(OrbKind.Frost);darkArt.Channel(OrbKind.Plasma);darkArt.Use("echo");darkArt.Events.Clear();darkArt.Use("dark");
            var darkChannels=darkArt.Events.Where(e=>e.Kind=="channel" && e.Card=="dark").ToArray();
            Check(darkChannels.Length==2 && darkChannels[0].Created!=darkChannels[1].Created && darkChannels.All(e=>darkArt.Orbs.Contains(e.Created)),"Full slots and Echo retain distinct created-orb identities for the two dark-sphere animations");
            Check(attachments.Charge.Alpha(10.3)>.999 && attachments.Charge.Alpha(12.29)>.999 && attachments.Charge.Alpha(12.9)==0,"Installed Charge Battery keeps a full two-second hold between its 0.3/0.6-second fades");
            double fading=attachments.Charge.Alpha(12.6);attachments.Charge.Cast(12.6);
            Check(Math.Abs(attachments.Charge.Alpha(12.6)-fading)<1e-8 && attachments.Charge.CreatedAt==10,"Repeated charging restores opacity continuously without restarting cable motion");
            attachments.Event(new PetEvent("focus",OrbKind.Frost,1),20);attachments.Event(new PetEvent("focus",OrbKind.Frost,2),20.4);attachments.Update(21.81,3);
            Check(attachments.Walls.Count==1 && attachments.Walls[0]==20.4,"Defragment rounds overlap and each completes its own 1.8-second lifetime");
            attachments.Event(new PetEvent("channel",OrbKind.Frost,1){Card="frost"},30);
            attachments.Event(new PetEvent("channel",OrbKind.Frost,1){Card="frost",Delay=.28},30);
            Check(attachments.Ice.Alpha(30.15)>0,"A delayed Echo refresh cannot hide the first Coolheaded ice layer");
            attachments.Update(30.29,3);Check(attachments.Ice.Alpha(30.4)>.8,"The queued Coolheaded refresh takes effect at the actual repeated resolution");
            attachments.Event(new PetEvent("shield",OrbKind.Frost,3){Card="buffer"},40);attachments.Event(new PetEvent("shield",OrbKind.Frost,3){Card="zap"},40.5);
            Check(attachments.BufferActive && attachments.BufferHitAt==40.5,"Buffer's sign persists through nonfinal consumption");
            attachments.Event(new PetEvent("shield",OrbKind.Frost,1){Card="zap"},41);
            Check(!attachments.BufferActive && attachments.BufferBreakAt==41,"The final Buffer consumption breaks the sign exactly once");
            attachments.Event(new PetEvent("shield",OrbKind.Frost,3){Card="buffer"},41.1);
            Check(attachments.BufferActive && attachments.BufferBreakAt==41,"A new Buffer sign can coexist with the old debris tail");
            Check(attachments.BufferDebris.Count==1,"A reacquired Buffer retains the independently expiring old sign fragments");
            attachments.Event(new PetEvent("hologram",OrbKind.Frost,1),50);attachments.Card("zap",50.5);
            Check(attachments.HologramOpen(51)>.99 && attachments.HologramOpen(53)==0,"Hologram's three-second clock survives unrelated new card animations");
            attachments.Event(new PetEvent("charge",OrbKind.Plasma,2){Card="turbo"},60);attachments.Card("claw",60.2);
            Check(attachments.TurboAt==60 && !attachments.TurboCore,"A new body action interrupts Turbo's core while retaining the original visual timestamp");
            var provenance=new PetEngine();provenance.Use("frost");Check(provenance.Events.Any(e=>e.Kind=="channel" && e.Card=="frost"),"A real Coolheaded resolution carries its card identity into presentation");
            provenance.Events.Clear();provenance.Channel(OrbKind.Frost);Check(provenance.Events.Last().Card==null,"An idle frost channel does not pretend to be a Coolheaded card play");
            attachments.Reset();Check(attachments.Walls.Count==0 && !attachments.BufferActive && attachments.Charge.Alpha(61)==0,"Reboot clears independent installed-mod attachments and queued refreshes");
            Console.WriteLine("TOTAL " + passed + " checks passed"); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
