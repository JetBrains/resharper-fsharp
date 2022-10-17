using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class LocalPatternDeclarationBase : LocalDeclarationBase, IFSharpPatternDeclaredElement
  {
    public TreeNodeCollection<IAttribute> Attributes =>
      TreeNodeCollection<IAttribute>.Empty;

    public virtual IType GetPatternType() => TypeFactory.CreateUnknownType(GetPsiModule());

    public virtual IEnumerable<IFSharpPattern> NestedPatterns =>
      EmptyList<IFSharpPattern>.Instance;

    public virtual IEnumerable<IFSharpDeclaration> Declarations =>
      NestedPatterns.OfType<IFSharpDeclaration>();

    public bool IsLocal => true;
    
    public string ShortName { get; }
    public (int group, int index) Position { get; }
    public IFSharpParameterOwnerDeclaration OwnerDeclaration { get; }
    public IList<IFSharpParameter> Parameters { get; }
  }
}
