using System;

namespace DefectCompanion
{
    // A shared monotonic timeline caps both composition and fallback wakeups at 60 Hz.
    public sealed class FramePacer
    {
        public const double TargetFps = 60;
        private double next = Double.NaN, previous = Double.NaN;
        public void Reset() { next = previous = Double.NaN; }
        public bool Due(double seconds)
        {
            if (seconds == previous) return false; // WPF can raise Rendering twice for one composition.
            if (Double.IsNaN(next) || seconds < previous || seconds - next > .25) next = seconds;
            previous = seconds;
            if (seconds + .0005 < next) return false;
            next += 1.0 / TargetFps;
            return true;
        }
    }
    public static class AnimationTiming
    {
        public const int Samples = 8;
        public static int Sample(int row, int sourceCount, double seconds, bool overclocked)
        {
            double fps = row == 1 || row == 2 ? sourceCount / .9 : row == 0 ? 4 : row == 4 ? 6.5 : row == 3 ? 7 : 8;
            if (overclocked) fps *= 1.5;
            int sample = (int)(Math.Max(0, seconds) * fps * Samples);
            bool loop = row == 0 || row == 1 || row == 2 || row == 6 || row == 7;
            // The final seven samples are identical holds, not a return to the first pose.
            return loop ? sample % (sourceCount * Samples) : Math.Min(sample, (sourceCount - 1) * Samples);
        }
    }
    public sealed class DragDirection
    {
        public int Direction { get; private set; }
        private double previous, reverseTravel;
        public void Begin(double pointerX, int direction)
        { previous = pointerX; Direction = direction < 0 ? -1 : 1; reverseTravel = 0; }
        public int Move(double pointerX)
        {
            double delta = pointerX - previous; previous = pointerX;
            if (delta * Direction < 0) reverseTravel -= delta * Direction;
            else reverseTravel = Math.Max(0, reverseTravel - Math.Abs(delta));
            if (reverseTravel >= 14) { Direction = -Direction; reverseTravel = 0; }
            return Direction;
        }
    }
}
