/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.CommandLine;
using System.Linq;

// Kernel32Api is required to attach to (or allocate) a Windows console window before
// printing help text, because Greenshot is built as a WinExe and has no console by default.
using Dapplo.Windows.Kernel32;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Holds the result of parsing Greenshot's command line arguments.
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// When true, send an exit command to all running Greenshot instances and exit.
        /// </summary>
        public bool Exit { get; set; }

        /// <summary>
        /// When true, send a reload-configuration command to all running Greenshot instances and exit.
        /// </summary>
        public bool Reload { get; set; }

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
        /// One or more image files to open in the running Greenshot instance,
        /// or in a new instance if none is running.
        /// </summary>
        public string[] Files { get; set; } = Array.Empty<string>();

        /// <summary>
        /// When true, the application was started by the Windows Restart Manager
        /// (e.g. to restore state after a Windows Update reboot).
        /// This option is reserved for the Windows Restart Manager and is NOT intended for manual use.
        /// Note: The Windows Restart Manager invokes applications with /restore; this flag serves as the
        /// designated placeholder for that behaviour in future implementations.
        /// </summary>
        public bool Restore { get; set; }
    }

    /// <summary>
    /// Defines and parses Greenshot's command line interface using System.CommandLine.
    /// To add a new argument, declare a new <see cref="Option{T}"/> or <see cref="Argument{T}"/> field,
    /// register it via <see cref="BuildRootCommand"/>, and read it inside the handler in <see cref="Parse"/>.
    /// </summary>
    internal static class GreenshotCommandLine
    {
        private static readonly Option<bool> ExitOption = new Option<bool>("--exit")
        {
            Description = "Send an exit command to all running Greenshot instances."
        };

        private static readonly Option<bool> ReloadOption = new Option<bool>("--reload")
        {
            Description = "Send a reload-configuration command to all running Greenshot instances."
        };

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
        /// Reserved for the Windows Restart Manager.
        /// Windows may invoke the application with /restore after a system restart (e.g. following a
        /// Windows Update). This option is the designated placeholder for that behaviour and is NOT
        /// intended to be used manually by end users.
        /// </summary>
        private static readonly Option<bool> RestoreOption = new Option<bool>("--restore")
        {
            Hidden = true,
            Description = "[Reserved] Called by the Windows Restart Manager to restore the application after a system restart. Not intended for manual use."
        };

        private static readonly Argument<string[]> FilesArgument = new Argument<string[]>("files")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "One or more image files to open. If Greenshot is already running, the files are opened in the existing instance."
        };

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
            var rootCommand = BuildRootCommand();

            // Greenshot is a WinExe and has no console window by default.
            // Attach to the parent's console (or allocate a new one) before printing help or error messages.
            bool needsConsole = args.Any(a => a is "--help" or "-h" or "-?");
            bool allocatedNewConsole = false;
            if (needsConsole)
            {
                bool attached = Kernel32Api.AttachConsole();
                if (!attached)
                {
                    Kernel32Api.AllocConsole();
                    allocatedNewConsole = true;
                }
            }

            CommandLineOptions result = null;
            rootCommand.SetAction(parseResult => {
                result = new CommandLineOptions
                {
                    Exit = parseResult.GetValue(ExitOption),
                    Reload = parseResult.GetValue(ReloadOption),
                    NoRun = parseResult.GetValue(NoRunOption),
                    Language = parseResult.GetValue(LanguageOption),
                    IniDirectory = parseResult.GetValue(IniDirectoryOption),
                    Restore = parseResult.GetValue(RestoreOption),
                    Files = parseResult.GetValue(FilesArgument) ?? Array.Empty<string>()
                };
            });

            ParseResult parseResult = rootCommand.Parse(args);
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

        private static RootCommand BuildRootCommand()
        {
            var rootCommand = new RootCommand("Greenshot");

            rootCommand.Description = "Greenshot is a free and open source screenshot tool for Windows.\n\n" +
                             "Note: When another Greenshot instance is already running, commands such as\n" +
                             "--exit and --reload are forwarded to that running instance via IPC, and the\n" +
                             "new instance then exits. File arguments are similarly forwarded to the running\n" +
                             "instance. If no other instance is running, Greenshot starts normally.";
            rootCommand.Options.Add(ExitOption);
            rootCommand.Options.Add(ReloadOption);
            rootCommand.Options.Add(NoRunOption);
            rootCommand.Options.Add(LanguageOption);
            rootCommand.Options.Add(IniDirectoryOption);
            rootCommand.Options.Add(RestoreOption);
            rootCommand.Arguments.Add(FilesArgument);

            return rootCommand;
        }
    }
}
