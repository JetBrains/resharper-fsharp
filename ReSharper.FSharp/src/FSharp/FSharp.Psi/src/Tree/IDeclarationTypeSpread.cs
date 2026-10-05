using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public partial interface IDeclarationTypeSpread
{
  [CanBeNull] IDeclaredType GetRecordType();
  IEnumerable<IFSharpRecordField> GetFields();
}
