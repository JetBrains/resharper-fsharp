using System;
using JetBrains.Application.BuildScript.Install;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.FSharp.BuildScript;

public class RequestAppConfigInstall
{
    [BuildStep]
    public static InstallAppConfig[] Run(AllAssembliesOnEverything allass)
    {
        return allass.FindSubplatformByClass<RequestAppConfigInstall>() is SubplatformOnSources subplatform ? new[] {new InstallAppConfig(subplatform.Name, "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.csproj")} : Array.Empty<InstallAppConfig>();
    }
}