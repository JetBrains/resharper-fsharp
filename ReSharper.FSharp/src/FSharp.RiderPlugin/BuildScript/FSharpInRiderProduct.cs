using System;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.PackageSpecification;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Rider.Backend.BuildScript;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.RiderPlugin.BuildScript;

/// <summary>
///   Defines a bundled plugin which drives adding the referenced packages as a plugin for Rider.
/// </summary>
public class FSharpInRiderProduct
{
  public static readonly SubplatformName ThisSubplatformName = new((RelativePath)"Plugins" / "resharper-fsharp" / "ReSharper.FSharp" / "src" / "FSharp.RiderPlugin");

  public static readonly RelativePath DotFilesFolder = @"plugins\rider-plugins-fsharp\DotFiles";

  public const string ProductTechnicalName = "FSharp";

  [BuildStep]
  public static SubplatformComponentForPackagingFast[] ProductMetaDependency(AllAssembliesOnSources allassSrc)
  {
    if (!allassSrc.Has(ThisSubplatformName))
      return Array.Empty<SubplatformComponentForPackagingFast>();

    return new[]
    {
      new SubplatformComponentForPackagingFast
      (
        ThisSubplatformName,
        new JetPackageMetadata
        {
          Spec = new JetSubplatformSpec
          {
            ComplementedProductName = RiderConstants.ProductTechnicalName
          }
        }
      )
    };
  }

  public class Fantomas
  {
    public static readonly SubplatformName FantomasSubplatformName = new((RelativePath)"Plugins" / "resharper-fsharp" / "ReSharper.FSharp" / "src" / "FSharp.Fantomas.Host");

    public static readonly RelativePath FantomasFolder = @"plugins\rider-plugins-fsharp\fantomas";

    public const string ProductTechnicalName = "FSharp.Fantomas";

    [BuildStep]
    public static SubplatformComponentForPackagingFast[] ProductMetaDependency(AllAssembliesOnSources allassSrc)
    {
      if (!allassSrc.Has(FantomasSubplatformName))
        return Array.Empty<SubplatformComponentForPackagingFast>();

      return new[]
      {
        new SubplatformComponentForPackagingFast
        (
          FantomasSubplatformName,
          new JetPackageMetadata
          {
            Spec = new JetSubplatformSpec
            {
              ComplementedProductName = RiderConstants.ProductTechnicalName
            }
          }
        )
      };
    }
  }

  public class TypeProviders
  {
    public static readonly SubplatformName TypeProvidersSubplatformName = new((RelativePath)"Plugins" / "resharper-fsharp" / "ReSharper.FSharp" / "src" / "FSharp.TypeProviders.Host");

    public static readonly RelativePath TypeProvidersFolder = @"plugins\rider-plugins-fsharp\typeProviders";

    public const string ProductTechnicalName = "FSharp.TypeProviders";

    [BuildStep]
    public static SubplatformComponentForPackagingFast[] ProductMetaDependency(AllAssembliesOnSources allassSrc)
    {
      if (!allassSrc.Has(TypeProvidersSubplatformName))
        return Array.Empty<SubplatformComponentForPackagingFast>();

      return new[]
      {
        new SubplatformComponentForPackagingFast
        (
          TypeProvidersSubplatformName,
          new JetPackageMetadata
          {
            Spec = new JetSubplatformSpec
            {
              ComplementedProductName = RiderConstants.ProductTechnicalName
            }
          }
        )
      };
    }
  }
}