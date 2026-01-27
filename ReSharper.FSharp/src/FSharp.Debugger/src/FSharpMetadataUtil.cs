using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;

namespace JetBrains.ReSharper.Plugins.FSharp.Debugger;

public static class FSharpMetadataUtil
{
  public static bool IsFSharpTypeFuncSpecialize([CanBeNull] this IMetadataMethod method)
  {
    if (method?.Name is not "Specialize")
      return false;

    var declaringType = method.DeclaringType;
    if (declaringType.Assembly is not { AssemblyName.Name: "FSharp.Core" }) return false;

    return declaringType.TypeName == "FSharpTypeFunc";
  }
}
