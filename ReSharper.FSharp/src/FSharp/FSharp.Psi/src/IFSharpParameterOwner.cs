using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpParameterOwner : IFSharpMember, IParametersOwner
{
  IList<IList<IFSharpParameter>> FSharpParameterGroups { get; }
  [CanBeNull] IFSharpParameter GetParameter(FSharpParameterIndex index);
}
