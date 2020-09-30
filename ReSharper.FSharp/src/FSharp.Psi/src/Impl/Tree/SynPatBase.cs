using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopReferencePat
  {
    public bool IsDeclaration => true;

    public IEnumerable<IDeclaration> Declarations => new[] {this};

    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding != null, "GetBinding() != null");
      binding.SetIsMutable(true);
    }

    public override IBinding Binding => this.GetBinding();
  }

  internal partial class LocalReferencePat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => ReferenceName?.Identifier;

    public bool IsDeclaration
    {
      get
      {
        // todo: check other parents: e.g. parameters?
        if (Parent is IBinding)
          return true;

        var referenceName = ReferenceName;
        if (referenceName == null)
          return false;

        if (referenceName.Qualifier != null)
          return false;

        var name = referenceName.ShortName;
        if (!name.IsEmpty() && name[0].IsLowerFast())
          return true;

        var idOffset = GetNameIdentifierRange().StartOffset.Offset;
        return FSharpFile.GetSymbolUse(idOffset) == null;
      }
    }

    public IEnumerable<IDeclaration> Declarations => new[] {this};

    public override TreeTextRange GetNameIdentifierRange() =>
      NameIdentifier.GetMemberNameIdentifierRange();

    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding != null, "GetBinding() != null");
      binding.SetIsMutable(true);
    }

    public bool CanBeMutable => Binding != null;

    public IBinding Binding => this.GetBinding();
  }

  internal partial class LocalParametersOwnerPat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => ReferenceName?.Identifier;

    public bool IsDeclaration
    {
      get
      {
        if (Parent is IBinding)
          return true;

        if (ReferenceName?.Qualifier != null)
          return false;

        var idOffset = GetNameIdentifierRange().StartOffset.Offset;
        return Parameters.IsEmpty && FSharpFile.GetSymbolUse(idOffset) == null;
      }
    }

    public IEnumerable<IDeclaration> Declarations =>
      IsDeclaration
        ? new[] {this}
        : Parameters.SelectMany(param => param.Declarations);
  }

  internal partial class TopAsPat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public bool IsDeclaration => true;
    public IEnumerable<IDeclaration> Declarations => Pattern?.Declarations.Prepend(this) ?? new[] {this};

    public TreeNodeCollection<IAttribute> Attributes =>
      this.GetBinding()?.AllAttributes ??
      TreeNodeCollection<IAttribute>.Empty;

    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding != null, "GetBinding() != null");
      binding.SetIsMutable(true);
    }

    public override IBinding Binding => this.GetBinding();
  }

  internal partial class LocalAsPat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
    public bool IsDeclaration => true;
    public IEnumerable<IDeclaration> Declarations => Pattern?.Declarations.Prepend(this) ?? new[] {this};
    
    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding is LocalBinding, "GetBinding() is LocalBinding");
      binding.SetIsMutable(true);
    }

    public bool CanBeMutable => Binding is LocalBinding;

    private IBinding Binding => this.GetBinding();
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

  internal partial class TopParametersOwnerPat
  {
    public bool IsDeclaration => Parent is IBinding;

    public IEnumerable<IDeclaration> Declarations =>
      IsDeclaration
        ? new[] {this}
        : Parameters.SelectMany(param => param.Declarations);

    public override IBinding Binding => this.GetBinding();
  }

  internal partial class ArrayPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Patterns.SelectMany(pat => pat.Declarations);
  }

  internal partial class ListPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Patterns.SelectMany(pat => pat.Declarations);
  }

  internal partial class TuplePat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Patterns.SelectMany(pat => pat.Declarations);
  }

  internal partial class ParenPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class AttribPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class RecordPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      FieldPatterns.SelectMany(pat => pat.Pattern?.Declarations).WhereNotNull();
  }

  internal partial class OptionalValPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class TypedPat
  {
    public override IEnumerable<IDeclaration> Declarations =>
      Pattern.Declarations;
  }

  internal partial class ListConsPat
  {
    public override IEnumerable<IDeclaration> Declarations
    {
      get
      {
        var pattern1Decls = HeadPattern?.Declarations ?? EmptyList<IDeclaration>.Instance;
        var pattern2Decls = TailPattern?.Declarations ?? EmptyList<IDeclaration>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }
}
