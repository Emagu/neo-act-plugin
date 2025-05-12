using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using NeoActPlugin.Common;

namespace NeoActPlugin.Core
{
    public class BaseParser
    {
        public static Regex regex_hit = new Regex(@"^(.*?) 命中(?:了)?(.*?)[，,]造成(?:了)?([\d,]+)點(暴擊)?傷害(?:[，,].*)?[。.]?$");
        public static Regex regex_critical = new Regex(@"^(.*?) (.*?)[，,]造成了([\d,]+)點(暴擊)?傷害[。.]?$");
        public static Regex regex_block = new Regex(@"^(.*?) (?:被)?(.*?)格擋，但造成了([\d,]+)點傷害[。.]?$");
        public static Regex regex_block2 = new Regex(@"^(.*?) (.*?)的反擊，仍造成了([\d,]+)點傷害[。.]?$");
        public static Regex regex_attakHeal = new Regex(@"^(.*?) 命中(?:了)?(.*?)，造成(?:了)?([\d,]+)點(暴擊)?傷害及吸收了([\d,]+)點生命力。");
        
        public static Regex regex_miss = new Regex(@"^(.*?) (.*?) [。.]?$");

        public static Regex regex_reduce = new Regex(@"^(.*?) (.*?)命中(?:了)?[，,、。]?但(解除了.*?)效果");

        public static Regex regex_defeat = new Regex(@"^\s*(.*?)受到(.*?) (死亡了)[。.]?$");
        public static Regex regex_defeat2 = new Regex(@"^\s*由於(.*?)的(.*?)，(.*?) 死亡了[。.]?$");
        public static Regex regex_playerDefeat = new Regex(@"^\s*由於(.*?)的(.*?)，(.*?) 陷入瀕死狀態[。.]?$");
        public static Regex regex_playerDefeat2 = new Regex(@"^\s*受到(.*?)的(.*?)影響，(.*?)\s*陷入瀕死狀態[。.]?$");

        public static Regex regex_hitButPerry = new Regex(@"^(.*?) (.*?)命中，但抵抗了(.*?) 效果[。.]?$");
        public static Regex regex_heal = new Regex(@"^由於(.*?)效果，恢復了([\d,]+)點(.*?)[。.]?$");
        public static Regex regex_heal2 = new Regex(@"^(.*?)受到(.*?) 恢復了([\d,]+)點(.*?)[。.]?$");
        //public static Regex regex_heal3 = new Regex(@"^(.*?)受到(.*?) 恢復了([\d,]+)點生命力[。.]?$");

        public static Regex regex_debuff = new Regex(@"^(.*?)的(.*?) 命中，受到(.*?) 效果[。.]?");
        public static Regex regex_debuff2 = new Regex(@"^(.*?) (.*?) 效果[。.]?$");
        public static Regex regex_debuff3 = new Regex(@"^(.*?) 對(.*?)造成效果[。.]?$");

        public static Regex regex_dot = new Regex(@"^(.*?) 給(.*?)[，,]?造成了([\d,]+)點傷害");
        public static Regex regex_dot2 = new Regex(@"^(.*?)受到(.*?) 受到([\d,]+)點傷害");
        public static Regex regex_dot3 = new Regex(@"^由於(.*?)的(.*?)，(.*?) 受到([\d,]+)點傷害");

        public static Regex regex_dot4 = new Regex(@"^(.*?)的(.*?)效果給(.*?)造成了([\d,]+)點傷害");

        public static Regex regex_incomingdamage = new Regex(@"^\s*(.*?)的\s*([^的\s]*) 命中(?:了)?(.*?)[，,]受到([\d,]+)點傷害(?:.*)?$");
        public static Regex regex_incomingdamage2 = new Regex(@"^\s*(.*)的\s*([^的\s]*) 命中(?:了)?(.*?)[，,]造成(?:了)?([\d,]+)點傷害(?:.*)?$");
        public static Regex regex_incomingdamage_block = new Regex(@"^\s*(.*)的\s*([^的\s]*) (.*?)[，,]但仍受到([\d,]+)點傷害(?:.*)?$");
        public static Regex regex_incomingdamageCtit = new Regex(@"^\s*(.*)的\s*([^的\s]*) 命中(?:了)?(.*?)[，,]造成(?:了)?([\d,]+)點暴擊傷害(?:.*)?$");

