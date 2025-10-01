/*----------------------------------------------------------------
 * 版权所有 (c) 2025 HaiTangYunchi  保留所有权利
 * CLR版本：4.0.30319.42000
 * 公司名称：HaiTangYunchi
 * 命名空间：HaiTang.library
 * 唯一标识：6e37dc09-17f0-4511-ae7f-8277e8956c48
 * 文件名：Logger
 * 
 * 创建者：海棠云螭
 * 电子邮箱：haitangyunchi@126.com
 * 创建时间：2025/9/29 5:13:43
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：
 * 时间：
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/

namespace HaiTang.library
{
    public class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        public enum LogLevel
        {
            INFO,
            WARN,
            ERROR
        }

        public static void Log(string message, LogLevel level = LogLevel.INFO)
        {
            Task.Run(() => // 异步写入避免阻塞UI
            {
                lock (_lock)
                {
                    try
                    {
                        if (!Directory.Exists(LogDirectory))
                            Directory.CreateDirectory(LogDirectory);

                        string logFile = Path.Combine(LogDirectory, $"log_{DateTime.Now:yyyyMMdd}.log");
                        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";

                        File.AppendAllText(logFile, logMessage);
                    }
                    catch
                    {
                        
                    }
                }
            });
        }

    }
}
