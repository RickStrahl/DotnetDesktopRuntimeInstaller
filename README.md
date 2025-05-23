# Dotnet 9.0 Desktop and Windows Hosting Runtime Checker and Installer

This project creates a tiny Windows Console executable that can be shipped with a .NET Core application **to ensure that a minimum version of the Desktop or Windows Hosting Bundle Runtime is installed**. 

The idea is that you can build a custom version of this CLI executable and ship it with your application, and fire it from your installer or install script to install the required minimum runtime if not already present. The tool checks for the minimum version, and if not present downloads and installs it.

> You can also customize this project to act as a launcher that checks for the Runtime and installs it if not present and then forwards the command line to your main .NET Core executable. But this is not built into this project.

The executable does the following:

* Checks for a minimum .NET Desktop Runtime Version or Windows Hosting Bundle (v8.0 currently)
* If already installed does nothing and exits
* If not installed prompts to download the latest runtime (latest patch)
* Prompts to install the downloaded runtime running as Admin
* Optionally can run silently using the `-silent` commandline switch
* Runs on .NET 4.72 so executable is a single file that is 100% self contained

Here's what it looks like in interactive mode:

![](ScreenShot.png)

## Silent Mode
The figure above shows the installer running in **interactive mode**, which prompts for download, and installation explicitly and shows the .NET Runtime installer interactively.

If you'd rather run the installer silently, for example when you are calling it from an installer or script, you can use the `-silent` command line switch switch:

```ps
.\DotnetDesktopRuntimeInstaller -silent
```

This removes all prompts from the Console app and runs the .NET Runtime installer silently.

## It's a Sample Project - You need to Customize it!
This project is meant as a sample template project that you can customize and rename or copy into your own solutions for distribution. It works as is in a pinch but you likely want to customize the prompts and potentially the versions used in the configuration. We'll try to keep up with new releases and update to the latest runtimes, but no guarantees.

You can and probably should rename the executable to an application specific launcher name or integrate it into an existing launcher. For example, I have this code integrated into a `mm.exe` which is the Markdown Monster alternate launcher that otherwise forwards commands to the main executable, but performs several support tasks and added a `-runtimeinstall` command line switch to execute the code in `program.cs`.

## How does it work?
As mentioned this project is a Console project so it compiles into an EXE that shows the Windows console when it executes. All output and input prompts are handled through the Console.

Using the class is literally a single line of code:

```cs
static void Main(string[] args)
{
    if (!DesktopRuntimeInstaller.CheckAndInstallRuntime(Environment.CommandLine.Contains("-silent")))
    {
        Environment.Exit(1);
    }
}
```        

The installer returns `true` if:

* The runtime is already installed
* The runtime was successfully installed

It returns false:

* If the installation failed or was aborted

## Configuration
In order for this tool to work **you have to specify a few things** that determine what versions are checked for and are downloaded. You need to specify:


* An application name (for prompts)
* A minimum version you need to have supported (ie. 9.0.4)
* A direct download URL (from MS site)  
<small><i>([from here](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) - choose x64 Desktop Runtime and follow to the download link)</i></small>
* An optional SHA512 has of the file content
* An optional Download URL for manual user downloading

You can configure these values in `DesktopRuntimeInstaller.cs` in the `DesktopRuntimeConfiguration` class:

```csharp
internal class DesktopRuntimeConfiguration
{
    internal static string ApplicationName = "Markdown Monster";

    /// <summary>
    /// Minimum version of the Desktop Runtime that's supported.
    ///
    /// Launcher checks for installed version and if lower prompts
    /// to install it.
    /// </summary>
    internal static string MinDotnetRuntimeVersion { get; } = "9.0.0";

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
    internal static string DesktopRuntimeDownloadUrl { get; } =
        "https://download.visualstudio.microsoft.com/download/pr/342ba160-3776-4ffa-91dd-e3cd9dc0f817/ba649d6b80b27ca164d80bd488cdb51f/windowsdesktop-runtime-7.0.7-win-x64.exe";

    /// <summary>
    /// Optional SHA512 hash of the downloaded file to verify the file integrity.
    ///
    /// Checked only if the value is set.
    ///
    /// This value is also displayed on the Microsoft download page along with the download link
    /// </summary>
    internal static string DownloadExeSha512 { get; } =
        "d59e8716324bd8973a95a9fd42a58ad0b176f4b37e8b8ba732be0e13d6f7ffdea79a52aa98363ec86860d551e124bdfaf71ac979b8f41f398e668fd12aa8483e";

    /// <summary>
    /// Url to the latest Desktop Runtime Download Page.
    /// </summary>
    internal static string ManualDownloadPage { get; } =   
        "https://dotnet.microsoft.com/download/dotnet/7.0/runtime?cid=getdotnetcore&runtime=desktop&os=windows&arch=x64";
}
```

I'd recommended that you check for the **lowest viable runtime version** that your application can run with, but **always download and install the latest patch version** specified in the download Url. So for example, my application works with 9.0.0, but I install the latest which is 9.0.4. 

This is preferable over always checking for the latest version, as a valid runtime may already be installed rather than downloading the very latest. This helps reduce piling up every singe runtime installations on the user's machine as well. Windows 11 has also started to update .NET Core to newer patch versions, if they are already installed.

You can find the latest 7.0 version download URL at this dynamic link:

[Latest .NET 9.0 Desktop Runtime Download](https://dotnet.microsoft.com/download/dotnet/9.0/runtime?cid=getdotnetcore&runtime=desktop&os=windows&arch=x64)

On this page you find:

* A direct download link
* The SHA512 hash


## Windows Hosting Bundle Runtime Installation
There's also similar support for the Windows Hosting Bundle Runtime installation using the `WindowsHostingBundleInstaller`. It works the same ways as the DesktopRuntimeInstaller with a different class name. 

This is a less likely use case as server installations to deployed applications tend to be less common, but it's come up on a few occasions for me when creating turnkey Web installers for clients. It gives the option of installing the hosting bundle as a utility process even if no installer is used to launch it from automatically.

## Resources


* [Related Blog Post](https://weblog.west-wind.com/posts/2023/Jun/21/Getting-the-NET-Desktop-Runtime-Installed-with-a-Custom-Runtime-Checker-and-Installer)
