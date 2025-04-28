using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using NeoActPlugin.Common;

namespace NeoActPlugin.Core
{
    public class PluginMain
    {
        private TinyIoCContainer _container;
        private static ILogger _logger;
        readonly List<string> BossNames = new List<string>
        {
            "青龍鬼",
            "赤龍鬼"
        };
        TabPage tab;
        Label label;
        ControlPanel panel;
        LogParser LogParser;

        private System.Windows.Forms.Timer updateTimer;
        private Form dpsForm;
        private Panel dpsPanel;
        private Dictionary<string, DpsBarControl> playerControls = new Dictionary<string, DpsBarControl>();

        internal string PluginDirectory { get; private set; }

        public PluginMain(string pluginDirectory, Logger logger, TinyIoCContainer container)
        {
            _container = container;
            PluginDirectory = pluginDirectory;
            _logger = logger;
            LogParser = new LogParser();

            _container.Register(this);
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            try
            {
                this.tab = pluginScreenSpace;
                this.label = pluginStatusText;

                this.label.Text = "Initializing...";

                if (!IsRunningAsAdmin())
                {
                    this.label.Text = "Error: Run ACT as Administrator.";

                    MessageBox.Show(
                        "NeoActPlugin requires ACT to be run as Administrator. Please restart ACT with elevated privileges.",
                        "Admin Rights Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    this.DeInitPlugin();

                    return;
                }

                this.panel = new ControlPanel(_container);
                this.panel.Dock = DockStyle.Fill;
                this.tab.Controls.Add(this.panel);
                this.tab.Name = "Neo ACT Plugin";

                _logger.Log(LogLevel.Info, "Initialized.");
                /*
                Updater.Updater.PerformUpdateIfNecessary(PluginDirectory, _container);

                ActGlobals.oFormActMain.UpdateCheckClicked += new FormActMain.NullDelegate(UpdateCheckClicked);
                if (ActGlobals.oFormActMain.GetAutomaticUpdatesAllowed())
                {
                    Thread updateThread = new Thread(new ThreadStart(UpdateCheckClicked));
                    updateThread.IsBackground = true;
                    updateThread.Start();
                }

                UpdateACTTables();
                */
                LogParser.Initialize(new ACTWrapper());

                ActGlobals.oFormActMain.LogPathHasCharName = false;
                ActGlobals.oFormActMain.LogFileFilter = "*.log";

                ActGlobals.oFormActMain.TimeStampLen = DateTime.Now.ToString("HH:mm:ss.fff").Length + 1;

                ActGlobals.oFormActMain.GetDateTimeFromLog = new FormActMain.DateTimeLogParser(LogParser.ParseLogDateTime);

                ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(LogParser.BeforeLogLineRead);

                ActGlobals.oFormActMain.OnCombatEnd += OnCombatEnd;

                ActGlobals.oFormActMain.OnCombatStart += OnCombatStart;

                ActGlobals.oFormActMain.ChangeZone("Blade & Soul");

                LogWriter.Initialize();

                dpsForm = new DpsOverlayForm
                {
                    Text = "隨便做的UI",
                    Width = 300,
                    Height = 350,
                    FormBorderStyle = FormBorderStyle.FixedSingle,
                    TopMost = true,
                    MaximizeBox = false,      // 禁用最大化按鈕
                    BackColor = Color.Lime,           // 透明用顏色
                    TransparencyKey = Color.Lime
                };

                dpsPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.Transparent
                };
                dpsForm.Controls.Add(dpsPanel);
                dpsForm.Show();

                updateTimer = new System.Windows.Forms.Timer
                {
                    Interval = 1000
                };
                updateTimer.Tick += UpdateTimer_Tick;
                updateTimer.Start();

                this.label.Text = "Initialized.";
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteInfoLog(ex.Message);
                WriteLog(LogLevel.Error, "Exception during InitPlugin: " + ex.ToString().Replace(Environment.NewLine, " "));
                this.label.Text = "InitPlugin Error.";
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var encounter = ActGlobals.oFormActMain.ActiveZone.ActiveEncounter;
                if (encounter == null) return;

                var combatants = encounter.Items.Values
                    .Where(c => c.Damage > 0)
                    .OrderByDescending(c => c.Damage)
                    .Take(6)
                    .ToList();

                double totalDamage = combatants.Sum(c => c.Damage);

                // 移除不在前6名的player
                var keysToRemove = playerControls.Keys.Except(combatants.Select(c => c.Name)).ToList();
                foreach (var key in keysToRemove)
                {
                    if (playerControls.TryGetValue(key, out var control))
                    {
                        dpsPanel.Controls.Remove(control);
                        control.Dispose();
                    }
                    playerControls.Remove(key);
                }

                int y = 10;
                int panelWidth = dpsPanel.ClientSize.Width - 20;

                foreach (var c in combatants)
                {
                    if (!playerControls.TryGetValue(c.Name, out var dpsBar))
                    {
                        dpsBar = new DpsBarControl
                        {
                            Width = panelWidth,
                            Height = 40, // 放大一點好看
                            BackColor = Color.Transparent
                        };
                        playerControls[c.Name] = dpsBar;
                        dpsPanel.Controls.Add(dpsBar);
                    }

                    double percent = (totalDamage > 0) ? (c.Damage / totalDamage) : 0;

                    dpsBar.SetValues(c.Name, c.EncDPS, percent);
                    dpsBar.Width = panelWidth;
                    dpsBar.Location = new Point(5, y);

                    y += dpsBar.Height + 5;
                }

                // 重新按照Damage高低排列Control順序
                dpsPanel.Controls.SetChildIndex(playerControls[combatants[0].Name], 0);
                if (combatants.Count > 1) dpsPanel.Controls.SetChildIndex(playerControls[combatants[1].Name], 1);
                if (combatants.Count > 2) dpsPanel.Controls.SetChildIndex(playerControls[combatants[2].Name], 2);
                if (combatants.Count > 3) dpsPanel.Controls.SetChildIndex(playerControls[combatants[3].Name], 3);
                if (combatants.Count > 4) dpsPanel.Controls.SetChildIndex(playerControls[combatants[4].Name], 4);
                if (combatants.Count > 5) dpsPanel.Controls.SetChildIndex(playerControls[combatants[5].Name], 5);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DpsBarPlugin] Error: {ex.Message}");
            }
        }

