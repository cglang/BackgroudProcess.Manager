namespace BackgroudProcess.Manager
{
    public class Drag
    {
        public Drag(bool autoRun, string command, string name)
        {
            AutoRun = autoRun;
            Command = command;
            Name = name;
        }

        public int Pid { get; set; }

        public State State { get; set; }

        public bool AutoRun { get; set; }

        public string Command { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}