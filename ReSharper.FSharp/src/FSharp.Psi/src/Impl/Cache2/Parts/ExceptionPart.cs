using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ExceptionPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    private static readonly string[] ourExtendsListShortNames = {"Exception", "IStructuralEquatable"};

    public ExceptionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public ExceptionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpException(this);
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.Exception;

    public override string[] ExtendsListShortNames =>
      ArrayUtil.Add(ourExtendsListShortNames, base.ExtendsListShortNames);

    public override IDeclaredType GetBaseClassType() =>
      GetPsiModule().GetPredefinedType().Exception;
  }
}