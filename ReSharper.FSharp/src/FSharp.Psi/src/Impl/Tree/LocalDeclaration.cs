using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalDeclaration : ITypeOwner, IFSharpDeclaredElement
  {
    public override IDeclaredElement DeclaredElement => this;
    public override string DeclaredName => Identifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => Identifier;
    public IList<IDeclaration> GetDeclarations() => new IDeclaration[] {this};

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) =>
      SharedImplUtil.GetDeclarationsIn(this, sourceFile);

    public DeclaredElementType GetElementType() => CLRDeclaredElementType.LOCAL_VARIABLE;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
    public ITypeElement GetContainingType() => GetContainingNode<ITypeDeclaration>()?.DeclaredElement;
    public ITypeMember GetContainingTypeMember() => GetContainingNode<ITypeMemberDeclaration>()?.DeclaredElement;
    public IPsiModule Module => GetPsiModule();
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public IType Type
    {
      get
      {
        if (!(GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv))
          return TypeFactory.CreateUnknownType(Module);

        var typeMemberDeclaration = GetContainingNode<ITypeMemberDeclaration>();
        Assertion.AssertNotNull(typeMemberDeclaration, "typeMemberDeclaration != null");
        return FSharpTypesUtil.GetType(mfv.FullType, typeMemberDeclaration, Module) ??
               TypeFactory.CreateUnknownType(Module);
      }
    }

    public override void SetName(string name) =>
      Identifier.ReplaceIdentifier(name);
  }
}
