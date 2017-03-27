using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpObjectTypeDeclaration
  {
    private const string Interface = "Interface";
    private const string AbstractClass = "AbstractClass";
    private const string Class = "Class";
    private const string Struct = "Struct";

    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public FSharpObjectModelTypeKind TypeKind
    {
      get
      {
        foreach (var attr in AttributesEnumerable)
        {
          var attrText = attr.GetText();
          if (attrText == Interface) return FSharpObjectModelTypeKind.Interface;
          if (attrText == AbstractClass || attrText == Class) return FSharpObjectModelTypeKind.Class;
          if (attrText == Struct) return FSharpObjectModelTypeKind.Struct;
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var member in TypeMembersEnumerable)
          if (!(member is IInterfaceInherit) && !(member is IAbstractSlot))
            return FSharpObjectModelTypeKind.Class;

        return FSharpObjectModelTypeKind.Interface;
      }
    }
  }
}