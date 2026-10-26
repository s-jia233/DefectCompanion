using System;
using System.Collections.Generic;

namespace DefectCompanion
{
    public enum OrbKind { Lightning, Frost, Dark, Plasma }
    public sealed class Orb
    {
        public OrbKind Kind;
        public double Born;
        public Orb(OrbKind kind, double now) { Kind = kind; Born = now; }
    }
    public sealed class PetEvent
    {
        public string Kind;
        public OrbKind Orb;
        public int Strength;
        public int Slot = -1;
        public double Delay;
        public string Card;
        public Orb Created;
        public PetEvent(string kind, OrbKind orb, int strength) { Kind = kind; Orb = orb; Strength = strength; }
    }
    // The interaction rules are independent of the window and animation code.
    public sealed class PetEngine
    {
        public readonly List<Orb> Orbs = new List<Orb>();
        public readonly List<PetEvent> Events = new List<PetEvent>();
        public int Energy { get; private set; }
        public bool EchoReady { get; private set; }
        public int Interactions { get; private set; }
        public string LastSkill { get; private set; }
        public double Now;
        public string Message = "点我生雷球 · 按住充电 · 右键选技能";
        public string Pose = "idle";
        public double PoseSince;
        public double PoseUntil;
        public double MessageUntil = 7;
        private double visualDelay;
        private string visualCard;
        private bool applying;
        public int Capacity { get; private set; }
        public PetEngine() { Capacity = 3; }
        public int Focus { get; private set; }
        public int ClawCombo { get; private set; }
        public int ShieldHits { get; private set; }
        public bool Storm { get; private set; }
        public bool Thunder { get; private set; }
        public bool Loop { get; private set; }
        public bool Learning { get; private set; }
        public bool CreativeAI { get; private set; }
        public double OverclockUntil { get; private set; }
        public double ShieldUntil { get; private set; }
        public string GeneratedSkill { get; private set; }
        private double nextLoop;
        private double nextAI;
        private int learnedCards;
        private readonly Random random = new Random();
        public bool IsOverclocked { get { return Now < OverclockUntil; } }
        public List<PowerStatus> PowerStatuses()
        {
            var states = new List<PowerStatus>();
            if (Focus > 0) states.Add(new PowerStatus("focus","集中",Focus,"增强充能球的激发强度，最多五点。"));
            if (Capacity > 3) states.Add(new PowerStatus("capacity","扩容",Capacity-3,"额外球槽 " + (Capacity-3) + " 个；当前共 " + Capacity + " 个球槽。"));
            if (Storm) states.Add(new PowerStatus("storm","雷暴",1,"之后使用能力牌时，额外生成一个闪电球。"));
            if (Thunder) states.Add(new PowerStatus("thunder","雷霆",1,"激发闪电球时追加一道雷击。"));
            if (Loop) states.Add(new PowerStatus("loop","循环",1,"每六秒触发最右球一次，并保留它。"));
            if (ShieldHits > 0 && Now < ShieldUntil) states.Add(new PowerStatus("buffer","缓冲",ShieldHits,"剩余 " + ShieldHits + " 次护盾涟漪；" + (int)System.Math.Ceiling(ShieldUntil-Now) + " 秒后消散。"));
            if (EchoReady) states.Add(new PowerStatus("echo_form","回响形态",1,"下一张牌额外施放一次，随后移除此图标。"));
            if (CreativeAI) states.Add(new PowerStatus("creative_ai","创造性 AI",1,"每十二秒自动抽取并施放一张卡牌。"));
            if (Learning) states.Add(new PowerStatus("machine_learning","机器学习",1,"再使用 " + (4-learnedCards) + " 张卡牌，获得一点集中。"));
            if (IsOverclocked) states.Add(new PowerStatus("energy_next_turn","超频",(int)System.Math.Ceiling(OverclockUntil-Now),"数字是剩余秒数；核心正在加速运转。"));
            return states;
        }
        public static bool IsPower(string skill)
        { return skill == "echo" || skill == "storm" || skill == "thunder" || skill == "capacitor" || skill == "defragment" || skill == "loop" || skill == "buffer" || skill == "creative_ai" || skill == "machine_learning"; }
        public static bool Supports(string skill) { return Known(skill) || skill == "echo" || skill == "hologram" || skill == "reboot"; }
        private void Emit(string kind, OrbKind orb, int strength, double delay = 0, int slot = -1)
        { Events.Add(new PetEvent(kind, orb, strength) { Delay = visualDelay + delay, Slot = slot, Card=visualCard }); }
        private void Passive(int index, double delay)
        {
            Orb orb = Orbs[index];
            Emit("passive", orb.Kind, 1 + Focus, delay, index);
            if (orb.Kind == OrbKind.Dark) orb.Born -= 4;
            if (orb.Kind == OrbKind.Plasma) Energy = Math.Min(3, Energy + 1);
        }
        private void BeforeCard(string skill)
        {
            PoseSince = Now;
            if (ShieldHits > 0 && Now < ShieldUntil && skill != "buffer")
            { ShieldHits--; Emit("shield", OrbKind.Frost, ShieldHits + 1); }
        }
        private void AfterCard(string skill)
        {
            if (Learning && skill != "machine_learning" && ++learnedCards >= 4)
            { learnedCards = 0; Focus = Math.Min(5, Focus + 1); Emit("learn", OrbKind.Frost, Focus); Say("机器学习 · 集中 " + Focus + "/5", 2.5); }
        }

