using System;
using System.Diagnostics;

namespace DotnetDesktopRuntimeInstaller
{
    /// <summary>
    /// Configure the runtime version and download URL here
    /// for the version you want to check for and install.
    ///
    /// Note these two don't have to be the same. You can check for
    /// a lower version but always install the latest patch version.
    /// </summary>
    internal class RuntimeConfiguration
    {
        internal static string ApplicationName = "Markdown Monster";

        /// <summary>
        /// Minimum version of the Desktop Runtime that's supported.
        ///
        /// Launcher checks for installed version and if lower prompts
        /// to install it.
        /// </summary>
        internal static string MinDotnetRuntimeVersion { get;  } = "7.0.3";

        /// <summary>
        /// Direct download URL for the .NET Desktop Runtime Installer.
        /// 
        /// Recommend you update this link to the latest available patch version so if you need to install
        /// you are installing the latest, not an older version.
        /// MM allows patch roll forward meaning later patches work but you don't install if a
        /// a compatible version is already installed.
        /// 
        /// Get this URL from the Microsoft .NET download site:
        /// https://dotnet.microsoft.com/en-us/download/dotnet/7.0 (Download x64 Desktop Runtime)
        /// </summary>
        internal static string DesktopRuntimeDownloadUrl { get;  } =
            "https://download.visualstudio.microsoft.com/download/pr/342ba160-3776-4ffa-91dd-e3cd9dc0f817/ba649d6b80b27ca164d80bd488cdb51f/windowsdesktop-runtime-7.0.7-win-x64.exe";

        /// <summary>
        /// Optional SHA512 hash of the downloaded file to verify the file integrity.
        ///
        /// Checked only if the value is set.
        /// </summary>
        internal static string DownloadExeSha512 { get; } = "d59e8716324bd8973a95a9fd42a58ad0b176f4b37e8b8ba732be0e13d6f7ffdea79a52aa98363ec86860d551e124bdfaf71ac979b8f41f398e668fd12aa8483e";
    }


    internal class Program
    {
        static void Main(string[] args)
        {
            if (!DesktopRuntimeInstaller.CheckAndInstallRuntime(Environment.CommandLine.Contains("-silent")))
            {
                Environment.Exit(1);
            }
        }
    }
}