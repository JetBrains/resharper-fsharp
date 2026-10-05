using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;

public class FSharpRecordFieldFromSpreadPointer(FSharpRecordFieldFromSpread element)
  : IDeclaredElementPointer<FSharpRecordFieldFromSpread>
{
  private readonly IDeclaredElementPointer<ITypeElement> myContainingTypePointer =
    element.GetContainingType().CreateElementPointer();

  private readonly string myName = element.ShortName;

  public FSharpRecordFieldFromSpread FindDeclaredElement() =>
    myContainingTypePointer.FindDeclaredElement()?.GetRecordFields()
      .OfType<FSharpRecordFieldFromSpread>()
      .FirstOrDefault(field => field.ShortName == myName);
}
