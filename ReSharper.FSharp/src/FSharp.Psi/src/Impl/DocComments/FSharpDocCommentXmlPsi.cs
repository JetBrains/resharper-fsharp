using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  internal class FSharpDocCommentXmlPsi : ClrDocCommentXmlPsi<XmlDocBlock>
  {
    private FSharpDocCommentXmlPsi(
      [NotNull] InjectedPsiHolderNode docCommentsHolder,
      [NotNull] XmlDocBlock fSharpDocCommentBlock,
      [NotNull] IXmlFile xmlFile, bool isShifted)
      : base(docCommentsHolder, xmlFile, isShifted, fSharpDocCommentBlock)
    {
    }

    [NotNull]
    public static FSharpDocCommentXmlPsi BuildPsi([NotNull] XmlDocBlock block)
    {
      BuildXmlPsi(
        FSharpXmlDocLanguage.Instance.NotNull(), block, GetCommentLines(block),
        out var holderNode, out var xmlPsiFile, out var isShifted);

      return new FSharpDocCommentXmlPsi(holderNode, block, xmlPsiFile, isShifted);
    }

    [NotNull, Pure]
    public static IReadOnlyList<string> GetCommentLines([NotNull] XmlDocBlock block) =>
      block.DocComments
        .Select(x => x.CommentText)
        .ToIReadOnlyList();

    protected override IReadOnlyList<ITreeNode> GetDocCommentNodes() => DocCommentBlock.DocComments;

    protected override string GetDocCommentStartText(ITreeNode commentNode) => "///";

    public override void SubTreeChanged()
    {
    }
  }
}
