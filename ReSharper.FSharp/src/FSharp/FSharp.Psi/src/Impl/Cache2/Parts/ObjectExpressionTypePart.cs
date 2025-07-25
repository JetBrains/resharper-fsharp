using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public class ObjectExpressionTypePart : FSharpTypePart<IObjExpr>, IFSharpClassPart
  {
    public ObjectExpressionTypePart([NotNull] IObjExpr declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, declaration.DeclaredName, MemberDecoration.DefaultValue, 0, cacheBuilder)
    {
      var extendListShortNames = new FrugalLocalHashSet<string>();

      var baseClassOrInterfaceName = declaration.TypeName?.Identifier?.Name;
      if (baseClassOrInterfaceName != null)
        extendListShortNames.Add(baseClassOrInterfaceName);

      foreach (var interfaceImplementation in declaration.InterfaceImplementations)
      {
        var interfaceName = interfaceImplementation.TypeName?.Identifier?.Name;
        if (interfaceName != null)
          extendListShortNames.Add(interfaceName);
      }

      ExtendsListShortNames = extendListShortNames.ToArray();
    }

    public override string[] ExtendsListShortNames { get; }

    public IDeclaredType GetBaseClassType()
    {
      var type = GetDeclaration()?.TypeName?.Reference.ResolveType();
      return type.IsClassType() ? type : null;
    }

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.ObjectExpression;

    public override TypeElement CreateTypeElement(IPsiModule module) =>
      new FSharpObjectExpressionClass(this);

    public ObjectExpressionTypePart(IReader reader) : base(reader) =>
      ExtendsListShortNames = reader.ReadStringArray();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteStringArray(ExtendsListShortNames);
    }

    public override bool CanBePartial => false;

    public override IDeclaration GetTypeParameterDeclaration(int index) => throw new InvalidOperationException();
    public override string GetTypeParameterName(int index) => throw new InvalidOperationException();
    public override TypeParameterVariance GetTypeParameterVariance(int index) => throw new InvalidOperationException();
    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index) => throw new InvalidOperationException();
    public override bool IsNullableContextEnabledForTypeParameter(int index) => throw new InvalidOperationException();

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index) =>
      throw new InvalidOperationException();

    public virtual IEnumerable<ITypeMember> GetTypeMembers()
    {
      var declaration = GetDeclaration();
      if (declaration == null)
        return EmptyList<ITypeMember>.Instance;

      var result = new LocalList<ITypeMember>();
      foreach (var memberDeclaration in declaration.MemberDeclarations)
        if (memberDeclaration.DeclaredElement is { } declaredElement)
          result.Add(declaredElement);

      foreach (var memberDeclaration in declaration.InterfaceMembers)
        if (memberDeclaration.DeclaredElement is { } declaredElement)
          result.Add(declaredElement);

      return result.ResultingList();
    }

    public IEnumerable<IDeclaredType> GetSuperTypes() =>
      GetDeclaration()?.SuperTypes ?? EmptyList<IDeclaredType>.Instance;

    public IEnumerable<ITypeElement> GetSuperTypeElements() =>
      GetSuperTypes().SelectNotNull(type => type.GetTypeElement());

    public MemberPresenceFlag GetMemberPresenceFlag() =>
      MemberPresenceFlag.MAY_EQUALS_OVERRIDE; // todo: calculate member flags

    public IClass GetSuperClass() =>
      GetBaseClassType()?.GetTypeElement() as IClass;
  }
}
