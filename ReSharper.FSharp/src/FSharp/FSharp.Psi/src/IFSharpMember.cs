using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpMember : IFSharpTypeMember, IOverridableMember
{
  [CanBeNull] FSharpMemberOrFunctionOrValue Mfv { get; }
}
