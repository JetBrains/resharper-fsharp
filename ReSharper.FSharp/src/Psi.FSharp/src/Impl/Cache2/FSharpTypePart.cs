using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public abstract class FSharpTypePart<TDeclaration> : TypePartImplBase<TDeclaration>
    where TDeclaration : class, ITypeDeclaration
  {
    protected FSharpTypePart(TDeclaration declaration, string shortName, int typeParameters = 0)
      : base(declaration, shortName, typeParameters)
    {
      ExtendsListShortNames = EmptyArray<string>.Instance; // todo
    }

    protected FSharpTypePart(IReader reader) : base(reader)
    {
      ExtendsListShortNames = reader.ReadStringArray();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteStringArray(ExtendsListShortNames);
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration)
    {
      if (Offset < TreeOffset.Zero) return null;
      if (candidateDeclaration is TDeclaration) return candidateDeclaration;
      return null;
    }

    public override string[] ExtendsListShortNames { get; }
    public override bool CanBePartial => true; // workaround for F# signatures

    public override IDeclaration GetTypeParameterDeclaration(int index)
    {
      throw new System.NotImplementedException();
    }

    public override string GetTypeParameterName(int index)
    {
      throw new System.NotImplementedException();
    }

    public override TypeParameterVariance GetTypeParameterVariance(int index)
    {
      throw new System.NotImplementedException();
    }

    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index)
    {
      return EmptyList<IType>.Instance;
    }

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index)
    {
      throw new System.NotImplementedException();
    }

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName)
    {
      return EmptyList<IAttributeInstance>.Instance;
    }

    public override bool HasAttributeInstance(IClrTypeName clrTypeName)
    {
      return false;
    }

    public override IList<IAttributeInstance> GetTypeParameterAttributeInstances(int index, IClrTypeName typeName)
    {
      return EmptyList<IAttributeInstance>.Instance;
    }

    public override bool HasTypeParameterAttributeInstance(int index, IClrTypeName typeName)
    {
      return false;
    }

    public override MemberDecoration Modifiers => MemberDecoration.FromModifiers(Psi.Modifiers.PUBLIC);
    public override string[] AttributeClassNames => EmptyArray<string>.Instance;
  }
}