        public DateTime ParseLogDateTime(string message)
        {
            DateTime ret = DateTime.MinValue;
            try
            {
                if (message == null) return ret;

                if (message.Contains("|"))
                {
                    int pipeIndex = message.IndexOf('|');
                    string timestampPart = message.Substring(0, pipeIndex);
                    if (!DateTime.TryParse(timestampPart, out ret))
                    {

                        Log(false, "Failed to parse timestamp");
                        return DateTime.MinValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(false, "Error [ParseLogDateTime] " + ex.ToString().Replace(Environment.NewLine, " "));
            }
            return ret;
        }

        protected virtual void CheckAct()
        { 
        }

        protected virtual void Log(bool IsDebug, string msg)
        { 
            Console.WriteLine(msg);
        }

        protected virtual void AddAction(
            DateTime timestamp, 
            string actor, 
            string target,
            string skill,
            string damage = "",
            bool isCrit = false)
        { }

        protected void Parse(string logLine, DateTime timestamp)
        {
            try
            {
                Match m;

                m = regex_incomingdamage.Match(logLine);
                if (!m.Success)
                    m = regex_incomingdamage2.Match(logLine);
                if (!m.Success)
                    m = regex_incomingdamage_block.Match(logLine);
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

                    Log(true, $"{logLine}=>{actor},{skill},{target},{damage}");
                    AddAction(timestamp, actor, target, skill, damage, false);

                    return;
                }

                m = regex_incomingdamageCtit.Match(logLine);
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

                    Log(true, $"{logLine}=>{actor},{skill},{target},{damage}");
                    AddAction(timestamp, actor, target, skill, damage, true);

                    return;
                }

                m = regex_dot4.Match(logLine);
                if (m.Success)
                {
                    string target = m.Groups[3].Success ? DecodeString(m.Groups[3].Value) : "";
                    if (target == "不明")
                        target = "_Unknown";
                    string actor = "自己";
                    string skill = m.Groups[1].Success ? DecodeString(m.Groups[1].Value) : "";
                    string damage = (m.Groups[4].Value ?? "").Replace(",", "");

                    if (!m.Groups[4].Success)
                        return;

                    Log(true, $"{logLine}=>{actor},{skill},{target},{damage}");
                    AddAction(timestamp, actor, target, skill, damage, false);

                    return;
                }
                m = regex_hit.Match(logLine);
                if (!m.Success)
                    m = regex_critical.Match(logLine);
                if (!m.Success)
                    m = regex_block.Match(logLine);
                if (!m.Success)
                    m = regex_block2.Match(logLine);
                if (!m.Success)
                    m = regex_attakHeal.Match(logLine);

                if (m.Success)
                {
                    string actor = "自己";
                    string skill = DecodeString(m.Groups[1].Value ?? "");
                    string target = m.Groups[2].Success ? DecodeString(m.Groups[2].Value) : "";
                    string damage = (m.Groups[3].Value ?? "").Replace(",", "");
                    bool isCrit = m.Groups.Count > 4 && m.Groups[4].Success;

                    Log(true, $"{logLine}=>{actor},{skill},{target},{isCrit},{damage}");
                    AddAction(timestamp, actor, target, skill, damage, isCrit);

                    return;
                }

                #region 因為不知道誰的dot傷害，公平起見都忽略
                m = regex_dot.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_dot2.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_dot3.Match(logLine);
                if (m.Success)
                {
                    return;
                }
                #endregion


                m = regex_miss.Match(logLine);
                if (m.Success)
                {
                    string actor = "自己";
                    string skill = DecodeString(m.Groups[1].Value ?? "");
                    string target = m.Groups[2].Success ? DecodeString(m.Groups[2].Value) : "";
                    
                    Log(true, $"{logLine}=>{actor},{skill},{target},閃避");
                    AddAction(timestamp, actor, target, skill);

                    return;
                }

                m = regex_heal.Match(logLine);
                if (m.Success)
                {
                    return;
                }
                m = regex_heal2.Match(logLine);
                if (m.Success)
                {
                    return;
                }
                
                #region 因為異常狀態不知道誰給的，公平起見都忽略
                m = regex_debuff.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_debuff2.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_debuff3.Match(logLine);
                if (m.Success)
                {
                    return;
                }
                #endregion

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

                #region 處理死亡訊息
                m = regex_defeat.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_defeat2.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_playerDefeat.Match(logLine);
                if (m.Success)
                {
                    return;
                }

                m = regex_playerDefeat2.Match(logLine);
                if (m.Success)
                {
                    return;
                }
                #endregion
            }
            catch (Exception ex)
            {
                string exception = ex.ToString().Replace(Environment.NewLine, " ");
                if (ex.InnerException != null)
                    exception += " " + ex.InnerException.ToString().Replace(Environment.NewLine, " ");

                Log(false,  "Error [LogParse.BeforeLogLineRead] " + exception + " " + logLine);
            }

            // For debugging
            if (!string.IsNullOrWhiteSpace(logLine))
                Log(false, "Unhandled Line: " + logLine);
        }
        

