using System.Collections.Generic;
using System.Diagnostics;
using FSharp.Compiler.SourceCodeServices;
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
    ITypeElement ITypeDeclaration.DeclaredElement => (ITypeElement) DeclaredElement;

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
        var result = new LocalList<ITypeMemberDeclaration>();
        foreach (var child in this.Children())
        {
          if (child is ITypeMemberDeclaration m)
            result.Add(m);

          if (child is IInterfaceImplementation interfaceImplementation)
          {
            foreach (var implementedMember in interfaceImplementation.Children<ITypeMemberDeclaration>())
              result.Add(implementedMember);
          }

          if (child is ITypeExtensionDeclaration typeExtension && !typeExtension.IsTypePartDeclaration)
          {
            foreach (var extensionMember in typeExtension.Children<ITypeMemberDeclaration>())
              result.Add(extensionMember);
          }

          if (child is ILet let)
            foreach (var binding in let.Bindings)
            {
              var headPattern = binding.HeadPattern;
              if (headPattern == null)
                continue;

              foreach (var declaration in headPattern.Declarations)
              {
                if (declaration is ITypeMemberDeclaration typeMemberDeclaration)
                  result.Add(typeMemberDeclaration);
              }
            }
        }

        return result.ReadOnlyList();
      }
    }

    public string CLRName => this.MakeClrName();
    public IReadOnlyList<ITypeDeclaration> TypeDeclarations => EmptyList<ITypeDeclaration>.Instance;
    public IEnumerable<ITypeDeclaration> TypeDeclarationsEnumerable => NestedTypeDeclarations;
    public IEnumerable<ITypeDeclaration> NestedTypeDeclarationsEnumerable => NestedTypeDeclarations;

    public IReadOnlyList<ITypeDeclaration> NestedTypeDeclarations
    {
      get
      {
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

      if (!(Parent is IModuleLikeDeclaration module))
        return;

      foreach (var member in module.Members)
      {
        if (!(member is IModuleDeclaration decl))
          continue;

        if (decl.SourceName == oldSourceName || decl.SourceName == name)
        {
          var element = member as CompositeElement;
          element?.SubTreeChanged(element, PsiChangedElementType.SourceContentsChanged);
        }
      }
    }
  }
}
