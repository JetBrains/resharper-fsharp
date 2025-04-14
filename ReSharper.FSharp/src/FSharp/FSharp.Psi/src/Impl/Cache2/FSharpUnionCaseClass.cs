using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;

public class FSharpUnionCaseClass([NotNull] Class.IClassPart part) : FSharpClass(part), IFSharpGeneratedFromUnionCase
{
  public IClrDeclaredElement OriginElement =>
    EnumerateParts().Select(part => (part as UnionCasePart)?.UnionCase).WhereNotNull().First();

  public bool IsReadOnly => false;

  public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
    new FSharpUnionCaseClassPointer(this);

  public override string ToString() => this.TestToString(BuildTypeParameterString());
}
