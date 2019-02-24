using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpTypeMember : IFSharpTypeParametersOwner, ITypeMember
  {
    [CanBeNull] FSharpSymbol Symbol { get; }

    bool IsVisibleFromFSharp { get; }
    bool CanNavigateTo { get; }

    bool IsExtensionMember { get; }
    bool IsFSharpMember { get; }
  }

  public interface IFSharpTypeParametersOwner : IFSharpDeclaredElement
  {
    IList<ITypeParameter> GetAllTypeParameters();
  }

  public interface IFSharpExtensionTypeMember : IFSharpTypeMember
  {
    [CanBeNull] FSharpEntity ApparentEntity { get; }
  }
}
