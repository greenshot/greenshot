/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Ini.Parsing;
using Greenshot.Base.Core;
using Greenshot.Configuration;
using Greenshot.Editor.Configuration;
using Greenshot.Forms;
using Greenshot.Helpers;
using log4net;

namespace Greenshot;

/// <summary>
/// Description of Main.
/// </summary>
public class GreenshotMain
{
    private static ILog LOG;
    public static string LogFileLocation;
    static GreenshotMain()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        Assembly ayResult = null;
        string sShortAssemblyName = args.Name.Split(',')[0];
        Assembly[] ayAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly ayAssembly in ayAssemblies)
        {
            if (sShortAssemblyName != ayAssembly.FullName.Split(',')[0])
            {
                continue;
            }

            ayResult = ayAssembly;
            break;
        }

        return ayResult;
    }

    [STAThread]
    public static void Main(string[] args)
    {
        // Enable TLS 1.2 and 1.3 support only (TLS 1.0/1.1 deprecated per RFC 8996)
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        // Set the Thread name, is better than "1"
        Thread.CurrentThread.Name = Application.ProductName;

        // Init Log4NET
        LogFileLocation = LogHelper.InitializeLog4Net();
        // Get logger
        LOG = LogManager.GetLogger(typeof(MainForm));

        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        TaskScheduler.UnobservedTaskException += Task_UnhandledException;

        // Parse command-line arguments early so the optional --ini-directory override
        // can be incorporated into the IniConfigRegistry search paths before Create().
        // Returns null when --help was shown or a parse error occurred (exit immediately).
        var options = GreenshotCommandLine.Parse(args);
        if (options == null)
        {
            return;
        }

        // Register custom value converters (NativeRect, Color, etc.) before building the registry.
        IniValueConverters.Register();

        // Detect PortableApp (PAF) mode: the App\Greenshot directory lives next to the executable.
        var startupPath = AppContext.BaseDirectory;
        var pafAppPath = Path.Combine(startupPath, @"App\Greenshot");
        GreenshotEnvironment.IsPortable = Directory.Exists(pafAppPath);

        // Build the IniConfigRegistry:
        //   AddAppDataPath  → %APPDATA%\Greenshot
        //   AddSearchPath   → installation / startup directory
        //   --ini-directory → optional command-line override (highest priority)
        var builder = IniConfigRegistry.ForFile("greenshot.ini")
            .AddAppDataPath("Greenshot")
            .AddSearchPath(startupPath);

        if (!string.IsNullOrEmpty(options.IniDirectory) && Directory.Exists(options.IniDirectory))
        {
            builder.AddSearchPath(options.IniDirectory);
        }

        builder.AddDefaultsFile("greenshot-defaults.ini")
               .AddConstantsFile("greenshot-fixed.ini")
               .WithWriterOptions(new IniWriterOptions
               {
                   AssignmentSeparator = "=",
                   QuoteStyle = IniValueQuoteStyle.Auto,
                   EscapeSequences = true,
                   WriteComments = true
               })
               .RegisterSection<ICoreConfiguration>(new CoreConfigurationImpl())
               .RegisterSection<IEditorConfiguration>(new EditorConfigurationImpl())
               .RegisterSection<IWin10Configuration>(new Win10ConfigurationImpl())
               .AutoSaveInterval(TimeSpan.FromSeconds(2))
               .EmptyWhenNull()
               .LockFile()
               .EnableMetadata(applicationName: "Greenshot");

#if DEBUG
        builder.AddListener(new Helpers.IniListener());
#endif

        var iniConfig = builder.Create();

        // Log the startup
        LOG.Info("Starting: " + EnvironmentInfo.EnvironmentToString(false));

        MainForm.Start(options);
    }

    internal static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        Exception exceptionToLog = e.Exception;
        string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
        LOG.Error("Exception caught in the ThreadException handler.");
        LOG.Error(exceptionText);
        if (exceptionText != null && exceptionText.Contains("InputLanguageChangedEventArgs"))
        {
            // Ignore for BUG-1809
            return;
        }

        new BugReportForm(exceptionText).ShowDialog();
    }

    internal static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception exceptionToLog = e.ExceptionObject as Exception;
        string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
        LOG.Error("Exception caught in the UnhandledException handler.");
        LOG.Error(exceptionText);
        if (exceptionText != null && exceptionText.Contains("InputLanguageChangedEventArgs"))
        {
            // Ignore for BUG-1809
            return;
        }

        new BugReportForm(exceptionText).ShowDialog();
    }

    internal static void Task_UnhandledException(object sender, UnobservedTaskExceptionEventArgs args)
    {
        try
        {
            Exception exceptionToLog = args.Exception;
            string exceptionText = EnvironmentInfo.BuildReport(exceptionToLog);
            LOG.Error("Exception caught in the UnobservedTaskException handler.");
            LOG.Error(exceptionText);
            new BugReportForm(exceptionText).ShowDialog();
        }
        finally
        {
            args.SetObserved();
        }
    }
}