using System.Collections.Generic;
using System.Diagnostics;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpTypeElementDeclarationBase : FSharpCachedDeclarationBase, IFSharpTypeElementDeclaration
  {
    ITypeMember ITypeMemberDeclaration.DeclaredElement => (ITypeMember) DeclaredElement;
    ITypeElement ITypeDeclaration.DeclaredElement => (ITypeElement) CacheDeclaredElement;

    /// May take long time due to waiting for FCS.
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IEnumerable<IDeclaredType> SuperTypes => GetFSharpSymbol() is FSharpEntity entity
      ? FSharpTypesUtil.GetSuperTypes(entity, TypeParameters, GetPsiModule())
      : EmptyList<IDeclaredType>.Instance;

    /// May take long time due to waiting for FCS.
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IDeclaredType BaseClassType => GetFSharpSymbol() is FSharpEntity entity
      ? entity.MapBaseType(TypeParameters, GetPsiModule())
      : null;

    public override FSharpSymbol GetFSharpSymbol()
    {
      var symbol = base.GetFSharpSymbol();
      if (symbol is FSharpEntity || symbol is FSharpUnionCase)
        return symbol;

      return symbol is FSharpMemberOrFunctionOrValue mfv && (mfv.IsConstructor || mfv.IsImplicitConstructor)
        ? mfv.DeclaringEntity?.Value
        : null;
    }

    [NotNull]
    private IList<ITypeParameter> TypeParameters =>
      ((ITypeDeclaration) this).DeclaredElement?.TypeParameters ??
      EmptyList<ITypeParameter>.Instance;

    public virtual IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations
    {
      get
      {
        var result = new List<ITypeMemberDeclaration>();
        ProcessChildren(result, this);
        return result.AsReadOnly();
      }
    }

    private static void ProcessChildren(List<ITypeMemberDeclaration> result, ITreeNode node)
    {
      foreach (var child in node.Children())
      {
        if (child is ITypeMemberDeclaration m)
          result.Add(m);

        if (child is IInterfaceImplementation interfaceImplementation)
          foreach (var implementedMember in interfaceImplementation.TypeMembers)
            result.Add(implementedMember);

        if (child is ITypeDeclarationGroup typeDeclarationGroup)
          foreach (var typeDeclaration in typeDeclarationGroup.TypeDeclarations)
          {
            if (typeDeclaration is ITypeExtensionDeclaration { IsTypePartDeclaration: false } typeExtension)
            {
              foreach (var extensionMember in typeExtension.TypeMembers)
                if (extensionMember is ITypeMemberDeclaration typeMemberDeclaration)
                  result.Add(typeMemberDeclaration);
            }
            else
              result.Add(typeDeclaration);
          }

        if (child is IMemberSignatureOrDeclaration {IsIndexer: false} memberDeclaration)
          foreach (var accessor in memberDeclaration.AccessorDeclarations)
            if (accessor.IsExplicit)
              result.Add(accessor);

        if (child is ILetBindingsDeclaration let)
          foreach (var binding in let.Bindings)
            ProcessBinding(binding, result);

        if (child is IBindingSignature bindingSignature)
          ProcessBinding(bindingSignature, result);

        if (child is IUnionCaseFieldDeclarationList fieldDeclarationList)
          result.AddRange(fieldDeclarationList.FieldsEnumerable);

        if (child is ITypeMemberDeclarationList typeMemberDeclarationList)
          ProcessChildren(result, typeMemberDeclarationList);

        if (child is IMemberDeclarationList memberDeclarationList)
          ProcessChildren(result, memberDeclarationList);
      }
    }

    private static void ProcessBinding(IBindingLikeDeclaration binding, ICollection<ITypeMemberDeclaration> result)
    {
      var headPattern = binding.HeadPattern;
      if (headPattern == null) return;

      foreach (var declaration in headPattern.NestedPatterns)
        if (declaration is ITypeMemberDeclaration typeMemberDeclaration)
          result.Add(typeMemberDeclaration);
    }

    public string CLRName => this.MakeClrName();
    public IReadOnlyList<ITypeDeclaration> TypeDeclarations => EmptyList<ITypeDeclaration>.Instance;
    public IEnumerable<ITypeDeclaration> TypeDeclarationsEnumerable => NestedTypeDeclarations;
    public IEnumerable<ITypeDeclaration> NestedTypeDeclarationsEnumerable => NestedTypeDeclarations;

    public IReadOnlyList<ITypeDeclaration> NestedTypeDeclarations
    {
      get
      {
        if (!(this is IModuleDeclaration))
          return EmptyList<ITypeDeclaration>.Instance;

        var result = new LocalList<ITypeDeclaration>();
        foreach (var memberDeclaration in this.Children())
        {
          if (memberDeclaration is ITypeDeclaration typeDeclaration)
            result.Add(typeDeclaration);
        }

        return result.ReadOnlyList();
      }
    }

    public virtual PartKind TypePartKind => PartKind.Class;

    public override void SetName(string name, ChangeNameKind changeNameKind)
    {
      var oldSourceName = SourceName;
      base.SetName(name, changeNameKind);

      var module =
        ModuleLikeDeclarationNavigator.GetByMember(
          this as IModuleMember ??
          TypeDeclarationGroupNavigator.GetByTypeDeclaration(this as IFSharpTypeOrExtensionDeclaration));

      if (module == null)
        return;

      foreach (var member in module.Members)
      {
        if (!(member is IModuleDeclaration moduleDeclaration))
          continue;

        if (moduleDeclaration.SourceName != oldSourceName && moduleDeclaration.SourceName != name)
          continue;

        var element = member as CompositeElement;
        element?.SubTreeChanged(element, PsiChangedElementType.SourceContentsChanged);
      }
    }
  }
}
