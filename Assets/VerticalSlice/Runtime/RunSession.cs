using System;

namespace Jimmy.Run
{
    public enum RunState { Ready, Running, Dead, Finished, Restarting }

    // Pure state/clock model. A scene reload constructs a fresh Ready session.
    public sealed class RunSession
    {
        public RunState State { get; private set; } = RunState.Ready;
        public double Elapsed { get; private set; }
        public double StateSince { get; private set; }
        private double startedAt;

        public bool Begin(double now)
        {
            if (State != RunState.Ready || !Valid(now)) return false;
            startedAt = StateSince = now; Elapsed = 0; State = RunState.Running; return true;
        }
        public void Tick(double now)
        {
            if (State == RunState.Running && Valid(now)) Elapsed = Math.Max(Elapsed, Math.Max(0, now - startedAt));
        }
        public bool Die(double now) => End(RunState.Dead, now);
        public bool Finish(double now) => End(RunState.Finished, now);
        private bool End(RunState next, double now)
        {
            if (State != RunState.Running || !Valid(now)) return false;
            Tick(now); StateSince = now; State = next; return true;
        }
        public bool Restart(double now)
        {
            if (State == RunState.Restarting || !Valid(now)) return false;
            Tick(now); State = RunState.Restarting; StateSince = now; return true;
        }
        private static bool Valid(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
