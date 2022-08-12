using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.VB;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  public class FSharpDocCommentXmlPsi : ClrDocCommentXmlPsi<XmlDocBlock>
  {
    private FSharpDocCommentXmlPsi(
      [NotNull] InjectedPsiHolderNode docCommentsHolder,
      [NotNull] XmlDocBlock fSharpDocCommentBlock,
      [NotNull] IXmlFile xmlFile, bool isShifted)
      : base(docCommentsHolder, xmlFile, isShifted, fSharpDocCommentBlock)
    {

      var infos = new FSharpDocCommentElementFactory(this).DecodeCRefs(XmlFile);
      BindReferences<IDocCommentReference>(infos);
    }

    [NotNull]
    public static FSharpDocCommentXmlPsi BuildPsi([NotNull] XmlDocBlock block)
    {
      BuildXmlPsi(FSharpXmlDocLanguage.Instance.NotNull(), block, GetCommentLines(block),
        out var holderNode, out var xmlPsiFile, out var isShifted);

      return new FSharpDocCommentXmlPsi(holderNode, block, xmlPsiFile, isShifted);
    }

    [NotNull, Pure]
    public static IReadOnlyList<string> GetCommentLines([NotNull] XmlDocBlock block) =>
      block.DocComments
        .Select(t => t.CommentText)
        .ToIReadOnlyList();

    protected override IReadOnlyList<ITreeNode> GetDocCommentNodes() => DocCommentBlock.DocComments;

    protected override string GetDocCommentStartText(ITreeNode commentNode) => "///";

    public override void SubTreeChanged()
    {
    }
  }
}
