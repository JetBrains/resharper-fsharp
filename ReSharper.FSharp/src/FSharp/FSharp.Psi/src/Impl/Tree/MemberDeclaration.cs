using System;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MemberDeclaration : IFunctionDeclaration
  {
    IFunction IFunctionDeclaration.DeclaredElement => base.DeclaredElement as IFunction;
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);

    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    protected override FSharpSymbolUse GetSymbolDeclaration(TreeTextRange identifierRange) =>
      ObjExprNavigator.GetByMember(this) != null
        ? FSharpFile.GetSymbolUse(identifierRange.StartOffset.Offset)
        : base.GetSymbolDeclaration(identifierRange);

    protected override IDeclaredElement CreateDeclaredElement()
    {
      if (ParametersDeclarationsEnumerable.Any())
        return this.CreateMethod();

      return GetFcsSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;
    }

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol) =>
      this.CreateMemberDeclaredElement(fcsSymbol);

    public override bool IsExplicitImplementation => this.IsExplicitImplementation();
    public bool IsIndexer => this.IsIndexer();

    public override bool IsStatic =>
      StaticKeyword != null || TypeExtensionDeclarationNavigator.GetByTypeMember(this) != null;

    public override bool IsVirtual => MemberKeyword?.GetTokenType() == FSharpTokenType.DEFAULT;
    public override bool IsOverride => this.IsOverride();

    public override TreeTextRange GetNameIdentifierRange() =>
      NameIdentifier.GetNameIdentifierRange();

    public override void SetOverride(bool value)
    {
      if (value == IsOverride)
        return;

      if (!value)
        throw new NotImplementedException();

      ModificationUtil.ReplaceChild(MemberKeyword, FSharpTokenType.OVERRIDE.CreateLeafElement());
    }

    public override AccessRights GetAccessRights() => ModifiersUtil.GetAccessRights(AccessModifier);
    public ITreeNode Initializer => ChameleonExpression.Expression;
  }
}