        public void Tick(double now)
        {
            Now = now;
            if (now >= PoseUntil && Pose != "idle") { Pose = "idle"; PoseSince = now; }
            if (now >= ShieldUntil) ShieldHits = 0;
            if (Loop && now >= nextLoop)
            {
                nextLoop = now + 6; // One pulse after a long pause, never an unbounded catch-up burst.
                if (Orbs.Count > 0) Passive(0, 0);
            }
            if (CreativeAI && now >= nextAI)
            {
                nextAI = now + 12;
                string generated = DrawAICard(); Apply(generated);
                Events.Add(new PetEvent("ai-card",OrbKind.Frost,1) { Card = generated });
                Say("创造性 AI · 为你施放了一张新牌",3);
            }
        }
        public void Say(string message, double duration)
        { Message = message; MessageUntil = Now + duration; }
        public void Animate(string pose, double duration)
        {
            bool turning = (Pose == "running-right" || Pose == "running-left") && (pose == "running-right" || pose == "running-left");
            if (Pose != pose && !turning) PoseSince = Now;
            Pose = pose; PoseUntil = Now + duration;
        }
        public void Charge(double seconds)
        {
            if (seconds < .55) return;
            int gained = seconds >= 1.8 ? 2 : 1;
            Energy = Math.Min(3, Energy + gained);
            Animate("running", 1.5);
            Say("充电完成 · 能量 " + Energy + "/3\n下一次释放会更耀眼", 3);
            Events.Add(new PetEvent("charge", OrbKind.Plasma, Energy) {Card="battery",Delay=visualDelay});
            Interactions++;
        }
        public void Channel(OrbKind kind)
        {
            if (Orbs.Count >= Capacity) Evoke(0, 1);
            var created=new Orb(kind,Now+visualDelay);Orbs.Add(created);
            Events.Add(new PetEvent("channel", kind, 1) { Slot = Orbs.Count - 1, Delay = visualDelay,Card=applying?visualCard:null,Created=created });
        }
        public bool Evoke(int index, int times)
        {
            if (index < 0 || index >= Orbs.Count) return false;
            Orb orb = Orbs[index];
            Orbs.RemoveAt(index);
            int power = 1 + Energy + Focus;
            if (orb.Kind == OrbKind.Dark) power += Math.Max(0, Math.Min(3, (int)((Now - orb.Born) / 8)));
            for (int i = 0; i < times; i++) Events.Add(new PetEvent(i == 0 ? "evoke" : "evoke-delay", orb.Kind, power) { Slot = index, Delay = visualDelay + i * .24,Card=applying?visualCard:null });
            Energy = orb.Kind == OrbKind.Plasma ? 1 : 0;
            if (Thunder && orb.Kind == OrbKind.Lightning) Emit("thunder", OrbKind.Lightning, power + 1, .32);
            return true;
        }
        public void Use(string skill)
        {
            visualCard=skill;
            visualDelay = 0;
            GeneratedSkill = null;
            if (!Supports(skill)) return;
            BeforeCard(skill);
            if (skill == "echo")
            {
                EchoReady = true;
                Say("回响形态已就绪\n下一招会再施放一次", 4);
                Animate("review", 1.6);
                Events.Add(new PetEvent("echo-ready", OrbKind.Dark, 1));
                Interactions++;
                if (Storm) Channel(OrbKind.Lightning);
                AfterCard(skill);
                return;
            }
            if (skill == "hologram")
            {
                if (LastSkill == null) { Say("还没有可以重演的招式\n先点我，生成一颗雷球吧", 3); return; }
                string previous = LastSkill;
                bool repeatReplay = EchoReady;
                EchoReady = false;
                Events.Add(new PetEvent("hologram", OrbKind.Frost, 1));
                Apply(previous);
                if (repeatReplay) { visualDelay = .28; Apply(previous); visualDelay = 0; Events.Add(new PetEvent("echo", OrbKind.Dark, 1) { Delay = .28 }); }
                Say(repeatReplay ? "全息影像 × 回响 · 重演两次" : "全息影像 · 重演上一招", 3);
                Interactions++;
                AfterCard(skill);
                return;
            }
            if (skill == "reboot")
            {
                Orbs.Clear(); Energy = 0; EchoReady = false; LastSkill = null;
                Capacity = 3; Focus = 0; ClawCombo = 0; ShieldHits = 0;
                Storm = Thunder = Loop = Learning = CreativeAI = false;
                OverclockUntil = ShieldUntil = nextLoop = nextAI = 0; learnedCards = 0;
                Events.Clear();
                Animate("failed", 1.25); Say("重启完成。又是新的一轮！", 3);
                Events.Add(new PetEvent("reboot", OrbKind.Plasma, 1));
                Interactions++;
                return;
            }
            if (!Known(skill)) return;
            bool echo = EchoReady;
            EchoReady = false;
            Apply(skill);
            LastSkill = skill;
            Interactions++;
            if (echo)
            {
                // Reapply the card action, preserving the same orb for Dualcast's two pulses.
                visualDelay = .28; Apply(skill); visualDelay = 0;
                Events.Add(new PetEvent("echo", OrbKind.Dark, 1) { Delay = .28 });
                Say("回响！这一招又施放了一次", 3);
            }
            AfterCard(skill);
        }
        public void TapOrb(int index)
        {
            if (Evoke(index, 1))
            {
                Interactions++; Animate("waving", 1);
                Say("充能球已释放", 2);
            }
        }
        private static bool Known(string skill)
        {
            return ("|zap|frost|dark|plasma|dualcast|rainbow|leap|rocket|battery|ball_lightning|cold_snap|glacier|chill|tempest|storm|thunder|tesla_coil|capacitor|defragment|loop|buffer|claw|barrage|sweeping_beam|hyperbeam|meteor_strike|ice_lance|ftl|turbo|double_energy|creative_ai|machine_learning|overclock|").Contains("|" + skill + "|");
        }
        private string DrawAICard()
        {
            string[] choices = { "zap", "frost", "dark", "plasma", "battery", "glacier", "ball_lightning", "defragment" };
            return choices[random.Next(choices.Length)];
        }
        private void Apply(string skill)
        {
            string previousCard=visualCard;bool previousApplying=applying;
            visualCard=skill;applying=true;
            try {
            switch (skill)
            {
                case "zap": Channel(OrbKind.Lightning); Animate("waving", .9); Say("电击 · 生成一颗雷球", 2.2); break;
                case "frost": Channel(OrbKind.Frost); Animate("waiting", 1.4); Say("冷静头脑 · 冰霜球陪你降降温", 3); break;
                case "dark": Channel(OrbKind.Dark); Animate("running", 1.6); Say("漆黑 · 留得越久，释放越强", 3); break;
                case "plasma": Channel(OrbKind.Plasma); Animate("review", 1.3); Say("聚变 · 释放等离子球可补充能量", 3); break;
                case "rainbow":
                    double offset = visualDelay;
                    Channel(OrbKind.Lightning); visualDelay = offset + .14; Channel(OrbKind.Frost); visualDelay = offset + .28; Channel(OrbKind.Dark); visualDelay = offset;
                    Animate("waiting", 1.4); Say("彩虹 · 闪电、冰霜、黑暗齐聚", 3); break;
                case "battery": Charge(1.8); break;
                case "dualcast":
                    if (Orbs.Count == 0) { Say("身边还没有充能球\n点一下我，先召唤一颗吧", 3); break; }
                    Evoke(0, 2); Animate("running", 1.1); Say("双重释放！", 2); break;
                case "leap": Animate("jumping", 1.1); Say("飞跃 · 接住我！", 2); Events.Add(new PetEvent("leap", OrbKind.Frost, 1)); break;
                case "rocket": Animate("waving", .95); Say("火箭飞拳 · 和你碰个拳", 2.4); Events.Add(new PetEvent("rocket", OrbKind.Lightning, 1 + Energy) { Delay = visualDelay }); Energy = 0; break;
                case "ball_lightning": Channel(OrbKind.Lightning); Animate("waving", 1); Emit("ball", OrbKind.Lightning, 1 + Focus); Say("球状闪电 · 接球！", 2); break;
                case "cold_snap": Channel(OrbKind.Frost); Animate("waving", .9); Emit("ice-slash", OrbKind.Frost, 1); Say("寒流 · 清醒一下", 2); break;
                case "glacier":
                    Channel(OrbKind.Frost); visualDelay += .15; Channel(OrbKind.Frost); visualDelay -= .15;
                    ShieldHits = 3; ShieldUntil = Now + 15; Animate("waiting", 1.4); Emit("glacier", OrbKind.Frost, 3); Say("冰川 · 冰晶护盾展开", 2.5); break;
                case "chill":
                    int empty = Capacity - Orbs.Count;
                    for (int i = 0; i < empty; i++) { visualDelay += .08; Channel(OrbKind.Frost); }
                    visualDelay -= empty * .08; Animate("waiting", 1.4); Emit("snow", OrbKind.Frost, 2); Say("冰寒 · 填满空球槽", 2.5); break;
                case "tempest":
                    int bolts = Math.Max(2, Math.Min(4, Energy + 1)); Energy = 0;
                    for (int i = 0; i < bolts; i++) { Channel(OrbKind.Lightning); Emit("thunder", OrbKind.Lightning, 2, i * .15); visualDelay += .1; }
                    visualDelay -= bolts * .1; Animate("running", 1.5); Say("暴风雨 · 雷声滚滚", 2.5); break;
                case "storm": Storm = true; Emit("storm", OrbKind.Lightning, 1); Animate("review", 1.4); Say("雷暴开启 · 能力牌会带来雷球", 3); break;
                case "thunder": Thunder = true; Emit("thunder-ready", OrbKind.Lightning, 2); Animate("review", 1.2); Say("雷霆开启 · 闪电激发追加雷击", 3); break;
                case "tesla_coil":
                    int count = 0;
                    for (int i = 0; i < Orbs.Count; i++) if (Orbs[i].Kind == OrbKind.Lightning) { Passive(i, count * .12); count++; }
                    if (count == 0) Emit("coil", OrbKind.Lightning, 1);
                    Animate("running", 1); Say(count == 0 ? "特斯拉线圈 · 先召唤雷球吧" : "特斯拉线圈 · 雷球保留，继续放电", 2.5); break;
                case "capacitor": Capacity = Math.Min(5, Capacity + 2); Emit("capacity", OrbKind.Plasma, Capacity); Animate("review", 1.2); Say("扩容 · 现在有 " + Capacity + " 个球槽", 3); break;
                case "defragment": Focus = Math.Min(5, Focus + 1); Emit("focus", OrbKind.Frost, Focus); Animate("review", 1.2); Say("碎片整理 · 集中 " + Focus + "/5", 3); break;
                case "loop": Loop = true; nextLoop = Now + 6; Emit("loop", OrbKind.Frost, 1); Animate("review", 1.2); Say("循环开启 · 每六秒触发最右球", 3); break;
                case "buffer": ShieldHits = 3; ShieldUntil = Now + 15; Emit("shield", OrbKind.Frost, 3); Animate("waiting", 1.2); Say("缓冲 · 身后牌子抵挡三次，耗尽后破碎", 3); break;
                case "claw":
                    ClawCombo = Math.Min(10, ClawCombo + 1); Emit("claw", OrbKind.Lightning, ClawCombo); Animate("waving", .95); Say("爪击 · 连击 " + ClawCombo, 2); break;
                case "barrage":
                    int shots = Math.Max(1, Orbs.Count);
                    for (int i = 0; i < shots; i++) Emit("barrage", i < Orbs.Count ? Orbs[i].Kind : OrbKind.Lightning, 1 + Energy, i * .09);
                    Energy = 0; Animate("waving", 1.2); Say("弹幕齐射 · " + shots + " 发！", 2); break;
                case "sweeping_beam": Emit("beam", OrbKind.Frost, 1); Animate("waving", 1.2); Say("扫荡射线 · 锁定方向", 2); break;
                case "hyperbeam": Emit("hyperbeam", OrbKind.Frost, 4); Emit("cooling", OrbKind.Frost, 2, .8); Animate("running", 1.7); Say("超能光束！\n核心正在散热…", 3); break;
                case "meteor_strike":
                    Emit("meteor",OrbKind.Plasma,4);
                    double meteorBase=visualDelay;
                    for(int i=0;i<3;i++){visualDelay=meteorBase+.82+i*.12;Channel(OrbKind.Plasma);}
                    visualDelay=meteorBase;Animate("running",1.6);Say("陨石打击 · 冲击与三路等离子回流",2.5);break;
                case "ice_lance":
                    Channel(OrbKind.Frost); visualDelay += .15; Channel(OrbKind.Frost); visualDelay -= .15;
                    Emit("lance", OrbKind.Frost, 3); Animate("waving", 1.2); Say("冰之长枪 · 贯穿！", 2); break;
                case "ftl": Emit("dash", OrbKind.Frost, 3); Animate("running-right", .7); Say("超越光速 · 你跟得上吗？", 2); break;
                case "turbo": Energy = Math.Min(3, Energy + 2); Emit("charge", OrbKind.Plasma, Energy); Animate("running", 1.2); Say("内核加速 · 储能 " + Energy + "/3", 2); break;
                case "double_energy": Energy = Math.Max(1, Math.Min(3, Energy * 2)); Emit("energy", OrbKind.Plasma, Energy); Animate("review", 1.1); Say("双倍能量 · 储能 " + Energy + "/3", 2); break;
                case "creative_ai":
                    CreativeAI = true; nextAI = Now + 12;
                    string selected = DrawAICard(); Apply(selected); GeneratedSkill = selected; Emit("ai", OrbKind.Frost, 1); Say("创造性 AI 开启 · 每十二秒施放新牌", 3); break;
                case "machine_learning": Learning = true; learnedCards = 0; Emit("learn", OrbKind.Frost, 1); Animate("review", 1.3); Say("机器学习开启 · 每四张牌增加集中", 3); break;
                case "overclock": Energy = 3; OverclockUntil = Now + 5; Emit("overclock", OrbKind.Plasma, 3); Emit("cooling", OrbKind.Frost, 2, 5); Animate("running", 5); Say("超频 · 核心全速运转五秒", 3); break;
            }
            if (Storm && IsPower(skill) && skill != "storm") Channel(OrbKind.Lightning);
            } finally {visualCard=previousCard;applying=previousApplying;}
        }
    }
}
