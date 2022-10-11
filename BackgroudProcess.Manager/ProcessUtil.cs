using System.Diagnostics;

namespace BackgroudProcess.Manager
{
    public static class ProcessUtil
    {
        public static State GetState(int pid)
        {
            if (pid <= 0) return State.Stop;
            try
            {
                Process.GetProcessById(pid);
            }
            catch
            {
                return State.Stop;
            }
            return State.Run;
        }

        public static int StartProcess(string command)
        {
            var psi = new ProcessStartInfo(command)
            {
                WorkingDirectory = string.Empty,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            return proc!.Id;
        }

        public static bool StopProcess(int pid)
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
