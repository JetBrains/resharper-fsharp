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
  internal abstract class LocalDeclarationBase : FSharpDeclarationBase, ITypeOwner, IFSharpDeclaredElement,
    IFSharpLocalDeclaration
  {
    public override IDeclaredElement DeclaredElement => this;

    string IDeclaredElement.ShortName => SourceName;
    public override string CompiledName => SourceName;

    public IList<IDeclaration> GetDeclarations() => new IDeclaration[] {this};

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) =>
      SharedImplUtil.GetDeclarationsIn(this, sourceFile);

    public DeclaredElementType GetElementType() => CLRDeclaredElementType.LOCAL_VARIABLE;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
    public ITypeElement GetContainingType() => GetContainingNode<ITypeDeclaration>()?.DeclaredElement;
    public ITypeMember GetContainingTypeMember() => ContainingMember;
    public IPsiModule Module => GetPsiModule();
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public ITypeMember ContainingMember =>
      GetContainingNode<ITypeMemberDeclaration>()?.DeclaredElement;

    public IType Type
    {
      get
      {
        if (!(GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv))
          return TypeFactory.CreateUnknownType(Module);

        var typeParameters =
          ContainingMember is IFSharpTypeParametersOwner parametersOwner
            ? parametersOwner.GetAllTypeParameters()
            : EmptyList<ITypeParameter>.Instance;

        return mfv.FullType.MapType(typeParameters, Module);
      }
    }

    public override void SetName(string name) =>
      NameIdentifier.ReplaceIdentifier(name);
  }

  internal partial class LocalDeclaration
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
  }

  internal abstract class LocalPatternDeclarationBase : LocalDeclarationBase
  {
    public TreeNodeCollection<IFSharpAttribute> Attributes =>
      TreeNodeCollection<IFSharpAttribute>.Empty;

    public TreeNodeEnumerable<IFSharpAttribute> AttributesEnumerable =>
      TreeNodeEnumerable<IFSharpAttribute>.Empty;
  }
}
