/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    public static class FilenameHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FilenameHelper));

        // Specify the regular expression for the filename formatting:
        // Starting with ${
        // than the varname, which ends with a : or }
        // If a parameters needs to be supplied, than a ":" should follow the name... everything from the : until the } is considered to be part of the parameters.
        // The parameter format is a single alpha followed by the value belonging to the parameter, e.g. :
        // ${capturetime:d"yyyy-MM-dd HH_mm_ss"}
        private static readonly Regex VarRegexp = new Regex(@"\${(?<variable>[^:}]+)[:]?(?<parameters>[^}]*)}", RegexOptions.Compiled);
        private static readonly Regex CmdVarRegexp = new Regex(@"%(?<variable>[^%]+)%", RegexOptions.Compiled);

        private static readonly Regex SplitRegexp = new Regex(";(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Compiled);
        private const int MaxTitleLength = 80;
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private const string UnsafeReplacement = "_";
        private static readonly Random RandomNumberGen = new Random();
        private static readonly Regex RandRegexp = new Regex("^R+$", RegexOptions.Compiled);

        /// <summary>
        /// Remove invalid characters from the fully qualified filename
        /// </summary>
        /// <param name="fullPath">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakeFqFilenameSafe(string fullPath)
        {
            string path = MakePathSafe(Path.GetDirectoryName(fullPath));
            string filename = MakeFilenameSafe(Path.GetFileName(fullPath));
            // Make the fullpath again and return
            return Path.Combine(path, filename);
        }

        /// <summary>
        /// Remove invalid characters from the filename
        /// </summary>
        /// <param name="filename">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakeFilenameSafe(string filename)
        {
            // Make the filename save!
            if (filename != null)
            {
                foreach (char disallowed in Path.GetInvalidFileNameChars())
                {
                    filename = filename.Replace(disallowed.ToString(), UnsafeReplacement);
                }
            }

            return filename;
        }

        /// <summary>
        /// Remove invalid characters from the path
        /// </summary>
        /// <param name="path">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakePathSafe(string path)
        {
            // Make the path save!
            if (path != null)
            {
                foreach (char disallowed in Path.GetInvalidPathChars())
                {
                    path = path.Replace(disallowed.ToString(), UnsafeReplacement);
                }
            }

            return path;
        }

        public static string GetFilenameWithoutExtensionFromPattern(string pattern)
        {
            return GetFilenameWithoutExtensionFromPattern(pattern, null);
        }

        public static string GetFilenameWithoutExtensionFromPattern(string pattern, ICaptureDetails captureDetails)
        {
            return FillPattern(pattern, captureDetails, true);
        }

        public static string GetFilenameFromPattern(string pattern, OutputFormat imageFormat)
        {
            return GetFilenameFromPattern(pattern, imageFormat, null);
        }

        public static string GetFilenameFromPattern(string pattern, OutputFormat imageFormat, ICaptureDetails captureDetails)
        {
            return FillPattern(pattern, captureDetails, true) + "." + imageFormat.ToString().ToLower();
        }

        /// <summary>
        /// Return a filename for the current image format (png,jpg etc) with the default file pattern
        /// that is specified in the configuration
        /// </summary>
        /// <param name="format">A string with the format</param>
        /// <param name="captureDetails"></param>
        /// <returns>The filename which should be used to save the image</returns>
        public static string GetFilename(OutputFormat format, ICaptureDetails captureDetails)
        {
            string pattern = CoreConfig.OutputFileFilenamePattern;
            if (string.IsNullOrEmpty(pattern?.Trim()))
            {
                pattern = "greenshot ${capturetime}";
            }

            return GetFilenameFromPattern(pattern, format, captureDetails);
        }


        /// <summary>
        /// This method will be called by the regexp.replace as a MatchEvaluator delegate!
        /// Will delegate this to the MatchVarEvaluatorInternal and catch any exceptions
        /// </summary>
        /// <param name="match">What are we matching?</param>
        /// <param name="captureDetails">The detail, can be null</param>
        /// <param name="processVars">Variables from the process</param>
        /// <param name="userVars">Variables from the user</param>
        /// <param name="machineVars">Variables from the machine</param>
        /// <param name="filenameSafeMode"></param>
        /// <returns>string with the match replacement</returns>
        private static string MatchVarEvaluator(Match match, ICaptureDetails captureDetails, IDictionary processVars, IDictionary userVars, IDictionary machineVars,
            bool filenameSafeMode)
        {
            try
            {
                return MatchVarEvaluatorInternal(match, captureDetails, processVars, userVars, machineVars, filenameSafeMode);
            }
            catch (Exception e)
            {
                Log.Error("Error in MatchVarEvaluatorInternal", e);
            }

            return string.Empty;
        }

        /// <summary>
        /// This method will be called by the regexp.replace as a MatchEvaluator delegate!
        /// </summary>
        /// <param name="match">What are we matching?</param>
        /// <param name="captureDetails">The detail, can be null</param>
        /// <param name="processVars"></param>
        /// <param name="userVars"></param>
        /// <param name="machineVars"></param>
        /// <param name="filenameSafeMode"></param>
        /// <returns></returns>
        private static string MatchVarEvaluatorInternal(Match match, ICaptureDetails captureDetails, IDictionary processVars, IDictionary userVars, IDictionary machineVars,
            bool filenameSafeMode)
        {
            // some defaults
            int padWidth = 0;
            int startIndex = 0;
            int endIndex = 0;
            char padChar = ' ';
            string dateFormat = "yyyy-MM-dd HH-mm-ss";
            IDictionary<string, string> replacements = new Dictionary<string, string>();
            string replaceValue = string.Empty;
            string variable = match.Groups["variable"].Value;
            string parameters = match.Groups["parameters"].Value;
            string randomChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            if (parameters.Length > 0)
            {
                string[] parms = SplitRegexp.Split(parameters);
                foreach (string parameter in parms)
                {
                    switch (parameter.Substring(0, 1))
                    {
                        // Padding p<width>[,pad-character]
                        case "p":
                            string[] padParams = parameter.Substring(1).Split(',');
                            try
                            {
                                padWidth = int.Parse(padParams[0]);
                            }
                            catch
                            {
                                // ignored
                            }

                            if (padParams.Length > 1)
                            {
                                padChar = padParams[1][0];
                            }

                            break;
                        // replace
                        // r<old string>,<new string>
                        case "r":
                            string[] replaceParameters = parameter.Substring(1).Split(',');
                            if (replaceParameters != null && replaceParameters.Length == 2)
                            {
                                replacements.Add(replaceParameters[0], replaceParameters[1]);
                            }

                            break;
                        // Dateformat d<format>
                        // Format can be anything that is used in C# date formatting
                        case "d":
                            dateFormat = parameter.Substring(1);
                            if (dateFormat.StartsWith("\""))
                            {
                                dateFormat = dateFormat.Substring(1);
                            }

                            if (dateFormat.EndsWith("\""))
                            {
                                dateFormat = dateFormat.Substring(0, dateFormat.Length - 1);
                            }

                            break;
                        // Substring:
                        // s<start>[,length]
                        case "s":
                            string range = parameter.Substring(1);
                            string[] rangelist = range.Split(',');
                            if (rangelist.Length > 0)
                            {
                                try
                                {
                                    startIndex = int.Parse(rangelist[0]);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }

                            if (rangelist.Length > 1)
                            {
                                try
                                {
                                    endIndex = int.Parse(rangelist[1]);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }

                            break;
                    }
                }
            }

            if (processVars != null && processVars.Contains(variable))
            {
                replaceValue = (string) processVars[variable];
                if (filenameSafeMode)
                {
                    replaceValue = MakePathSafe(replaceValue);
                }
            }
            else if (userVars != null && userVars.Contains(variable))
            {
                replaceValue = (string) userVars[variable];
                if (filenameSafeMode)
                {
                    replaceValue = MakePathSafe(replaceValue);
                }
            }
            else if (machineVars != null && machineVars.Contains(variable))
            {
                replaceValue = (string) machineVars[variable];
                if (filenameSafeMode)
                {
                    replaceValue = MakePathSafe(replaceValue);
                }
            }
            else if (captureDetails?.MetaData != null && captureDetails.MetaData.ContainsKey(variable))
            {
                replaceValue = captureDetails.MetaData[variable];
                if (filenameSafeMode)
                {
                    replaceValue = MakePathSafe(replaceValue);
                }
            }
            else if (RandRegexp.IsMatch(variable))
            {
                for (int i = 0; i < variable.Length; i++)
                {
                    replaceValue += randomChars[RandomNumberGen.Next(randomChars.Length)];
                }
            }
            else
            {
                // Handle other variables
                // Default use "now" for the capture take´n
                DateTime capturetime = DateTime.Now;
                // Use default application name for title
                string title = Application.ProductName;

                // Check if we have capture details
                if (captureDetails != null)
                {
                    capturetime = captureDetails.DateTime;
                    if (captureDetails.Title != null)
                    {
                        title = captureDetails.Title;
                        if (title.Length > MaxTitleLength)
                        {
                            title = title.Substring(0, MaxTitleLength);
                        }
                    }
                }

                switch (variable)
                {
                    case "domain":
                        replaceValue = Environment.UserDomainName;
                        break;
                    case "user":
                        replaceValue = Environment.UserName;
                        break;
                    case "hostname":
                        replaceValue = Environment.MachineName;
                        break;
                    case "YYYY":
                        if (padWidth == 0)
                        {
                            padWidth = -4;
                            padChar = '0';
                        }

                        replaceValue = capturetime.Year.ToString();
                        break;
                    case "MM":
                        replaceValue = capturetime.Month.ToString();
                        if (padWidth == 0)
                        {
                            padWidth = -2;
                            padChar = '0';
                        }

                        break;
                    case "DD":
                        replaceValue = capturetime.Day.ToString();
                        if (padWidth == 0)
                        {
                            padWidth = -2;
                            padChar = '0';
                        }

                        break;
                    case "hh":
                        if (padWidth == 0)
                        {
                            padWidth = -2;
                            padChar = '0';
                        }

                        replaceValue = capturetime.Hour.ToString();
                        break;
                    case "mm":
                        if (padWidth == 0)
                        {
                            padWidth = -2;
                            padChar = '0';
                        }

                        replaceValue = capturetime.Minute.ToString();
                        break;
                    case "ss":
                        if (padWidth == 0)
                        {
                            padWidth = -2;
                            padChar = '0';
                        }

                        replaceValue = capturetime.Second.ToString();
                        break;
                    case "now":
                        replaceValue = DateTime.Now.ToString(dateFormat);
                        if (filenameSafeMode)
                        {
                            replaceValue = MakeFilenameSafe(replaceValue);
                        }

                        break;
                    case "capturetime":
                        replaceValue = capturetime.ToString(dateFormat);
                        if (filenameSafeMode)
                        {
                            replaceValue = MakeFilenameSafe(replaceValue);
                        }

                        break;
                    case "NUM":
                        CoreConfig.OutputFileIncrementingNumber++;
                        IniConfig.Save();
                        replaceValue = CoreConfig.OutputFileIncrementingNumber.ToString();
                        if (padWidth == 0)
                        {
                            padWidth = -6;
                            padChar = '0';
                        }

                        break;
                    case "title":
                        replaceValue = title;
                        if (filenameSafeMode)
                        {
                            replaceValue = MakeFilenameSafe(replaceValue);
                        }

                        break;
                    case "MyPictures":
                        replaceValue = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                        break;
                    case "MyMusic":
                        replaceValue = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                        break;
                    case "MyDocuments":
                        replaceValue = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        break;
                    case "Personal":
                        replaceValue = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                        break;
                    case "Desktop":
                        replaceValue = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        break;
                    case "ApplicationData":
                        replaceValue = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        break;
                    case "LocalApplicationData":
                        replaceValue = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        break;
                }
            }

            // do padding
            if (padWidth > 0)
            {
                replaceValue = replaceValue.PadRight(padWidth, padChar);
            }
            else if (padWidth < 0)
            {
                replaceValue = replaceValue.PadLeft(-padWidth, padChar);
            }

            // do substring
            if (startIndex != 0 || endIndex != 0)
            {
                if (startIndex < 0)
                {
                    startIndex = replaceValue.Length + startIndex;
                }

                if (endIndex < 0)
                {
                    endIndex = replaceValue.Length + endIndex;
                }

                if (endIndex != 0)
                {
                    try
                    {
                        replaceValue = replaceValue.Substring(startIndex, endIndex);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                else
                {
                    try
                    {
                        replaceValue = replaceValue.Substring(startIndex);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }

            // new for feature #697
            if (replacements.Count > 0)
            {
                foreach (string oldValue in replacements.Keys)
                {
                    replaceValue = replaceValue.Replace(oldValue, replacements[oldValue]);
                }
            }

            return replaceValue;
        }

        /// <summary>
        /// "Simply" fill the pattern with environment variables
        /// </summary>
        /// <param name="pattern">String with pattern %var%</param>
        /// <param name="filenameSafeMode">true to make sure everything is filenamesafe</param>
        /// <returns>Filled string</returns>
        public static string FillCmdVariables(string pattern, bool filenameSafeMode = true)
        {
            IDictionary processVars = null;
            IDictionary userVars = null;
            IDictionary machineVars = null;
            try
            {
                processVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.Process", e);
            }

            try
            {
                userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.User", e);
            }

            try
            {
                machineVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.Machine", e);
            }

            return CmdVarRegexp.Replace(pattern,
                m => MatchVarEvaluator(m, null, processVars, userVars, machineVars, filenameSafeMode)
            );
        }

        /// <summary>
        /// "Simply" fill the pattern with environment variables
        /// </summary>
        /// <param name="pattern">String with pattern ${var}</param>
        /// <param name="filenameSafeMode">true to make sure everything is filenamesafe</param>
        /// <returns>Filled string</returns>
        public static string FillVariables(string pattern, bool filenameSafeMode)
        {
            IDictionary processVars = null;
            IDictionary userVars = null;
            IDictionary machineVars = null;
            try
            {
                processVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.Process", e);
            }

            try
            {
                userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.User", e);
            }

            try
            {
                machineVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.Machine", e);
            }

            return VarRegexp.Replace(pattern,
                m => MatchVarEvaluator(m, null, processVars, userVars, machineVars, filenameSafeMode)
            );
        }

        /// <summary>
        /// Fill the pattern wit the supplied details
        /// </summary>
        /// <param name="pattern">Pattern</param>
        /// <param name="captureDetails">CaptureDetails, can be null</param>
        /// <param name="filenameSafeMode">Should the result be made "filename" safe?</param>
        /// <returns>Filled pattern</returns>
        public static string FillPattern(string pattern, ICaptureDetails captureDetails, bool filenameSafeMode)
        {
            IDictionary processVars = null;
            IDictionary userVars = null;
            IDictionary machineVars = null;
            try
            {
                processVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.Process", e);
            }

            try
            {
                userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.User", e);
            }

            try
            {
                machineVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
            }
            catch (Exception e)
            {
                Log.Error("Error retrieving EnvironmentVariableTarget.Machine", e);
            }

            try
            {
                return VarRegexp.Replace(pattern,
                    m => MatchVarEvaluator(m, captureDetails, processVars, userVars, machineVars, filenameSafeMode)
                );
            }
            catch (Exception e)
            {
                // adding additional data for bug tracking
                if (captureDetails != null)
                {
                    e.Data.Add("title", captureDetails.Title);
                }

                e.Data.Add("pattern", pattern);
                throw;
            }
        }

        /// <summary>
        /// Checks whether a directory name is valid in the current file system
        /// </summary>
        /// <param name="directoryName">directory name (not path!)</param>
        /// <returns>true if directory name is valid</returns>
        public static bool IsDirectoryNameValid(string directoryName)
        {
            var forbiddenChars = Path.GetInvalidPathChars();
            foreach (var forbiddenChar in forbiddenChars)
            {
                if (directoryName == null || directoryName.Contains(forbiddenChar.ToString()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a filename is valid in the current file system
        /// </summary>
        /// <param name="filename">name of the file</param>
        /// <returns>true if filename is valid</returns>
        public static bool IsFilenameValid(string filename)
        {
            var forbiddenChars = Path.GetInvalidFileNameChars();
            foreach (var forbiddenChar in forbiddenChars)
            {
                if (filename == null || filename.Contains(forbiddenChar.ToString()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}