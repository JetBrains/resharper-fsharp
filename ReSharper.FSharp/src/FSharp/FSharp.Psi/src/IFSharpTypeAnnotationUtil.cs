using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpTypeAnnotationUtil
{
  void SetPatternFcsType(IFSharpPattern pattern, FSharpType fcsType);
  void SetTypeOwnerFcsType(IFSharpTypeUsageOwnerNode typeUsageOwnerNode, FSharpType fcsType);

  ITypedPat SetPatternTypeUsage(IFSharpPattern pattern, ITypeUsage typeUsage);
  void ReplaceWithFcsType(ITypeUsage typeUsage, FSharpType fcsType);
}
