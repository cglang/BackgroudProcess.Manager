namespace BackgroudProcess.Manager
{
    /// <summary>程序信息（与 csproj 中的 Version / Authors / Product 保持一致）。</summary>
    public static class AppInfo
    {
        public const string Name = "bpm";

        public const string Version = "0.0.2";

        public const string Author = "cglang";

        public static string Display => $"{Name} v{Version}  by {Author}";
    }
}
