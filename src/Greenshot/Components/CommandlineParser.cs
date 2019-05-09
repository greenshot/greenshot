// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapplo.Log;
using Dapplo.Windows.Kernel32;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;

namespace Greenshot.Components
{
    /// <summary>
    /// Process the commandline
    /// </summary>
    public class CommandlineParser
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly DestinationHolder _destinationHolder;

        public CommandlineParser(
            ICoreConfiguration coreConfiguration,
            DestinationHolder destinationHolder)
        {
            _coreConfiguration = coreConfiguration;
            _destinationHolder = destinationHolder;
        }

        /// <summary>
        /// Process the commandline arguments
        /// </summary>
        /// <param name="isAlreadyRunning">bool which specifies if the application is already running</param>
        /// <param name="arguments">string array with the arguments</param>
        public void Process(bool isAlreadyRunning, string[] arguments)
        {
            var filesToOpen = new List<string>();
            if (arguments.Length > 0 && Log.IsDebugEnabled())
            {
                Log.Debug().WriteLine("Greenshot arguments: " + string.Join(",", arguments.Select(s => $"\"{s}\"")));
            }

            for (var argumentNr = 0; argumentNr < arguments.Length; argumentNr++)
            {
                var argument = arguments[argumentNr];

                switch (argument)
                {
                    case "/help":
                    case "/h":
                    case "/?":
                        break;
                }
                // Help
                if (argument.ToLower().Equals("/help") || argument.ToLower().Equals("/h") || argument.ToLower().Equals("/?"))
                {
                    // Try to attach to the console
                    var attachedToConsole = Kernel32Api.AttachConsole();
                    // If attach didn't work, open a console
                    if (!attachedToConsole)
                    {
                        Kernel32Api.AllocConsole();
                    }
                    var helpOutput = new StringBuilder();
                    helpOutput.AppendLine();
                    helpOutput.AppendLine("Greenshot commandline options:");
                    helpOutput.AppendLine().AppendLine();
                    helpOutput.AppendLine("\t/help");
                    helpOutput.AppendLine("\t\tThis help.");
                    helpOutput.AppendLine().AppendLine();
                    helpOutput.AppendLine("\t/exit");
                    helpOutput.AppendLine("\t\tTries to close all running instances.");
                    helpOutput.AppendLine().AppendLine();
                    helpOutput.AppendLine("\t/reload");
                    helpOutput.AppendLine("\t\tReload the configuration of Greenshot.");
                    helpOutput.AppendLine().AppendLine();
                    helpOutput.AppendLine("\t/language [language code]");
                    helpOutput.AppendLine("\t\tSet the language of Greenshot, e.g. greenshot /language en-US.");
                    helpOutput.AppendLine().AppendLine();
                    helpOutput.AppendLine("\t/inidirectory [directory]");
                    helpOutput.AppendLine("\t\tSet the directory where the greenshot.ini should be stored & read.");
                    helpOutput.AppendLine().AppendLine();
                    helpOutput.AppendLine("\t[filename]");
                    helpOutput.AppendLine("\t\tOpen the bitmap files in the running Greenshot instance or start a new instance");
                    helpOutput.AppendLine();
                    helpOutput.AppendLine();
                    helpOutput.AppendLine("\t/capture <region|window|fullscreen>[,Destination]");
                    helpOutput.AppendLine("\t\tStart capture from command line. Use /capture without arguments for more info. Greenshot must be running");
                    Console.WriteLine(helpOutput.ToString());

                    // If attach didn't work, wait for key otherwise the console will close to quickly
                    if (!attachedToConsole)
                    {
                        Console.ReadKey();
                    }
                    // TODO:
                    //FreeMutex();
                    return;
                }

                if (argument.ToLower().Equals("/exit"))
                {
                    // unregister application on uninstall (allow uninstall)
                    try
                    {
                        Log.Info().WriteLine("Sending all instances the exit command.");
                        // Pass Exit to running instance, if any
                        //GreenshotClient.Exit();
                    }
                    catch (Exception e)
                    {
                        Log.Warn().WriteLine(e, "Exception by exit.");
                    }
                    // TODO:
                    //FreeMutex();
                    return;
                }

                // Reload the configuration
                if (argument.ToLower().Equals("/reload"))
                {
                    // Modify configuration
                    Log.Info().WriteLine("Reloading configuration!");
                    try
                    {
                        // Update running instances
                        //GreenshotClient.ReloadConfig();
                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Couldn't reload configuration.");
                    }
                    // TODO:
/*
                    finally
                    {
                        FreeMutex();
                    }
*/
                    return;
                }

                // Stop running
                if (argument.ToLower().Equals("/norun"))
                {
                    // Make an exit possible
                    // TODO:
                    //FreeMutex();
                    return;
                }

                // Language
                if (argument.ToLower().Equals("/language"))
                {
                    _coreConfiguration.Language = arguments[++argumentNr];
                    continue;
                }

                // Setting the INI-directory
                if (argument.ToLower().Equals("/inidirectory"))
                {
                    // Change the ini config location
                    //IniConfig.Current.IniDirectory = arguments[++argumentNr];
                    continue;
                }

                // Capture from command line, only accept as first argument
                if (arguments.Length > 0 && arguments[0].ToLower().Equals("/capture"))
                {
                    // Try to attach to the console
                    bool attachedToConsole = Kernel32Api.AttachConsole();
                    // If attach didn't work, open a console
                    if (!attachedToConsole)
                    {
                        Kernel32Api.AllocConsole();
                    }
                    // Display help for /capture command if not enough arguments
                    if (arguments.Length < 2)
                    {
                        var helpOutput = new StringBuilder();

                        helpOutput.AppendLine();
                        helpOutput.AppendLine();
                        helpOutput.AppendLine("Usage for /capture:");
                        helpOutput.AppendLine("\t/capture <region|window|fullscreen>[,Destination]");
                        helpOutput.AppendLine();
                        helpOutput.AppendLine("\tDestinations: (CaseSensitive!)");
                        helpOutput.AppendFormat(
                            "\t\t{0,-16}\t==>\t{1}{2}",
                            "\"External CmdName\"",
                            "External Command like MS Paint",
                            Environment.NewLine
                            );
                        foreach (var destination in _destinationHolder.AllDestinations)
                        {
                            helpOutput.AppendFormat(
                                    "\t\t{0,-16}\t==>\t{1}{2}",
                                    destination.Metadata.Designation,
                                    destination.Value.Description,
                                    Environment.NewLine
                                    );
                        }
                        helpOutput.AppendLine();
                        helpOutput.AppendLine();
                        helpOutput.AppendLine("\tUsage examples:");
                        helpOutput.AppendLine("\t\t/capture window,FileNoDialog\t\t(capture window, save in default screenshot folder)");
                        helpOutput.AppendLine("\t\t/capture fullscreen\t\t\t(capture fullscreen, use destination from settings)");
                        helpOutput.AppendLine("\t\t/capture region,Clipboard\t\t(capture region directly to clipboard)");
                        helpOutput.AppendLine("\t\t/capture fullscreen,Picker\t\t(capture fullscreen, ask what to do)");
                        helpOutput.AppendLine("\t\t/capture region,\"External MS Paint\"\t(capture region and send to external command 'MS Paint')");
                        helpOutput.AppendLine();
                        helpOutput.AppendLine("\tShortcut path examples for Windows:");
                        helpOutput.AppendLine("\t\t\"C:\\Program Files\\Greenshot\\Greenshot.exe\" /capture region,\"External MS Paint\"");
                        helpOutput.AppendLine("\t\tD:\\Programme\\Greenshot\\Greenshot.exe /capture fullscreen,FileNoDialog");
                        helpOutput.AppendLine();
                        Console.WriteLine(helpOutput.ToString());
                        // If attach didn't work, wait for key otherwise the console will close to quickly
                        if (!attachedToConsole)
                        {
                            Console.ReadKey();
                        }
                    }
                    else if (!isAlreadyRunning)
                    {
                        Console.WriteLine("{0}{0}Please start Greenshot first", Environment.NewLine);
                        // If attach didn't work, wait for key otherwise the console will close to quickly
                        if (!attachedToConsole)
                        {
                            Console.ReadKey();
                        }
                    }
                    else
                    {
                        //GreenshotClient.Capture(arguments[1]);
                    }
                    // TODO:
                    //FreeMutex();
                    return;
                }

                // Files to open
                filesToOpen.Add(argument);
            }

            if (isAlreadyRunning)
            {
                // We didn't initialize the language yet, do it here just for the message box
                if (filesToOpen.Count > 0)
                {
                    //GreenshotClient.OpenFiles(filesToOpen);
                }
                else
                {
                    //ShowInstances();
                }
                // TODO:
                //FreeMutex();
                //Application.Exit();
                return;
            }


        }
    }
}
