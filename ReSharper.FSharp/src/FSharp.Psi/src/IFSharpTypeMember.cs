using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpTypeMember : IFSharpDeclaredElement, ITypeMember
  {
    [CanBeNull] FSharpSymbol Symbol { get; }

    bool IsVisibleFromFSharp { get; }
    bool CanNavigateTo { get; }

    bool IsExtensionMember { get; }
    bool IsFSharpMember { get; }
  }

  public interface IFSharpExtensionTypeMember : IFSharpTypeMember
  {
    [CanBeNull] FSharpEntity ApparentEntity { get; }
  }
}
