using System;
using System.Threading;

namespace DotnetRuntimeInstaller
{

    /// <summary>
    /// DotnetRuntimeInstaller [-silent] [-desktop] [-web]
    /// 
    /// Examples: 
    /// 
    /// DotnetRuntimeInstaller                             // uses default mode (web)
    /// DotnetRuntimeInstaller -silent                     // uses default mode (web)
    /// DotnetRuntimeInstaller -web -silent                // explicit mode
    /// DotnetRuntimeInstaller -desktop -silent            // explicit mode
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            bool isSilent = Environment.CommandLine.Contains("-silent");
            
            string mode = "web";  // default

            if (Environment.CommandLine.Contains("-desktop"))
                mode = "desktop";
            else if (Environment.CommandLine.Contains("-web"))
                mode = "web";

            if (mode == "web")
            {
                if (WindowsHostingBundleInstaller.CheckAndInstallRuntime(isSilent))
                {
                    if (!isSilent)
                        Thread.Sleep(2000);
                    Environment.Exit(1);
                }
            }
            else if (mode == "desktop")
            {
                if (!DesktopRuntimeInstaller.CheckAndInstallRuntime(isSilent))
                {
                    if (!isSilent)
                        Thread.Sleep(2000);
                    Environment.Exit(1);
                }
            }

            // display final message
            if (!isSilent)
                Thread.Sleep(2000);
        }
    }
}