using System;
using System.Diagnostics;

namespace DotnetDesktopRuntimeInstaller
{

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