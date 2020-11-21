using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPropertyWithGeneratedAccessors<TDeclaration> : FSharpPropertyBase<TDeclaration>, IFSharpProperty
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public FSharpPropertyWithGeneratedAccessors([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
      var members = mfv.DeclaringEntity?.Value.MembersFunctionsAndValues;

      var range = mfv.DeclarationLocation;
      var logicalName = mfv.LogicalName;

      var getterName = "get_" + logicalName;
      var setterName = "set_" + logicalName;

      var getter = members?.FirstOrDefault(m => m.LogicalName == getterName && m.DeclarationLocation.Equals(range));
      var setter = members?.FirstOrDefault(m => m.LogicalName == setterName && m.DeclarationLocation.Equals(range));

      var isReadable = IsReadable = getter != null;
      var isWritable = IsWritable = setter != null;

      Getter = isReadable ? new FSharpPropertyAccessor(getter, this, AccessorKind.GETTER) : null;
      Setter = isWritable ? new FSharpPropertyAccessor(setter, this, AccessorKind.SETTER) : null;
    }

    public override bool IsReadable { get; }
    public override bool IsWritable { get; }
    public override IAccessor Getter { get; }
    public override IAccessor Setter { get; }
    public override AccessRights GetAccessRights() => AccessRights.PRIVATE;
    public AccessRights RepresentationAccessRights => base.GetAccessRights();

    public IEnumerable<IAccessor> Getters
    {
      get
      {
        foreach (var declaration in GetDeclarations())
        {
          if (declaration.DeclaredElement is IFSharpProperty {IsReadable: true} prop)
            yield return prop.Getter;
        }
      }
    }

    public IEnumerable<IAccessor> Setters
    {
      get
      {
        foreach (var declaration in GetDeclarations())
        {
          if (declaration.DeclaredElement is IFSharpProperty {IsWritable: true} prop)
            yield return prop.Setter;
        }
      }
    }
  }
}
