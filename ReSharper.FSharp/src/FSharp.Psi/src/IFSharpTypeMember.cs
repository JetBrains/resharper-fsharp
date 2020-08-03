using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpTypeMember : IFSharpTypeParametersOwner, ITypeMember
  {
    [CanBeNull] FSharpSymbol Symbol { get; }

    bool IsVisibleFromFSharp { get; }
    bool CanNavigateTo { get; }

    bool IsExtensionMember { get; }
  }

  public interface IFSharpMember : IFSharpTypeMember
  {
    [CanBeNull] FSharpMemberOrFunctionOrValue Mfv { get; }
  }
  
  public interface IFSharpTypeParametersOwner : IFSharpDeclaredElement
  {
    IList<ITypeParameter> AllTypeParameters { get; }
  }

  public interface IFSharpGeneratedFromOtherElement : IFSharpDeclaredElement
  {
    [NotNull] IClrDeclaredElement OriginElement { get; }
    IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer();
  }
}
