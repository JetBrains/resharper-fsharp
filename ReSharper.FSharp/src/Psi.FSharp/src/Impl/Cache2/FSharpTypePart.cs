using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public abstract class FSharpTypePart<T> : TypePartImplBase<T> where T : class, IFSharpDeclaration, ITypeDeclaration
  {
    private readonly MemberDecoration myDecoration;

    protected FSharpTypePart(T declaration, MemberDecoration memberDecoration, int typeParameters,
      ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder.Intern(declaration.ShortName), typeParameters)
    {
      // todo: calc access rights in class (impl & sig files)
      myDecoration = memberDecoration;
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
      if (candidateDeclaration is T) return candidateDeclaration;
      return null;
    }

    public override string[] ExtendsListShortNames => EmptyArray<string>.Instance;
    public override bool CanBePartial => true; // workaround for F# signatures
    public override MemberDecoration Modifiers => myDecoration;

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