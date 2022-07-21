using System.Collections.Generic;
using FSharp.Compiler.Symbols;
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
  }

  public interface IFSharpMember : IFSharpTypeMember, IOverridableMember
  {
    [CanBeNull] FSharpMemberOrFunctionOrValue Mfv { get; }
  }

  public interface IFSharpTypeParametersOwner : IFSharpDeclaredElement
  {
    IList<ITypeParameter> AllTypeParameters { get; }
  }

  public interface ISecondaryDeclaredElement
  {
    [NotNull] IClrDeclaredElement OriginElement { get; }
    bool IsReadOnly { get; }
  }

  public interface IFSharpGeneratedElement : IFSharpDeclaredElement
  {
  }

  public interface IFSharpGeneratedFromOtherElement : IFSharpGeneratedElement, ISecondaryDeclaredElement
  {
    IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer();
  }

  public interface IFSharpGeneratedFromUnionCase : IFSharpGeneratedFromOtherElement
  {
  }
}
