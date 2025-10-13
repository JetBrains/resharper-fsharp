using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

public class FSharpGeneratedExceptionDefaultConstructor(TypePart typePart) : FSharpGeneratedConstructor(typePart)
{
  public override IList<IParameter> Parameters =>
  [
    new Parameter(this, 0, ParameterKind.VALUE, PredefinedType.SerializationInfo, "info"),
    new Parameter(this, 1, ParameterKind.VALUE, PredefinedType.StreamingContext, "context")
  ];

  public override AccessRights GetAccessRights() => AccessRights.PROTECTED;
}
