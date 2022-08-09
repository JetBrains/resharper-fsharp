using System.Collections.Generic;
using System.Xml;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public interface IFSharpDocCommentBlock : IDocCommentBlockWithPsi<IDocCommentXmlPsi, DocComment>
  {
  }

  public partial class XmlDocBlock : IFSharpDocCommentBlock
  {

    private volatile IDocCommentXmlPsi myDocCommentXmlPsi;

    public IDocCommentXmlPsi GetXmlPsi()
    {
      Assertion.Assert(IsValid());
      if (myDocCommentXmlPsi == null)
      {
        lock (this) myDocCommentXmlPsi ??= FSharpDocCommentXmlPsi.BuildPsi(this);
      }

      // Assertion.Assert(myDocCommentXmlPsi != null && myDocCommentXmlPsi.DocCommentBlock == this,
      //   "myDocCommentPsi.DocCommentBlock == this");

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

    public DocComment AddDocCommentBefore(DocComment nodeToAdd, DocComment anchor)
    {
      using var cookie = WriteLockCookie.Create(IsPhysical());
      if (anchor != null)
        return ModificationUtil.AddChildBefore(anchor, nodeToAdd);
      if (DocComments.Count > 0)
        return ModificationUtil.AddChildAfter(DocComments.Last(), nodeToAdd);

      return ModificationUtil.AddChild(this, nodeToAdd);
    }

    ///Rewrite more optimal
    public DocComment AddDocCommentAfter(DocComment nodeToAdd, DocComment anchor)
    {
      using var cookie = WriteLockCookie.Create(IsPhysical());
      if (anchor != null)
        return ModificationUtil.AddChildAfter(anchor, nodeToAdd);

      return DocComments.Count > 0
        ? ModificationUtil.AddChildBefore(DocComments[0], nodeToAdd)
        : ModificationUtil.AddChild(this, nodeToAdd);
    }

    public void RemoveDocComment(DocComment docCommentNode)
    {
      //Assertion.Assert(this == DocCommentBlockNodeNavigator.GetByDocCommentNode(docCommentNode), string.Empty);

      using var cookie = WriteLockCookie.Create(IsPhysical());
      ModificationUtil.DeleteChild(docCommentNode);
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
