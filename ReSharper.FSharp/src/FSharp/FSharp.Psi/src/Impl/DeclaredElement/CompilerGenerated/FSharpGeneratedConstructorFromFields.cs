using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

public class FSharpGeneratedConstructorFromFields([NotNull] TypePart typePart)
  : FSharpGeneratedConstructor(typePart), IFSharpParameterOwner, IFSharpGeneratedFromOtherElement
{
  public override IList<IParameter> Parameters =>
    ParameterGroup.Cast<IParameter>().ToList();

  public IList<IList<IFSharpParameter>> FSharpParameterGroups => [ParameterGroup];

  public IFSharpParameter GetParameter(FSharpParameterIndex index) =>
    this.GetFSharpParameter(index);

  private IList<IFSharpParameter> ParameterGroup =>
    TypePart is IFSharpFieldsOwnerPart part
      ? part.Fields.Select(IFSharpParameter (f) => new FSharpGeneratedParameter(this, f, false)).ToList()
      : EmptyList<IFSharpParameter>.Instance;

  public IClrDeclaredElement OriginElement => TypePart.TypeElement;

  public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
    new FSharpGeneratedConstructorFromFieldsPointer(this);

  public class FSharpGeneratedConstructorFromFieldsPointer(FSharpGeneratedConstructorFromFields ctor)
    : FSharpGeneratedElementPointerBase<FSharpGeneratedConstructorFromFields, TypeElement>(ctor)
  {
    public override FSharpGeneratedConstructorFromFields CreateGenerated(TypeElement typeElement) =>
      new(typeElement.Parts.NotNull());
  }
}
