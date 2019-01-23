using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedPat
  {
    public bool IsDeclaration => true;

    public IEnumerable<ITypeMemberDeclaration> Declarations =>
      Pattern?.Declarations.Prepend(this) ?? new []{this};

    /// Workaround for type members cache:
    /// in `let a, b as c = 1, 2` we want `a` and `a, b as c` to have different offsets
    /// so use `c` offset when identifier exists.
    public TreeOffset GetOffset() =>
      Identifier?.GetTreeStartOffset() ?? GetTreeStartOffset();
  }

  internal partial class OrPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      EmptyList<ITypeMemberDeclaration>.Instance; // todo
  }

  internal partial class AndsPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      EmptyList<ITypeMemberDeclaration>.Instance;
  }

  internal partial class LongIdentPat
  {
    public bool IsDeclaration => Parent is IBinding;

    public IEnumerable<ITypeMemberDeclaration> Declarations =>
      IsDeclaration
        ? new[] {this}
        : Parameters.SelectMany(param => param.Declarations);
  }

  internal partial class ListPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      Patterns.SelectMany(pat => pat.Declarations);
  }

  internal partial class ParenPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class RecordPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      Patterns.SelectMany(pat => pat.Declarations);
  }

  internal partial class OptionalValPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class IsInstPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class TypedPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class ConsPat
  {
    public override IEnumerable<ITypeMemberDeclaration> Declarations
    {
      get
      {
        var pattern1Decls = Pattern1?.Declarations ?? EmptyList<ITypeMemberDeclaration>.Instance;
        var pattern2Decls = Pattern2?.Declarations ?? EmptyList<ITypeMemberDeclaration>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }
}
