using System;
using NeoActPlugin.Core;

namespace NeoActPlugin.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("日誌解析測試程式");
            Console.WriteLine("==================");

            if (args.Length == 0)
            {
                Console.WriteLine("請提供日誌檔案路徑作為參數");
                Console.WriteLine("用法: NeoActPlugin.Test.exe <日誌檔案路徑>");
                return;
            }

            string logFilePath = args[0];
            new LogParserTest().TestLogFile(logFilePath);

            Console.WriteLine("\n按任意鍵退出...");
            Console.ReadKey();
        }
    }
} 