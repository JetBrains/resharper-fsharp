using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public abstract class FSharpTypePart<TDeclaration> : TypePartImplBase<TDeclaration>
    where TDeclaration : class, IFSharpDeclaration, ITypeDeclaration
  {
    private readonly MemberDecoration myDecoration;

    protected FSharpTypePart(TDeclaration declaration, MemberDecoration memberDecoration, bool isHidden,
      int typeParameters = 0) : base(declaration, declaration.ShortName, typeParameters)
    {
      myDecoration = memberDecoration;

      if (myDecoration.AccessRights == AccessRights.NONE)
        myDecoration.AccessRights = isHidden ? AccessRights.INTERNAL : AccessRights.PUBLIC;
    }

    protected FSharpTypePart(IReader reader) : base(reader)
    {
      myDecoration = MemberDecoration.FromInt(reader.ReadInt());
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteInt(myDecoration.ToInt());
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration)
    {
      if (Offset < TreeOffset.Zero) return null;
      if (candidateDeclaration is TDeclaration) return candidateDeclaration;
      return null;
    }

    public override string[] ExtendsListShortNames => EmptyArray<string>.Instance;
    public override bool CanBePartial => true; // workaround for F# signatures
    public override MemberDecoration Modifiers => myDecoration;

    public override IDeclaration GetTypeParameterDeclaration(int index)
    {
      var declaration = GetDeclaration() as IFSharpTypeParametersOwnerDeclaration;
      if (declaration == null) return null;

      var parameters = declaration.TypeParameters;
      Assertion.Assert(parameters.Count >= index, "typeParametersDeclarations.Count >= index");
      return parameters[index];
    }

    public override string GetTypeParameterName(int index)
    {
      var declaration = GetDeclaration() as IFSharpTypeParametersOwnerDeclaration;
      Assertion.AssertNotNull(declaration, "typeParamsOwnerDeclaration != null");

      var name = declaration.TypeParameters[index].GetText();
      return name[0] == '\'' ? name.Substring(1) : name;
    }

    public override TypeParameterVariance GetTypeParameterVariance(int index)
    {
      return TypeParameterVariance.INVARIANT;
    }

    public override IEnumerable<IType> GetTypeParameterSuperTypes(int index)
    {
      return EmptyList<IType>.Instance;
    }

    public override TypeParameterConstraintFlags GetTypeParameterConstraintFlags(int index)
    {
      return 0;
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

    public override string[] AttributeClassNames => EmptyArray<string>.Instance;
  }
}