using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpTypeMembersOwnerTypePart : FSharpClassLikePart<IFSharpTypeOldDeclaration>
  {
    protected FSharpTypeMembersOwnerTypePart([NotNull] IFSharpTypeOldDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifier, declaration.Attributes),
        declaration.TypeParameters, cacheBuilder)
    {
      var extendListShortNames = new FrugalLocalHashSet<string>();
      extendListShortNames = ProcessMembers(declaration.TypeMembersEnumerable, extendListShortNames);

      if (declaration is IFSharpTypeDeclaration { TypeRepresentation: IObjectModelTypeRepresentation repr })
        extendListShortNames = ProcessMembers(repr.TypeMembersEnumerable, extendListShortNames);

      ExtendsListShortNames = extendListShortNames.ToArray();
    }

    private static FrugalLocalHashSet<string> ProcessMembers(TreeNodeEnumerable<IFSharpTypeMemberDeclaration> members,
      FrugalLocalHashSet<string> names)
    {
      foreach (var member in members)
      {
        var baseTypeIdentifier = (member as ITypeInherit)?.TypeName?.Identifier;
        if (baseTypeIdentifier != null)
        {
          names.Add(baseTypeIdentifier.Name);
          continue;
        }

        var interfaceImplIdentifier = (member as IInterfaceImplementation)?.TypeName?.Identifier;
        if (interfaceImplIdentifier != null)
        {
          names.Add(interfaceImplIdentifier.Name);
          continue;
        }

        var interfaceInheritIdentifier = (member as IInterfaceInherit)?.TypeName?.Identifier;
        if (interfaceInheritIdentifier != null) names.Add(interfaceInheritIdentifier.Name);
      }

      return names;
    }

    protected FSharpTypeMembersOwnerTypePart(IReader reader) : base(reader) =>
      ExtendsListShortNames = reader.ReadStringArray();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteStringArray(ExtendsListShortNames);
    }

    public override string[] ExtendsListShortNames { get; }

    [CanBeNull] internal IClrTypeName[] SuperTypesClrTypeNames;

    public override IEnumerable<IDeclaredType> GetSuperTypes()
    {
      if (ExtendsListShortNames.IsEmpty())
        return EmptyList<IDeclaredType>.InstanceList;

      var declaration = GetDeclaration();
      return declaration != null ? declaration.SuperTypes : EmptyList<IDeclaredType>.InstanceList;
    }

    public override IEnumerable<ITypeElement> GetSuperTypeElements()
    {
      var psiModule = GetPsiModule();
      if (SuperTypesClrTypeNames != null)
        return SuperTypesClrTypeNames.ToTypeElements(psiModule);

      var superTypeNames = new HashSet<IClrTypeName>();
      var superTypeElements = new HashSet<ITypeElement>();

      foreach (var declaredType in GetSuperTypes())
      {
        var typeElement = declaredType.GetTypeElement();
        if (typeElement == null)
          continue;

        superTypeNames.Add(typeElement.GetClrName().GetPersistent());
        superTypeElements.Add(typeElement);
      }

      SuperTypesClrTypeNames = superTypeNames.ToArray();
      return superTypeElements;
    }
  }
}
