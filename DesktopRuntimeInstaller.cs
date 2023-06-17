using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Westwind.Utilities;

namespace DotnetDesktopRuntimeInstaller
{
    /// <summary>
    /// Console based downloader that downloads and installs the .NET Desktop Runtime
    /// if it is not already installed.
    ///
    /// * Checks if the Minimum Runtime version is installed by  checking RT install folder
    /// * If not installed prompts to download and install
    /// * Downloads the installer to the Downloads folder
    /// * Prompts to install the runtime
    /// * Runs the runtime installer as admin
    /// * Optionally can run silently (-silent) w/o prompting
    ///
    /// </summary>
    internal class DesktopRuntimeInstaller
    {

        /// <summary>
        /// Checks runtime version, downloads and installs runtime if not installed.
        /// </summary>
        /// <param name="isSilent">
        /// if true doesn't prompt for confirmation of download and install
        /// Use -silent when calling from an installer or other automated process
        /// </param>
        /// <returns></returns>
        public static bool CheckAndInstallRuntime(bool isSilent = false)
        {
            if (RuntimeVersionCheck())
            {
                ConsoleWrite(
                    $".NET Desktop Runtime v{RuntimeConfiguration.MinDotnetRuntimeVersion} or later is already installed.",
                    ConsoleColor.Green);
                return true;
            }

            var text =
                $"{RuntimeConfiguration.ApplicationName} requires the .NET Desktop Runtime v{RuntimeConfiguration.MinDotnetRuntimeVersion} or later.";
            var line = new string('-', text.Length);
            ConsoleWrite($"{line}\n{text}\n{line}\n", ConsoleColor.Red);

            if (isSilent)
            {
                DownloadAndInstall(true);
            }
            else
            {
                ConsoleWrite("Would you like to open to download and install the runtime now? [Y]/[n] ",
                    ConsoleColor.Yellow);
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
                {
                    DownloadAndInstall();
                }
            }

            return true;
        }

        private static bool DownloadAndInstall(bool isSilent = false)
        {
            var url = RuntimeConfiguration.DesktopRuntimeDownloadUrl;
            var filename = Path.GetFileName(url);
            
            string pattern = @"(\d+\.\d+\.\d+)";

            var dlFolder =
                Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders",
                    "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
            var dlPath = Path.Combine(dlFolder, filename);

            ConsoleWrite("==> Downloading.NET Desktop Runtime Installer", ConsoleColor.DarkCyan);

            ConsoleWrite($@"
from:
{url}
to:
{dlPath}
", ConsoleColor.DarkGray);

            Console.WriteLine("Please wait while we download...");
            try
            {
                var client = new WebClient();
                client.DownloadFile(url, dlPath);
            }
            catch (Exception ex)
            {
                ConsoleWrite("Download failed: " + ex.GetBaseException().Message, ConsoleColor.Red);
                return false;
            }

            ConsoleWrite("Download complete.", ConsoleColor.DarkYellow);

            if (!string.IsNullOrEmpty(RuntimeConfiguration.DownloadExeSha512) && 
                !CheckFileSha512(dlPath, RuntimeConfiguration.DownloadExeSha512))
            {
                ConsoleWrite($"File Integrity check based on SHA512 Hash failed.\nexpected: {RuntimeConfiguration.DownloadExeSha512}\n  actual: {ComputedFileHash}", ConsoleColor.Red);
                return false;
            }
            ConsoleWrite("File integrity SHA512 hash matches.", ConsoleColor.DarkYellow);

            if (!isSilent)
            {
                ConsoleWrite(
                    "\nWould you like to install the runtime now? [Y]/[n] (Yes installs, no opens in Explorer)",
                    ConsoleColor.Yellow);

                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.Enter)
                {
                    ShellUtils.OpenFileInExplorer(dlPath);
                    return true;
                }
            }


            string dlVersion = RuntimeConfiguration.MinDotnetRuntimeVersion;
            Match match = Regex.Match(filename, pattern);
            if (match.Success)
            {
                dlVersion = match.Groups[1].Value;
            }
            ConsoleWrite($"\n==> Installing Runtime .NET Desktop Runtime v{dlVersion}...\n",
                ConsoleColor.DarkCyan);

            try
            {
                ShellUtils.ExecuteCommandLine(dlPath + (isSilent ? " /quiet" : null), null, verb: "RunAs", waitForExitMs: 180000);
            }
            catch (Exception ex)
            {
                ConsoleWrite("Runtime Installation failed: " + ex.GetBaseException().Message, ConsoleColor.Red);
                return false;
            }

            ConsoleWrite("\nInstallation complete.", ConsoleColor.Green);

            return true;
        }

        private static void ConsoleWrite(string text, ConsoleColor color = ConsoleColor.Yellow)
        {
            var oldColor = Console.ForegroundColor;
            if (color == oldColor)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = oldColor;
            }
        }

        private static bool RuntimeVersionCheck()
        {
            var desktopRuntimePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"dotnet\shared\Microsoft.WindowsDesktop.App"
            );

            if (!Directory.Exists(desktopRuntimePath))
            {
                return false;
            }

            bool found = Directory.GetDirectories(desktopRuntimePath).OrderByDescending(d => d)
                .Any(d =>
                {
                    try
                    {
                        var dirName = Path.GetFileName(d);
                        var res = dirName.StartsWith(RuntimeConfiguration.MinDotnetRuntimeVersion.Substring(0, 2)) &&
                                  new Version(dirName) >= new Version(RuntimeConfiguration.MinDotnetRuntimeVersion);
                        return res;
                    }
                    catch
                    {
                        return false;
                    }
                });
            return found;
        }

        private static string ComputedFileHash = string.Empty;

        private static bool CheckFileSha512(string filePath, string providedHash)
        {
            if (string.IsNullOrEmpty(providedHash))
                return true;

            using (var sha512 = SHA512.Create())
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] fileHash = sha512.ComputeHash(fileStream);
                    ComputedFileHash = BitConverter.ToString(fileHash).Replace("-", "").ToLower();

                    return string.Equals(ComputedFileHash, providedHash, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }
}
