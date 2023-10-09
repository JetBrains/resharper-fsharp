using JetBrains.Application.Build.Validation;
using JetBrains.Build;
using JetBrains.ReSharper.Plugins.FSharp.RiderPlugin.BuildScript;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.RiderPlugin.Install;

public static class ExemptFromZoningValidation
{
    [BuildStep]
    public static ZonesValidationResult.PackageZoningInvalid[] FSharpZoningIsInvalid()
    {
        // FIXME(k15tfu): Temporarily ignore zone validation errors for JetBrains.ReSharper.Plugins.FSharp.*.  See https://youtrack.jetbrains.com/issue/RIDER-99672.
        return new[]
        {
            new ZonesValidationResult.PackageZoningInvalid(new((RelativePath)"Plugins" / "resharper-fsharp" / "ReSharper.FSharp" / "src" / "FSharp")),
            new ZonesValidationResult.PackageZoningInvalid(new((RelativePath)"Plugins" / "resharper-fsharp" / "ReSharper.FSharp" / "test" / "src"))
        };
    }
}