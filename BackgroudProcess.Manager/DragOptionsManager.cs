namespace BackgroudProcess.Manager
{
    public class DragOptionsManager
    {
        private static readonly string configPath = Path.Combine(Program.BasePath, "drag.conf");

        public static IEnumerable<DragOptions> GetDragOptions()
        {
            if (File.Exists(configPath))
                return File.ReadAllLines(configPath).Select(DragOptions.Parse);

            File.Create(configPath);
            throw new Exception($"未找到配置文件 {configPath} 已创建新的配置文件");
        }

        public static async Task SaveOptionsAsync(List<DragOptions> dragOptions)
        {
            var configs = dragOptions.Select(x => x.ToString());
            await File.WriteAllLinesAsync(configPath, configs);
        }
    }
}
