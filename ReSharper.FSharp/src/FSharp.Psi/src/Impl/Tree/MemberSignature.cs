using System;
using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MemberSignature
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode)Identifier;
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);

    protected override IDeclaredElement CreateDeclaredElement()
    {
      if (Parent is IMemberConstraint)
        return null;

      if (ReturnTypeInfo is { ReturnType: IFunctionTypeUsage })
        return this.CreateMethod();

      return GetFcsSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;
    }

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol) =>
      this.CreateMemberDeclaredElement(fcsSymbol);

    public bool IsIndexer => this.IsIndexer();

    public override bool IsStatic => StaticKeyword != null;
    public override bool IsVirtual => MemberKeyword?.GetTokenType() == FSharpTokenType.DEFAULT;
    public override bool IsOverride => this.IsOverride();

    public IList<IFSharpParameterDeclarationGroup> ParameterGroups => this.GetParameterGroups();
    public IList<IFSharpParameterDeclaration> ParameterDeclarations => this.GetParameterDeclarations();

    public IFSharpParameterDeclaration GetParameter((int group, int index) position) =>
      FSharpImplUtil.GetParameter(this, position);

    public IParameterDeclaration AddParameterDeclarationBefore(ParameterKind kind, IType parameterType,
      string parameterName, IParameterDeclaration anchor) =>
      throw new NotImplementedException();

    public IParameterDeclaration AddParameterDeclarationAfter(ParameterKind kind, IType parameterType,
      string parameterName, IParameterDeclaration anchor) =>
      throw new NotImplementedException();

    public void RemoveParameterDeclaration(int index) =>
      throw new NotImplementedException();

    public IParametersOwner DeclaredParametersOwner => (IParametersOwner)DeclaredElement;
    
    
  }
}
