using NeoActPlugin.Core;
using Newtonsoft.Json;

namespace NeoActPlugin.Test
{
    public class LogParserTest : BaseParser
    {
        Dictionary<string, Dictionary<string, int>> Data = new Dictionary<string, Dictionary<string, int>>();

        protected override void AddAction(DateTime timestamp, string actor, string target, string skill, string damage, bool isCrit)
        {
            if (string.IsNullOrEmpty(damage))
            {
                Console.WriteLine(skill + "攻擊閃避");
            }
            else
            {
                if (!Data.ContainsKey(actor))
                {
                    Data.Add(actor, new Dictionary<string, int>());
                }
                if (!Data[actor].ContainsKey(skill))
                {
                    Data[actor].Add(skill, 0);
                }
                Data[actor][skill] += int.Parse(damage);
            }
            
        }

        public void TestLogFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"檔案不存在: {filePath}");
                return;
            }

            Console.WriteLine($"開始分析日誌檔案: {filePath}");
            Console.WriteLine("==========================================");

            int totalLines = 0;

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    totalLines++;
                    this.Parse(line, DateTime.Now);
                }
            }
            Console.WriteLine(JsonConvert.SerializeObject(Data));
        }
    }
} 