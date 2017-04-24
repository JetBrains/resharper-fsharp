using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class LocalDeclaration : ITypeOwner
  {
    public override IDeclaredElement DeclaredElement => this;
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public IList<IDeclaration> GetDeclarations()
    {
      return new IDeclaration[] {this};
    }

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return SharedImplUtil.GetDeclarationsIn(this, sourceFile);
    }

    public DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.LOCAL_VARIABLE;
    }

    public XmlNode GetXMLDescriptionSummary(bool inherit)
    {
      return null;
    }

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;

    public ITypeElement GetContainingType()
    {
      return GetContainingNode<ITypeDeclaration>()?.DeclaredElement;
    }

    public ITypeMember GetContainingTypeMember()
    {
      return GetContainingNode<ITypeMemberDeclaration>()?.DeclaredElement;
    }

    public IPsiModule Module => GetPsiModule();
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

    public IType Type
    {
      get
      {
        var mfv = Symbol as FSharpMemberOrFunctionOrValue;
        if (mfv == null)
          return TypeFactory.CreateUnknownType(Module);

        var typeMemberDeclaration = GetContainingNode<ITypeMemberDeclaration>();
        Assertion.AssertNotNull(typeMemberDeclaration, "typeMemberDeclaration != null");
        return FSharpTypesUtil.GetType(mfv.FullType, typeMemberDeclaration, Module) ??
               TypeFactory.CreateUnknownType(Module);
      }
    }
  }
}