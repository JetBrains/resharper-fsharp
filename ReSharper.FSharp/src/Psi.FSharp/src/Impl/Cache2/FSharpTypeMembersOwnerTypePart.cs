using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal abstract class FSharpTypeMembersOwnerTypePart : FSharpClassLikePart<IFSharpTypeDeclaration>
  {
    protected FSharpTypeMembersOwnerTypePart(IFSharpTypeDeclaration declaration, ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable),
        declaration.TypeParameters, cacheBuilder)
    {
      var extendListShortNames = new FrugalLocalHashSet<string>();
      foreach (var member in declaration.TypeMembersEnumerable)
      {
        var inherit = member as ITypeInherit;
        if (inherit?.LongIdentifier != null)
        {
          extendListShortNames.Add(inherit.LongIdentifier.Name);
          continue;
        }

        var interfaceImpl = member as IInterfaceImplementation;
        if (interfaceImpl?.LongIdentifier != null)
        {
          extendListShortNames.Add(interfaceImpl.LongIdentifier.Name);
          continue;
        }

        var interfaceInherit = member as IInterfaceInherit;
        if (interfaceInherit?.LongIdentifier != null)
          extendListShortNames.Add(interfaceInherit.LongIdentifier.Name);
      }

      ExtendsListShortNames = extendListShortNames.ToArray();
    }

    protected FSharpTypeMembersOwnerTypePart(IReader reader) : base(reader)
    {
      ExtendsListShortNames = reader.ReadStringArray();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteStringArray(ExtendsListShortNames);
    }

    public override string[] ExtendsListShortNames { get; }
  }
}