        private static string DecodeString(string data)
        {
            string ret = data.Replace("&apos;", "'")
                .Replace("&amp;", "&");

            return ret;
        }
    }
    public class LogParser : BaseParser
    {
        private static IACTWrapper _ACT = null;

        public static void Initialize(IACTWrapper ACT)
        {
            _ACT = ACT;
        }

        protected override void CheckAct()
        {
            if (_ACT == null)
                throw new ApplicationException("ACT Wrapper not initialized.");
        }
        protected override void Log(bool IsDebug, string msg)
        {
            if (IsDebug)
            {
                PluginMain.WriteLog(LogLevel.Debug, msg);
            }
            else
            { 
                PluginMain.WriteLog(LogLevel.Info, msg);
            }
        }
        protected override void AddAction(DateTime timestamp, string actor, string target, string skill, string damage = "", bool isCirt = false)
        {
            var IsBoss = false;
            foreach (var name in PluginMain.BossNames)
            {
                if (target.Contains(name))
                { 
                    IsBoss = true;
                }
            }

            if (!IsBoss)
            {
                return;
            }

            if (_ACT.SetEncounter(timestamp, actor, target))
            {
                if (string.IsNullOrEmpty(damage))
                {
                    _ACT.AddCombatAction(
                        (int)Advanced_Combat_Tracker.SwingTypeEnum.NonMelee,
                        false,
                        "",
                        actor,
                        skill,
                        Advanced_Combat_Tracker.Dnum.Miss,
                        timestamp,
                        _ACT.GlobalTimeSorter,
                        target,
                        "");
                }
                else
                {
                    _ACT.AddCombatAction(
                        (int)Advanced_Combat_Tracker.SwingTypeEnum.NonMelee,
                        isCirt,
                        "",
                        actor,
                        skill,
                        new Advanced_Combat_Tracker.Dnum(int.Parse(damage)),
                        timestamp,
                        _ACT.GlobalTimeSorter,
                        target,
                        "");
                }   
            }
        }

        public void BeforeLogLineRead(bool isImport, Advanced_Combat_Tracker.LogLineEventArgs logInfo)
        {
            CheckAct();
            string logLine = logInfo.logLine;

            DateTime timestamp = ParseLogDateTime(logLine);
            if (logLine.Contains("|"))
            {
                int pipeIndex = logLine.IndexOf('|');
                logLine = logLine.Substring(pipeIndex + 1);
            }

            logInfo.logLine = string.Format("[{0:HH:mm:ss.fff}] {1}", timestamp, logLine);

            Parse(logLine, timestamp);
        }
    }
}
