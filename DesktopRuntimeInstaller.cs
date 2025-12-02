using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Westwind.Utilities;

namespace DotnetRuntimeInstaller
{
    /// <summary>
    /// Configure the runtime version and download URL here
    /// for the version you want to check for and install.
    ///
    /// Note these two don't have to be the same. You can check for
    /// a lower version but always install the latest patch version.
    /// </summary>
    internal class DesktopRuntimeConfiguration
    {
        internal static string ApplicationName = "Markdown Monster";

        /// <summary>
        /// Minimum version of the Desktop Runtime that's supported.
        ///
        /// Launcher checks for installed version and if lower prompts
        /// to install it.
        /// </summary>
        internal static string MinDotnetRuntimeVersion => "10.0.0";

        /// <summary>
        /// Determines on whether preview versions are allowed to be used
        /// as pre-installed versions. If false preview releases are not
        /// allowed and a release version must be installed or downloaded.
        /// </summary>
        internal static bool AllowPreviewVersions => false;

        /// <summary>
        /// Direct download URL for the .NET Desktop Runtime Installer.
        /// 
        /// Recommend you update this link to the latest available patch version so if you need to install
        /// you are installing the latest, not an older version.
        /// MM allows patch roll forward meaning later patches work but you don't install if a
        /// compatible version is already installed.
        /// 
        /// Get this URL from the Microsoft .NET download site:
        /// https://dotnet.microsoft.com/en-us/download/dotnet/9.0 (Download x64 or Arm64 Desktop Runtime)
        /// </summary>
        internal static string DesktopRuntimeDownloadUrlX64 { get; } =
          "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.0/windowsdesktop-runtime-10.0.0-win-x64.exe";

        /// <summary>
        /// Optional SHA512 hash of the downloaded file to verify the file integrity.
        ///
        /// Checked only if the value is set.
        ///
        /// This value is also displayed on the Microsoft download page along with the download link
        /// </summary>
        internal static string Downloadx64ExeSha512 { get; } = "5c6dd5616f8365cc618087916bf9e0470f5fe61242010df040ce7a688acc0598797bce0a62d3915d8951dc70f89deb1e30223c1dc0918f05af92c88658f4b3ec";


        internal static string DesktopRuntimeDownloadUrlArm64 { get; } =
            "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.0/windowsdesktop-runtime-10.0.0-win-arm64.exe";

       
        internal static string DownloadArm64ExeSha512 { get; } = "6c8b3c89a43aed39cbb19968b6a9727c2cc5d15cca1b68c8486cb355df76fcaaa1209a0ad06cb431b226bc561f06e521098934c96cea7d11d6322a6a48fcc422";

        /// <summary>
        /// Url to the latest Desktop Runtime Download Page.
        /// </summary>
        internal static string ManualDownloadPage { get; } = "https://dotnet.microsoft.com/download/dotnet/10.0/runtime?cid=getdotnetcore&runtime=desktop&os=windows&arch={0}";
    }



    /// <summary>
    /// Console based downloader that downloads and installs the .NET Desktop Runtime
    /// if it is not already installed.
    ///
    /// * Checks if the Minimum Runtime version is installed by  checking RT install folder
    /// * If not installed prompts to download and install.
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
                    $".NET Desktop Runtime v{DesktopRuntimeConfiguration.MinDotnetRuntimeVersion} or later is already installed.",
                    ConsoleColor.Green);
                return true;
            }

            bool result = false;
            var text =
                $"{DesktopRuntimeConfiguration.ApplicationName} requires the .NET Desktop Runtime v{DesktopRuntimeConfiguration.MinDotnetRuntimeVersion} or later.";
            var line = new string('-', text.Length);
            ConsoleWrite($"{line}\n{text}\n{line}\n", ConsoleColor.Red);

            if (isSilent)
            {
                result = DownloadAndInstall(true);
            }
            else
            {
                ConsoleWrite("Would you like to open to download and install the runtime now? [Y]/[n] ([m]anually download and install)",
                    ConsoleColor.Yellow);
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.M)
                {
                    ShellUtils.GoUrl(DesktopRuntimeConfiguration.ManualDownloadPage + (IsArm64 ? "arm64" : "x64"));
                    return true;
                }
                if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
                {
                    result = DownloadAndInstall();
                }
            }


            return result;
        }


        private static bool DownloadAndInstall(bool isSilent = false)
        {
            var url = DesktopRuntimeConfiguration.DesktopRuntimeDownloadUrlX64;
            if (IsArm64)
                url = DesktopRuntimeConfiguration.DesktopRuntimeDownloadUrlArm64;

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

            // Validate Sha512 hash
            if (!IsArm64)
            {
                if (!string.IsNullOrEmpty(DesktopRuntimeConfiguration.Downloadx64ExeSha512) &&
                    !CheckFileSha512(dlPath, DesktopRuntimeConfiguration.Downloadx64ExeSha512))
                {
                    ConsoleWrite($"File Integrity check based on SHA512 Hash failed.\nexpected: {DesktopRuntimeConfiguration.Downloadx64ExeSha512}\n  actual: {ComputedFileHash}", ConsoleColor.Red);
                    return false;
                }
                ConsoleWrite("File integrity SHA512 hash matches.", ConsoleColor.DarkYellow);
            }
            else
            {
                if (!string.IsNullOrEmpty(DesktopRuntimeConfiguration.DownloadArm64ExeSha512) &&
                    !CheckFileSha512(dlPath, DesktopRuntimeConfiguration.DownloadArm64ExeSha512))
                {
                    ConsoleWrite($"File Integrity check based on SHA512 Hash failed.\nexpected: {DesktopRuntimeConfiguration.Downloadx64ExeSha512}\n  actual: {ComputedFileHash}", ConsoleColor.Red);
                    return false;
                }
                ConsoleWrite("File integrity SHA512 hash matches.", ConsoleColor.DarkYellow);
            }

            if (!isSilent)
            {
                ConsoleWrite(
                    "\nWould you like to install the runtime now? [Y]/[n] (Yes installs, No opens in Explorer)",
                    ConsoleColor.Yellow);

                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.Enter)
                {
                    ShellUtils.OpenFileInExplorer(dlPath);
                    return true;
                }
            }


            string dlVersion = DesktopRuntimeConfiguration.MinDotnetRuntimeVersion;
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
                        if (DesktopRuntimeConfiguration.AllowPreviewVersions && dirName.Contains("-"))
                            dirName = dirName.Substring(0, dirName.IndexOf('-'));  // strip preview info

                        var res = dirName.StartsWith(DesktopRuntimeConfiguration.MinDotnetRuntimeVersion.Substring(0, 2)) &&
                                  new Version(dirName) >= new Version(DesktopRuntimeConfiguration.MinDotnetRuntimeVersion);
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



        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool IsWow64Process2(IntPtr hProcess, out ushort processMachine, out ushort nativeMachine);
        const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;

        internal static bool IsArm64
        {
            get
            {
                if (IsWow64Process2(Process.GetCurrentProcess().Handle,
                        out ushort procArch,
                        out ushort osArch))
                {
                    if (osArch == IMAGE_FILE_MACHINE_ARM64)
                        return true;
                }
                return false;

                // this doesn't return the correct result in net472
                //return RuntimeInformation.OSArchitecture == Architecture.Arm64;
            }
        }
       

    }




}
