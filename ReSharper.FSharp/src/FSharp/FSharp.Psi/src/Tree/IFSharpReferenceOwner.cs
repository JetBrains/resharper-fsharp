using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public enum FSharpReferenceContext
  {
    Expression,
    Pattern,
    Type
  }

  public interface IFSharpReferenceOwner : IFSharpTreeNode
  {
    [NotNull] FSharpSymbolReference Reference { get; }

    [CanBeNull] IFSharpIdentifier FSharpIdentifier { get; }

    [NotNull]
    IFSharpReferenceOwner SetName([NotNull] string name);
    
    FSharpReferenceContext? ReferenceContext { get; }
  }

  public interface IFSharpQualifiableReferenceOwner : IFSharpReferenceOwner
  {
    IList<string> Names { get; }
    FSharpSymbolReference QualifierReference { get; }
    bool IsQualified { get; }
    void SetQualifier([NotNull] IClrDeclaredElement declaredElement, ITreeNode context = null);
  }

  public static class FSharpQualifiableReferenceOwnerExtensions
  {
    public static void SetQualifier([NotNull] this IFSharpQualifiableReferenceOwner referenceOwner,
      [NotNull] Func<string, IFSharpQualifiableReferenceOwner> factory, [NotNull] IClrDeclaredElement declaredElement,
      ITreeNode context = null)
    {
      var identifier = referenceOwner.FSharpIdentifier;
      Assertion.Assert(identifier != null, "referenceOwner.FSharpIdentifier != null");

      // todo: type args
      var name = FSharpReferenceBindingUtil.SuggestShortReferenceName(declaredElement, referenceOwner.Language);
      var delimiter = ModificationUtil.AddChildBefore(identifier, FSharpTokenType.DOT.CreateLeafElement());
      var qualifier = ModificationUtil.AddChildBefore(delimiter, factory(name));
      qualifier.Reference.SetRequiredQualifiers(declaredElement, context ?? referenceOwner);
    }
  }
}
