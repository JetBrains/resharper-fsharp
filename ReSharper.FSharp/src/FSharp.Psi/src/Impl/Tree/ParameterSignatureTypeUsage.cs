using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ParameterSignatureTypeUsage
  {
    // todo: override getting symbol: get it from method

    public (int group, int index) Position
    {
      get
      {
        var tupleTypeUsage = TupleTypeUsageNavigator.GetByItem(this);
        var index = tupleTypeUsage?.Items.IndexOf(this) ?? 0;

        var topLevelTypeUsage = (ITypeUsage)tupleTypeUsage ?? this;
        var group = 0;
        var funTypeUsage = FunctionTypeUsageNavigator.GetByArgumentTypeUsage(topLevelTypeUsage);
        while (funTypeUsage != null)
        {
          group++;
          funTypeUsage = FunctionTypeUsageNavigator.GetByArgumentTypeUsage(funTypeUsage);
        }

        return (group, index);
      }
    }

    public IFSharpParameterOwnerDeclaration OwnerDeclaration => GetContainingNode<IFSharpParameterOwnerDeclaration>();
    public IList<IFSharpParameter> Parameters => new[] { (IFSharpParameter)DeclaredElement };

    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public string ShortName => DeclaredElementName;
    protected override string DeclaredElementName => Identifier.GetCompiledName();

    protected override IDeclaredElement CreateDeclaredElement() => new FSharpSignatureParameter(this, Position);
    public void SetType(IType type)
    {
      throw new NotImplementedException();
    }

    public IType Type =>
      DeclaredElement is ITypeOwner typeOwner
        ? typeOwner.Type
        : TypeFactory.CreateUnknownType(GetPsiModule());

    public override FSharpSymbol GetFcsSymbol() =>
      DeclaredElement is IFSharpParameter fsParam ? fsParam.Symbol : null;

    public override FSharpSymbolUse GetFcsSymbolUse() => null;
  }

  internal class ParameterSignatureGroup : IFSharpParameterDeclarationGroup
  {
    public ParameterSignatureGroup(IList<IFSharpParameterDeclaration> paramDecls) =>
      ParameterDeclarations = paramDecls;

    public IFSharpParameterDeclaration GetParameterDeclaration(int index) =>
      FSharpImplUtil.GetParameterDeclaration(this, index);

    public IList<IFSharpParameterDeclaration> ParameterDeclarations { get; }

    public IList<IFSharpParameter> GetOrCreateParameters(IList<FSharpParameter> fcsParams) =>
      ParameterDeclarations
        .Select(paramDecl => ((IParameterSignatureTypeUsage)paramDecl).DeclaredElement as IFSharpParameter).AsIList();
  }
}
