using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public abstract class FSharpObjectModelTypePart : FSharpClassLikePart<IFSharpTypeParametersOwnerDeclaration>
  {
    protected FSharpObjectModelTypePart(IFSharpTypeParametersOwnerDeclaration declaration, bool isHidden)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers), isHidden,
        declaration.TypeParameters.Count)
    {
      var extendListShortNames = new FrugalLocalHashSet<string>();
      foreach (var member in declaration.TypeMembersEnumerable)
      {
        var inherit = member as ITypeInherit;
        if (inherit != null)
        {
          extendListShortNames.Add(inherit.LongIdentifier.Name);
          continue;
        }

        var interfaceImpl = member as IInterfaceImplementation;
        if (interfaceImpl != null)
        {
          extendListShortNames.Add(interfaceImpl.LongIdentifier.Name);
          continue;
        }

        var interfaceInherit = member as IInterfaceInherit;
        if (interfaceInherit != null)
          extendListShortNames.Add(interfaceInherit.LongIdentifier.Name);
      }

      ExtendsListShortNames = extendListShortNames.ToArray();
    }

    protected FSharpObjectModelTypePart(IReader reader) : base(reader)
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