        private void OnCombatStart(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            playerControls.Clear();
            dpsPanel.Controls.Clear();
        }
        private void OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
        {
            if (isImport) return; // 匯入Log時不處理

            var encounter = encounterInfo.encounter;

            foreach (var data in encounter.Items.Values)
            {
                if (BossNames.Contains(data.Name))
                {
                    encounter.Title = $"[{data.Name}]({DateTime.Now:MMdd-HHmm})";
                    break; // 找到一個就改，不繼續找了
                }
            }
        }

        private bool IsRunningAsAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public void DeInitPlugin()
        {
            LogWriter.Uninitialize();

            ActGlobals.oFormActMain.UpdateCheckClicked -= this.UpdateCheckClicked;
            ActGlobals.oFormActMain.BeforeLogLineRead -= LogParser.BeforeLogLineRead;
            ActGlobals.oFormActMain.OnCombatEnd -= OnCombatEnd;
            ActGlobals.oFormActMain.OnCombatStart -= OnCombatStart;

            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Dispose();
            }

            if (dpsForm != null)
            {
                dpsForm.Close();
                dpsForm.Dispose();
            }

            if (this.label != null)
            {
                this.label.Text = "BnS Plugin Unloaded.";
                this.label = null;
            }
        }


        public void UpdateCheckClicked()
        {

        }

        private void UpdateACTTables()
        {

        }


        public static void WriteLog(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }

        private void cmdClearMessages_Click(object sender, EventArgs e)
        {
            //lstMessages.Items.Clear();
        }

        private void cmdCopyProblematic_Click(object sender, EventArgs e)
        {
            //StringBuilder sb = new StringBuilder();
            //foreach (object itm in lstMessages.Items)
            //    sb.AppendLine((itm ?? "").ToString());

            //if (sb.Length > 0)
            //    System.Windows.Forms.Clipboard.SetText(sb.ToString());
        }
    }

    public interface IACTWrapper
    {
        bool SetEncounter(DateTime Time, string Attacker, string Victim);
        void AddCombatAction(int SwingType, bool Critical, string Special, string Attacker, string theAttackType, Advanced_Combat_Tracker.Dnum Damage, DateTime Time, int TimeSorter, string Victim, string theDamageType);
        int GlobalTimeSorter { get; set; }
    }

    public class ACTWrapper : IACTWrapper
    {
        public int GlobalTimeSorter
        {
            get
            {
                return Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter;
            }

            set
            {
                Advanced_Combat_Tracker.ActGlobals.oFormActMain.GlobalTimeSorter = value;
            }
        }

        public void AddCombatAction(int SwingType, bool Critical, string Special, string Attacker, string theAttackType, Dnum Damage, DateTime Time, int TimeSorter, string Victim, string theDamageType)
        {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.AddCombatAction(SwingType, Critical, Special, Attacker, theAttackType, Damage, Time, TimeSorter, Victim, theDamageType);
        }

        public bool SetEncounter(DateTime Time, string Attacker, string Victim)
        {
            return Advanced_Combat_Tracker.ActGlobals.oFormActMain.SetEncounter(Time, Attacker, Victim);
        }
    }
}
