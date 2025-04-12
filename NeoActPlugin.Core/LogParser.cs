using System;
using System.Text.RegularExpressions;
using NeoActPlugin.Common;

namespace NeoActPlugin.Core
{
    public static class LogParser
    {
        public static Regex regex_hit = new Regex(@"^(.*?) (?:命中|命中了)?(.*?)[，,]造成(?:了)?([\d,]+)點(暴擊)?傷害");
        public static Regex regex_dot = new Regex(@"^(.*?) 給(.*?)[，,]?造成了([\d,]+)點傷害");
        public static Regex regex_critical = new Regex(@"^(.*?) (.*?)[，,]造成了([\d,]+)點暴擊傷害");
        public static Regex regex_block = new Regex(@"^(.*?) 被(.*?)格擋，但造成了([\d,]+)點傷害");

        public static Regex regex_reduce = new Regex(@"^(.*?) (.*?)(命中)[，,、。]?但(解除了.*?)效果");
        public static Regex regex_defeat = new Regex(@"^(.*?)受到(.*?) (死亡了)");
        public static Regex regex_hitButPerry = new Regex(@"^(.*?) (.*?)命中，但抵抗了(.*?) 效果");

        public static Regex regex_incomingdamage = new Regex(@"^(.*?)的(.*?) 命中(?:了(.*?))?[，,]受到([\d,]+)點傷害");
        public static Regex regex_incomingdamage2 = new Regex(@"^(.*?)的(.*?) 命中(?:了(.*?))?[，,]造成([\d,]+)點傷害");

        private static IACTWrapper _ACT = null;

        public static void Initialize(IACTWrapper ACT)
        {
            _ACT = ACT;
        }

        public static DateTime ParseLogDateTime(string message)
        {
            DateTime ret = DateTime.MinValue;

            if (_ACT == null)
                throw new ApplicationException("ACT Wrapper not initialized.");

            try
            {
                if (message == null) return ret;

                if (message.Contains("|"))
                {
                    int pipeIndex = message.IndexOf('|');
                    string timestampPart = message.Substring(0, pipeIndex);
                    if (!DateTime.TryParse(timestampPart, out ret))
                    {

                        PluginMain.WriteLog(LogLevel.Error, "Failed to parse timestamp");
                        return DateTime.MinValue;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginMain.WriteLog(LogLevel.Error, "Error [ParseLogDateTime] " + ex.ToString().Replace(Environment.NewLine, " "));
            }
            return ret;
        }

        public static void BeforeLogLineRead(bool isImport, Advanced_Combat_Tracker.LogLineEventArgs logInfo)
        {
            string logLine = logInfo.logLine;
            if (_ACT == null)
                throw new ApplicationException("ACT Wrapper not initialized.");

            try
            {
                DateTime timestamp = ParseLogDateTime(logLine);
                if (logLine.Contains("|"))
                {
                    int pipeIndex = logLine.IndexOf('|');
                    logLine = logLine.Substring(pipeIndex + 1);
                }

                logInfo.logLine = string.Format("[{0:HH:mm:ss.fff}] {1}", timestamp, logLine);

                Match m;

                m = regex_incomingdamage.Match(logLine);
                if(!m.Success)
                    m = regex_incomingdamage2.Match(logLine);
                if (m.Success)
                {
                    string target = m.Groups[3].Success ? DecodeString(m.Groups[3].Value) : "";
                    if (target == "不明")
                        target = "_Unknown";
                    string actor = m.Groups[1].Success ? DecodeString(m.Groups[1].Value) : "";
                    if (actor == "不明")
                        actor = "_Unknown";
                    string skill = m.Groups[2].Success ? DecodeString(m.Groups[2].Value) : "";
                    string damage = (m.Groups[4].Value ?? "").Replace(",", "");
                    if (string.IsNullOrWhiteSpace(target))
                        target = "自己";

                    if (string.IsNullOrWhiteSpace(actor))
                        actor = "不明";

                    if (!m.Groups[4].Success)
                        return;
                    PluginMain.WriteLog(LogLevel.Info, $"{logLine}=>{actor},{skill},{target},{damage}");
                    if (_ACT.SetEncounter(timestamp, actor, target))
                    {
                        _ACT.AddCombatAction(
                            (int)Advanced_Combat_Tracker.SwingTypeEnum.NonMelee,
                            false,
                            "",
                            actor,
                            skill,
                            new Advanced_Combat_Tracker.Dnum(int.Parse(damage)),
                            timestamp,
                            _ACT.GlobalTimeSorter,
                            target,
                            "");
                    }

                    return;
                }

                m = regex_hit.Match(logLine);
                if (!m.Success)
                    m = regex_dot.Match(logLine);
                if (!m.Success)
                    m = regex_critical.Match(logLine);
                if (!m.Success)
                    m = regex_block.Match(logLine);
                if (m.Success)
                {
                    SimpleHit(timestamp, m);
                    return;
                }

                m = regex_reduce.Match(logLine);
                if (m.Success)
                {
                    return;
                }
                
                m = regex_hitButPerry.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_defeat.Match(logLine);
                if (m.Success)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                string exception = ex.ToString().Replace(Environment.NewLine, " ");
                if (ex.InnerException != null)
                    exception += " " + ex.InnerException.ToString().Replace(Environment.NewLine, " ");

                PluginMain.WriteLog(LogLevel.Error, "Error [LogParse.BeforeLogLineRead] " + exception + " " + logInfo.logLine);
            }

            // For debugging
            if (!string.IsNullOrWhiteSpace(logLine))
                PluginMain.WriteLog(LogLevel.Warning, "Unhandled Line: " + logInfo.logLine);
        }

        private static void SimpleHit(DateTime timestamp, Match m)
        {
            string actor = "自己";
            string skill = DecodeString(m.Groups[1].Value ?? "");
            string target = m.Groups[2].Success ? DecodeString(m.Groups[2].Value) : "";
            string damage = (m.Groups[3].Value ?? "").Replace(",", "");
            bool isCrit = m.Groups.Count > 4 && m.Groups[4].Success;
            if (_ACT.SetEncounter(timestamp, actor, target))
            {
                _ACT.AddCombatAction(
                    (int)Advanced_Combat_Tracker.SwingTypeEnum.NonMelee,
                    isCrit,
                    "",
                    actor,
                    skill,
                    new Advanced_Combat_Tracker.Dnum(int.Parse(damage)),
                    timestamp,
                    _ACT.GlobalTimeSorter,
                    target,
                    "");
            }
            return;
        }

        private static string DecodeString(string data)
        {
            string ret = data.Replace("&apos;", "'")
                .Replace("&amp;", "&");

            return ret;
        }
    }
}
