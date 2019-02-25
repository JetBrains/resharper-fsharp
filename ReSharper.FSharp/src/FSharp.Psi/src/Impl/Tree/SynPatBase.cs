using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopNamedPat
  {
    public bool IsDeclaration => true;

    public IEnumerable<IDeclaration> Declarations =>
      Pattern?.Declarations.Prepend(this) ?? new[] {this};

    /// Workaround for type members cache:
    /// in `let a, b as c = 1, 2` we want `a` and `a, b as c` to have different offsets
    /// so use `c` offset when identifier exists.
    public TreeOffset GetOffset() =>
      Identifier?.GetTreeStartOffset() ?? GetTreeStartOffset();
  }

  internal partial class LocalNamedPat
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
    public bool IsDeclaration => true;
    public IEnumerable<IDeclaration> Declarations => Pattern?.Declarations.Prepend(this) ?? new[] {this};
    public TreeOffset GetOffset() => GetTreeStartOffset();
  }

  internal partial class LocalLongIdentPat
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    public bool IsDeclaration
    {
      get
      {
        if (Parent is IBinding)
          return true;

        // todo: add check for lid.Length > 1
        var offsetOffset = GetNameIdentifierRange().StartOffset.Offset;
        return Parameters.IsEmpty && FSharpFile.GetSymbolUse(offsetOffset) == null;
      }
    }

    public IEnumerable<IDeclaration> Declarations =>
      IsDeclaration
        ? new[] {this}
        : Parameters.SelectMany(param => param.Declarations);
  }

  internal partial class OrPat
  {
    public override IEnumerable<IDeclaration> Declarations
    {
      get
      {
        var pattern1Decls = Pattern1?.Declarations ?? EmptyList<IDeclaration>.Instance;
        var pattern2Decls = Pattern2?.Declarations ?? EmptyList<IDeclaration>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }

  internal partial class AndsPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      EmptyList<IDeclaration>.Instance;
  }

  internal partial class TopLongIdentPat
  {
    public bool IsDeclaration => Parent is IBinding;

    public IEnumerable<IDeclaration> Declarations =>
      IsDeclaration
        ? new[] {this}
        : Parameters.SelectMany(param => param.Declarations);
  }

  internal partial class ListPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Patterns.SelectMany(pat => pat.Declarations);
  }

  internal partial class ParenPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class RecordPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Patterns.SelectMany(pat => pat.Declarations);
  }

  internal partial class OptionalValPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class IsInstPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      EmptyList<IDeclaration>.Instance;
  }

  internal partial class TypedPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class ConsPat
  {
    public override IEnumerable<IDeclaration> Declarations
    {
      get
      {
        var pattern1Decls = Pattern1?.Declarations ?? EmptyList<IDeclaration>.Instance;
        var pattern2Decls = Pattern2?.Declarations ?? EmptyList<IDeclaration>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }
}
