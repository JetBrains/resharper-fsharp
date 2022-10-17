using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.SymbolCache;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpDeclaredElement : IClrDeclaredElement
  {
    // todo: add Symbol property
    string SourceName { get; }
  }

  public interface IFSharpTypeElement : IFSharpDeclaredElement, ITypeElement
  {
  }

  public interface IFSharpCompiledTypeElement : ICompiledTypeElement, IFSharpDeclaredElement,
    IAlternativeNameCacheTrieNodeOwner
  {
  }

  public interface IFSharpParameter : IParameter, IFSharpDeclaredElement
  {
    (int group, int index) Position { get; }
    bool IsErased { get; }
    bool IsGenerated { get; }
    [CanBeNull] FSharpParameter Symbol { get; }
  }

  public interface IFSharpParameterDeclaration : ITreeNode // IParameterDeclaration, IFSharpDeclaration
  {
    string ShortName { get; }
    (int group, int index) Position { get; }
    IFSharpParameterOwnerDeclaration OwnerDeclaration { get; }
    IList<IFSharpParameter> Parameters { get; }
  }

  public interface IFSharpParameterDeclarationGroup
  {
    [CanBeNull] IFSharpParameterDeclaration GetParameterDeclaration(int index);
    [NotNull] IList<IFSharpParameterDeclaration> ParameterDeclarations { get; }
    IList<IFSharpParameter> GetOrCreateParameters([NotNull] IList<FSharpParameter> fcsParams);
  }

  public interface IFSharpParameterOwnerDeclaration : ITreeNode //: IParametersOwnerDeclaration
  {
    [NotNull] IList<IFSharpParameterDeclarationGroup> ParameterGroups { get; }
    [CanBeNull] IFSharpParameterDeclaration GetParameter((int group, int index) position);
  }

  public interface IFSharpPatternParametersOwnerDeclaration : IFSharpParameterOwnerDeclaration
  {
    // IList<IParametersPatternDeclaration> ParameterPatternsDeclarations { get; }
  }
}
