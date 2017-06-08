using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts
{
  public abstract class FSharpTypePart<T> : TypePartImplBase<T> where T : class, IFSharpDeclaration, ITypeDeclaration
  {
    protected FSharpTypePart(T declaration, MemberDecoration memberDecoration, int typeParameters,
      ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder.Intern(declaration.ShortName), typeParameters)
    {
      Modifiers = memberDecoration;

      var attrOwner = declaration as IFSharpTypeDeclaration;
      if (attrOwner == null)
      {
        AttributeClassNames = EmptyArray<string>.Instance;
        return;
      }
      var attrNames = new FrugalLocalHashSet<string>();
      foreach (var attr in attrOwner.AttributesEnumerable)
        attrNames.Add(cacheBuilder.Intern(attr.GetText().SubstringBeforeLast("Attribute")));
      AttributeClassNames = attrNames.ToArray();
    }

    protected FSharpTypePart(IReader reader) : base(reader)
    {
      Modifiers = MemberDecoration.FromInt(reader.ReadInt());
      AttributeClassNames = reader.ReadStringArray();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteInt(Modifiers.ToInt());
      writer.WriteStringArray(AttributeClassNames);
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration)
    {
      if (Offset < TreeOffset.Zero) return null;
      if (candidateDeclaration is T) return candidateDeclaration;
      return null;
    }

    public override string[] ExtendsListShortNames => EmptyArray<string>.Instance;
    public override MemberDecoration Modifiers { get; }
    public override bool CanBePartial => true; // workaround for F# signatures

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName)
    {
      var entity = (GetDeclaration() as IFSharpTypeDeclaration)?.GetFSharpSymbol() as FSharpEntity;
      if (entity == null)
        return EmptyList<IAttributeInstance>.Instance;

      var attrs = new List<IAttributeInstance>();
      foreach (var attr in entity.Attributes)
        attrs.Add(new FSharpAttributeInstance(attr, GetPsiModule()));
      return attrs;
    }

    public override bool HasAttributeInstance(IClrTypeName clrTypeName)
    {
      // todo: get entity without getting declaration 
      var entity = (GetDeclaration() as IFSharpTypeDeclaration)?.GetFSharpSymbol() as FSharpEntity;
      return entity?.Attributes.Any(a => a.AttributeType.FullName == clrTypeName.FullName) ?? false;
    }

    public override IList<IAttributeInstance> GetTypeParameterAttributeInstances(int index, IClrTypeName typeName)
    {
      // todo
      return EmptyList<IAttributeInstance>.Instance;
    }

    public override bool HasTypeParameterAttributeInstance(int index, IClrTypeName typeName)
    {
      // todo
      return false;
    }

    public override string[] AttributeClassNames { get; }
  }
}