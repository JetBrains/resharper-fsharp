using System.Collections.Generic;
using System.Xml;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public interface IFSharpDocCommentBlock : IDocCommentBlockWithPsi<IDocCommentXmlPsi, DocComment>
  {
  }

  public partial class XmlDocBlock : IFSharpDocCommentBlock
  {
    private IDocCommentXmlPsi myDocCommentXmlPsi;

    public IDocCommentXmlPsi GetXmlPsi()
    {
      Assertion.Assert(IsValid());

      // TODO: compile/bind references in XmlDoc
      myDocCommentXmlPsi ??= FSharpDocCommentXmlPsi.BuildPsi(this);
      return myDocCommentXmlPsi;
    }

    public TreeNodeCollection<DocComment> DocComments
    {
      get
      {
        var list = new LocalList<DocComment>();
        for (var node = FirstChild; node != null; node = node.NextSibling)
          if (node is DocComment comment)
            list.Add(comment);

        return new TreeNodeCollection<DocComment>(list.ToArray());
      }
    }

    public DocComment AddDocCommentBefore(DocComment nodeToAdd, DocComment anchor) => nodeToAdd;
    public DocComment AddDocCommentAfter(DocComment nodeToAdd, DocComment anchor) => nodeToAdd;

    public void RemoveDocComment(DocComment docCommentNode)
    {
    }

    public XmlNode GetXML(ITypeMember element)
    {
      var lines = FSharpDocCommentXmlPsi.GetCommentLines(this);
      DocCommentBlockUtil.TryGetXml(lines, element, out var node);
      return node;
    }

    public IReadOnlyCollection<DocCommentError> GetErrors() => EmptyList<DocCommentError>.Instance;
  }
}
