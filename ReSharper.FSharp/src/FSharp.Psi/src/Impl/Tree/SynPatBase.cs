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

    public override IEnumerable<IFSharpPattern> NestedPatterns => new[] {this};

    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding != null, "GetBinding() != null");
      binding.SetIsMutable(true);
    }

    public override IBindingLikeDeclaration Binding => this.GetBinding();
  }

  internal partial class LocalReferencePat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => ReferenceName?.Identifier;

    public bool IsDeclaration
    {
      get
      {
        // todo: check other parents: e.g. parameters?
        if (Parent is IBindingLikeDeclaration)
          return true;

        var referenceName = ReferenceName;
        if (!(referenceName is { Qualifier: null }))
          return false;

        var name = referenceName.ShortName;
        if (!name.IsEmpty() && name[0].IsLowerFast())
          return true;

        var idOffset = GetNameIdentifierRange().StartOffset.Offset;
        return FSharpFile.GetSymbolUse(idOffset) == null;
      }
    }

    public override IEnumerable<IFSharpPattern> NestedPatterns => new[] {this};

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

    public IBindingLikeDeclaration Binding => this.GetBinding();
  }

  internal partial class ParametersOwnerPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Parameters.SelectMany(param => param.NestedPatterns);
  }

  internal partial class TopAsPat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public bool IsDeclaration => true;
    public override IEnumerable<IFSharpPattern> NestedPatterns => Pattern?.NestedPatterns.Prepend(this) ?? new[] {this};

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

    public override IBindingLikeDeclaration Binding => this.GetBinding();
  }

  internal partial class LocalAsPat
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
    public bool IsDeclaration => true;
    public override IEnumerable<IFSharpPattern> NestedPatterns => Pattern?.NestedPatterns.Prepend(this) ?? new[] {this};
    
    public bool IsMutable => Binding?.IsMutable ?? false;

    public void SetIsMutable(bool value)
    {
      var binding = Binding;
      Assertion.Assert(binding is LocalBinding, "GetBinding() is LocalBinding");
      binding.SetIsMutable(true);
    }

    public bool CanBeMutable => Binding is LocalBinding;

    private IBindingLikeDeclaration Binding => this.GetBinding();
  }

  internal partial class OrPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns
    {
      get
      {
        var pattern1Decls = Pattern1?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        var pattern2Decls = Pattern2?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }

  internal partial class AndsPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      EmptyList<IFSharpPattern>.Instance;
  }

  internal partial class ArrayPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Patterns.SelectMany(pat => pat.NestedPatterns);
  }

  internal partial class ListPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Patterns.SelectMany(pat => pat.NestedPatterns);
  }

  internal partial class TuplePat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Patterns.SelectMany(pat => pat.NestedPatterns);

    public bool IsStruct => StructKeyword != null;
  }

  internal partial class ParenPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class AttribPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class RecordPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      FieldPatterns.SelectMany(pat => pat.Pattern?.NestedPatterns).WhereNotNull();
  }

  internal partial class OptionalValPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class TypedPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Pattern.NestedPatterns;
  }

  internal partial class ListConsPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns
    {
      get
      {
        var pattern1Decls = HeadPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        var pattern2Decls = TailPattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;
        return pattern2Decls.Prepend(pattern1Decls);
      }
    }
  }
}
