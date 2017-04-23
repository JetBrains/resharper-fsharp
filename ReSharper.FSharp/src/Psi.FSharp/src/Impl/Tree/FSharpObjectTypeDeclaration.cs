using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpObjectTypeDeclaration
  {
    private const string Interface = "Interface";
    private const string AbstractClass = "AbstractClass";
    private const string Class = "Class";
    private const string Sealed = "Sealed";
    private const string Struct = "Struct";

    public override string DeclaredName => FSharpImplUtil.GetName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public FSharpPartKind TypePartKind
    {
      get
      {
        foreach (var attr in AttributesEnumerable)
        {
          var attrText = attr.GetText();
          switch (attrText)
          {
            case Interface:
              return FSharpPartKind.Interface;
            case AbstractClass:
            case Sealed:
            case Class:
              return FSharpPartKind.Class;
            case Struct:
              return FSharpPartKind.Struct;
          }
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var member in TypeMembersEnumerable)
          if (!(member is IInterfaceInherit) && !(member is IAbstractSlot))
            return FSharpPartKind.Class;

        return FSharpPartKind.Interface;
      }
    }
  }
}