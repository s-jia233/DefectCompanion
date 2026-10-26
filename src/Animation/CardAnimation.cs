using System;
using System.Collections.Generic;

namespace DefectCompanion
{
    public sealed class CardAnimation
    {
        public readonly string Clip;
        public readonly double Release,HoldUntil,Duration,ReleasePose,HoldPose,EndPose;
        private readonly bool linear;
        private CardAnimation(string clip,double release,double holdUntil,double duration,double releasePose,double holdPose,double endPose,bool linear=false)
        {Clip=clip;Release=release;HoldUntil=holdUntil;Duration=duration;ReleasePose=releasePose;HoldPose=holdPose;EndPose=endPose;this.linear=linear;}
        private static double Smooth(double x){x=Math.Max(0,Math.Min(1,x));return x*x*(3-2*x);}
        public double PoseTime(double time)
        {
            if(linear)return Math.Max(0,Math.Min(EndPose,time));
            if(time<Release)return ReleasePose*Smooth(time/Release);
            if(time<HoldUntil)return ReleasePose+(HoldPose-ReleasePose)*Smooth((time-Release)/(HoldUntil-Release));
            return HoldPose+(EndPose-HoldPose)*Smooth((time-HoldUntil)/(Duration-HoldUntil));
        }
        private static readonly CardAnimation attack=new CardAnimation("attack",.29,.48,1.24,.32,.40,1.499);
        private static readonly CardAnimation cast=new CardAnimation("cast",.28,.66,1.26,.34,.45,1.232);
        private static readonly CardAnimation core=new CardAnimation("process",.34,.85,1.42,.42,.56,1.565);
        private static readonly Dictionary<string,CardAnimation> scores=new Dictionary<string,CardAnimation> {
            {"machine_learning",new CardAnimation("art-learning",.48,1.85,2.60,.48,1.85,2.599,true)},
            {"plasma",new CardAnimation("art-fusion",1.12,1.65,2.50,1.12,1.65,2.499,true)},
            {"claw",new CardAnimation("art-claw",.30,.46,1.60,.30,.46,1.599,true)},
            {"ball_lightning",new CardAnimation("art-ball-lightning",.72,1.10,1.90,.72,1.10,1.899,true)},
            {"cold_snap",new CardAnimation("attack",.25,.40,1.12,.33,.40,1.499)},
            {"ice_lance",new CardAnimation("art-ice-spear",.82,1.16,2.30,.82,1.16,2.299,true)},
            {"rocket",new CardAnimation("art-rocket",.22,.88,1.50,.22,.88,1.499,true)},
            {"sweeping_beam",new CardAnimation("attack",.33,.78,1.38,.32,.43,1.499)},
            {"hyperbeam",new CardAnimation("art-hyperbeam",.58,1.48,2.10,.58,1.48,2.099,true)},
            {"battery",new CardAnimation("process",.36,.98,1.52,.43,.57,1.565)},
            {"frost",new CardAnimation("process",.30,.75,1.35,.38,.51,1.565)},
            {"buffer",new CardAnimation("cast",.34,.90,1.44,.38,.45,1.232)},
            {"hologram",new CardAnimation("cast",.28,.94,1.58,.32,.44,1.232)},
            {"dualcast",new CardAnimation("art-dualcast",.34,1.02,1.80,.34,1.02,1.799,true)},
            {"dark",new CardAnimation("art-dark",.72,1.20,1.80,.72,1.20,1.799,true)},
            {"echo",new CardAnimation("art-echo",.48,1.35,2.20,.48,1.35,2.199,true)},
            {"meteor_strike",new CardAnimation("cast",.40,1.13,1.78,.37,.44,1.232)},
            {"tempest",new CardAnimation("art-tempest",.52,1.55,2.30,.52,1.55,2.299,true)},
            {"glacier",new CardAnimation("art-glacier",.44,1.50,2.40,.44,1.50,2.399,true)},
            {"rainbow",new CardAnimation("art-rainbow",.38,1.20,2.15,.38,1.20,2.149,true)},
            {"thunder",new CardAnimation("art-thunder",.36,1.22,1.95,.36,1.22,1.949,true)},
            {"storm",new CardAnimation("art-storm",.48,1.65,2.50,.48,1.65,2.499,true)},
            {"turbo",new CardAnimation("art-turbo",.16,.64,47.0/30,.16,.64,1.565,true)},
            {"overclock",new CardAnimation("art-overclock",.42,1.20,2.10,.42,1.20,2.099,true)}
        };
        public static CardAnimation For(string skill)
        {
            CardAnimation score;if(skill!=null && scores.TryGetValue(skill,out score))return score;
            switch(skill)
            {
                case "ball_lightning":case "barrage":case "ftl":return attack;
                case "double_energy":case "defragment":case "capacitor":case "machine_learning":
                case "creative_ai":case "echo":case "loop":case "reboot":return core;
                default:return cast;
            }
        }
    }
}
