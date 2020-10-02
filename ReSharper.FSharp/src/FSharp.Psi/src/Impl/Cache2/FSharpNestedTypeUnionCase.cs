using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpNestedTypeUnionCase : FSharpClass, IFSharpGeneratedFromUnionCase
  {
    public FSharpNestedTypeUnionCase([NotNull] IClassPart part) : base(part)
    {
    }

    public IClrDeclaredElement OriginElement =>
      EnumerateParts().Select(part => (part as UnionCasePart)?.UnionCase).WhereNotNull().First();

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpNestedTypeUnionCasePointer(this);
  }

  public class FSharpNestedTypeUnionCasePointer : FSharpGeneratedElementPointerBase<FSharpNestedTypeUnionCase, IUnionCase>
  {
    public FSharpNestedTypeUnionCasePointer(FSharpNestedTypeUnionCase nestedType) : base(nestedType)
    {
    }

    public override FSharpNestedTypeUnionCase CreateGenerated(IUnionCase unionCase) =>
      unionCase.NestedType;
  }
}
