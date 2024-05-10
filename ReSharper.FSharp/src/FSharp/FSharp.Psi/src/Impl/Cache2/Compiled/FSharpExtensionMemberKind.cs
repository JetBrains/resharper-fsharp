using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;

public class FSharpExtensionMemberKind : ExtensionMemberKind
{
  private FSharpExtensionMemberKind([NotNull] string name) : base(name)
  {
  }

  public static readonly ExtensionMemberKind FSharpExtensionMember = new("FSharpExtensionMember");
}
