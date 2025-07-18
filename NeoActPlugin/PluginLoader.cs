﻿using Advanced_Combat_Tracker;
using NeoActPlugin.Common;
using NeoActPlugin.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace NeoActPlugin
{
    public class PluginLoader : IActPluginV1
    {
        PluginMain pluginMain;
        Logger logger;
        static AssemblyResolver asmResolver;
        string pluginDirectory;
        TabPage pluginScreenSpace;
        Label pluginStatusText;
        bool initFailed = false;

        public TinyIoCContainer Container { get; private set; }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            pluginDirectory = GetPluginDirectory();

            if (asmResolver == null)
            {
                asmResolver = new AssemblyResolver(new List<string>
                {
                    Path.Combine(pluginDirectory, "libs"),
                    Path.Combine(pluginDirectory, "addons"),
#if DEBUG
                    Path.Combine(pluginDirectory, "libs", Environment.Is64BitProcess ? "x64" : "x86"),
#else
                    //GetCefPath()
#endif
                });
            }

            this.pluginScreenSpace = pluginScreenSpace;
            this.pluginStatusText = pluginStatusText;

            if (!SanityChecker.LoadSaneAssembly("NeoActPlugin.Common") || !SanityChecker.LoadSaneAssembly("NeoActPlugin.Core"))
            {
                pluginStatusText.Text = Resources.FailedToLoadCommon;
                return;
            }

            Initialize();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize()
        {
            pluginStatusText.Text = Resources.InitRuntime;

            var container = new TinyIoCContainer();
            logger = new Logger();
            container.Register(logger);
            container.Register<ILogger>(logger);

            asmResolver.ExceptionOccured += (o, e) => logger.Log(LogLevel.Error, Resources.AssemblyResolverError, e.Exception);
            asmResolver.AssemblyLoaded += (o, e) => logger.Log(LogLevel.Info, Resources.AssemblyResolverLoaded, e.LoadedAssembly.FullName);

            this.Container = container;
            pluginMain = new PluginMain(pluginDirectory, logger, container);
            container.Register(pluginMain);

            pluginStatusText.Text = Resources.InitCef;

            SanityChecker.CheckDependencyVersions(logger);

            FinishInit(container);
        }

        public void FinishInit(TinyIoCContainer container)
        {
            try
            {
                pluginMain.InitPlugin(pluginScreenSpace, pluginStatusText);
                initFailed = false;
            }
            catch (Exception ex)
            {
                initFailed = true;

                MessageBox.Show("Failed to init plugin: " + ex.ToString(), "NeoActPlugin Error");
            }
        }

        public void DeInitPlugin()
        {
            if (pluginMain != null && !initFailed)
            {
                pluginMain.DeInitPlugin();
            }

        }

        private string GetPluginDirectory()
        {
            var plugin = ActGlobals.oFormActMain.ActPlugins.Where(x => x.pluginObj == this).FirstOrDefault();
            if (plugin != null)
            {
                return Path.GetDirectoryName(plugin.pluginFile.FullName);
            }
            else
            {
                throw new Exception("Could not find ourselves in the plugin list!");
            }
        }
    }
}
