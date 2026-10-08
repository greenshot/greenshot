/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Runtime.CompilerServices;

// Kernel32Api is required to attach to (or allocate) a Windows console window before
// printing help text, because Greenshot is built as a WinExe and has no console by default.
using Dapplo.Windows.Kernel32;
using Greenshot.Ipc.Cli;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Holds the result of parsing Greenshot's command line arguments.
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// When true, exit without starting or interacting with the application.
        /// </summary>
        public bool NoRun { get; set; }

        /// <summary>
        /// When set, update the configured UI language and save the configuration before continuing.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// When set, use this directory for reading and writing the greenshot.ini configuration file.
        /// </summary>
        public string IniDirectory { get; set; }

        /// <summary>
        /// When true, the application was started by the Windows Restart Manager
        /// (e.g. to restore state after a Windows Update reboot).
        /// This option is reserved for the Windows Restart Manager and is NOT intended for manual use.
        /// </summary>
        public bool Restore { get; set; }

        /// <summary>
        /// The arguments after the startup options: a Greenshot command in the syntax of greenshot-cli.exe
        /// (e.g. <c>--file a.png</c>, <c>a.png</c>, <c>--recipe ocr</c>, <c>--reload</c>, <c>--exit</c>).
        /// It is sent to the running Greenshot exactly as greenshot-cli.exe sends it; empty when there is none.
        /// </summary>
        public string[] CommandArguments { get; set; } = [];
    }

    /// <summary>
    /// Parses Greenshot.exe's command line: leading startup options, which only Greenshot.exe itself uses
    /// (--language, --ini-directory, --no-run, --restore, --help), followed by an optional Greenshot command.
    /// The command is not interpreted here: it is parsed by <see cref="CliCommandParser"/>, like every
    /// command that reaches Greenshot through greenshot-cli.exe or greenshot-proxy.exe.
    /// </summary>
    internal static class GreenshotCommandLine
    {
        private static readonly Option<bool> NoRunOption = new Option<bool>("--no-run")
        {
            Description = "Exit immediately without starting or showing Greenshot."
        };

        private static readonly Option<string> LanguageOption = new Option<string>("--language")
        {
            HelpName = "language-code",
            Description = "Set the UI language for Greenshot (e.g. en-US, de-DE) and save the configuration."
        };

        private static readonly Option<string> IniDirectoryOption = new Option<string>("--ini-directory")
        {
            HelpName = "directory",
            Description = "Set the directory where greenshot.ini is stored and read."
        };

        /// <summary>
        /// Reserved for the Windows Restart Manager, which starts Greenshot with the arguments registered via
        /// RegisterApplicationRestart. Not intended to be used manually.
        /// </summary>
        private static readonly Option<bool> RestoreOption = new Option<bool>("--restore")
        {
            Hidden = true,
            Description = "[Reserved] Called by the Windows Restart Manager to restore the application after a system restart. Not intended for manual use."
        };

        /// <summary>Startup options that take a value.</summary>
        private static readonly HashSet<string> OptionsWithValue = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--language", "--ini-directory" };

        /// <summary>Startup options without a value, including the help options.</summary>
        private static readonly HashSet<string> Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--no-run", "--restore", "--help", "-h", "-?" };

        /// <summary>
        /// Parses the given command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments passed to the application.</param>
        /// <returns>
        /// A <see cref="CommandLineOptions"/> instance when the application should continue processing,
        /// or <c>null</c> when the application should exit (e.g. after printing help or encountering a
        /// parse error).
        /// </returns>
        public static CommandLineOptions Parse(string[] args)
        {
            // Most starts (autostart, the start menu) have no arguments: this doesn't load and initialize System.CommandLine
            if (args == null || args.Length == 0)
            {
                return new CommandLineOptions();
            }

            return ParseArguments(args);
        }

        /// <summary>
        /// Parses a command line with at least one argument, kept apart from Parse so System.CommandLine is only loaded when it's needed
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CommandLineOptions ParseArguments(string[] args)
        {
            // The startup options come first; everything from the first other argument on is the command
            int commandStart = 0;
            while (commandStart < args.Length)
            {
                if (OptionsWithValue.Contains(args[commandStart]))
                {
                    commandStart = Math.Min(commandStart + 2, args.Length);
                }
                else if (Flags.Contains(args[commandStart]))
                {
                    commandStart++;
                }
                else
                {
                    break;
                }
            }
            string[] startupArguments = args.Take(commandStart).ToArray();
            string[] commandArguments = args.Skip(commandStart).ToArray();

            var rootCommand = BuildRootCommand();

            // Greenshot is a WinExe and has no console window by default.
            // Attach to the parent's console (or allocate a new one) before printing help.
            bool needsConsole = startupArguments.Any(a => a is "--help" or "-h" or "-?");
            bool allocatedNewConsole = needsConsole && AttachOrAllocateConsole();

            CommandLineOptions result = null;
            rootCommand.SetAction(parseResult => {
                result = new CommandLineOptions
                {
                    NoRun = parseResult.GetValue(NoRunOption),
                    Language = parseResult.GetValue(LanguageOption),
                    IniDirectory = parseResult.GetValue(IniDirectoryOption),
                    Restore = parseResult.GetValue(RestoreOption),
                    CommandArguments = commandArguments
                };
            });

            ParseResult parseResult = rootCommand.Parse(startupArguments);
            // Invoke the command. Returns 0 when the handler ran successfully,
            // or non-zero when help was displayed or a parse error occurred (handler is not invoked).
            _ = parseResult.Invoke();

            // If a new console was allocated, wait for a key press before closing it
            // so the user has time to read the output.
            if (allocatedNewConsole)
            {
                Console.ReadKey();
            }

            return result;
        }

        /// <summary>
        /// Writes an error about the command line to the console of the caller, if Greenshot was started from one.
        /// </summary>
        public static void ReportError(string message)
        {
            if (Kernel32Api.AttachConsole())
            {
                Console.Error.WriteLine($"greenshot: {message}");
            }
        }

        /// <summary>Returns true when a new console had to be allocated.</summary>
        private static bool AttachOrAllocateConsole()
        {
            if (Kernel32Api.AttachConsole())
            {
                return false;
            }
            Kernel32Api.AllocConsole();
            return true;
        }

        private static RootCommand BuildRootCommand()
        {
            var rootCommand = new RootCommand("Greenshot")
            {
                Description = "Greenshot is a free and open source screenshot tool for Windows.\n\n" +
                              "Usage: Greenshot.exe [startup options] [command]\n\n" +
                              "The command has the same syntax as for greenshot-cli.exe (see greenshot-cli.exe --help), e.g.\n" +
                              "  Greenshot.exe image.png            open a file with the recipes of its OpenFile triggers\n" +
                              "  Greenshot.exe --recipe ocr         run a recipe\n" +
                              "  Greenshot.exe --reload / --exit    reload the configuration / exit Greenshot\n" +
                              "The command is sent to the running Greenshot; if none is running, Greenshot starts and runs it.\n" +
                              "Unlike greenshot-cli.exe, Greenshot.exe shows no output of the command."
            };
            rootCommand.Options.Add(NoRunOption);
            rootCommand.Options.Add(LanguageOption);
            rootCommand.Options.Add(IniDirectoryOption);
            rootCommand.Options.Add(RestoreOption);

            return rootCommand;
        }
    }
}
