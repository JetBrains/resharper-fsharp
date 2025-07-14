using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpParameterDeclaration : INameIdentifierOwner
{
  [CanBeNull] IFSharpParameter FSharpParameter { get; }
}
