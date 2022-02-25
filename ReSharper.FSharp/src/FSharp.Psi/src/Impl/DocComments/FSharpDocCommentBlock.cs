using System.Collections.Generic;
using System.Xml;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  public partial class XmlDocBlock: IFSharpDocCommentBlock
  {
    private volatile IDocCommentXmlPsi myDocCommentXmlPsi;

    public IDocCommentXmlPsi GetXmlPsi()
    {
      Assertion.Assert(IsValid(), "IsValid()");
      if (myDocCommentXmlPsi == null)
      {
        lock (this)
        {
          myDocCommentXmlPsi ??= FSharpDocCommentXmlPsi.BuildPsi(this);
        }
      }

      // Assertion.Assert(myDocCommentXmlPsi != null && myDocCommentXmlPsi.DocCommentBlock == this,
      //   "myDocCommentPsi.DocCommentBlock == this");

      return myDocCommentXmlPsi;
    }

    public TreeNodeCollection<IFSharpDocCommentNode> DocComments
    {
      get
      {
        var list = new LocalList<IFSharpDocCommentNode>();
        for (var node = FirstChild; node != null; node = node.NextSibling)
          if (node is IFSharpDocCommentNode comment)
            list.Add(comment);

        return new TreeNodeCollection<IFSharpDocCommentNode>(list.ToArray());
      }
    }

    public IFSharpDocCommentNode AddDocCommentBefore(IFSharpDocCommentNode nodeToAdd, IFSharpDocCommentNode anchor)
    {
      using (WriteLockCookie.Create(IsPhysical()))
      {
        if (anchor != null)
          return ModificationUtil.AddChildBefore(anchor, nodeToAdd);
        if (DocComments.Count > 0)
          return ModificationUtil.AddChildAfter(DocComments.Last(), nodeToAdd);

        return ModificationUtil.AddChild(this, nodeToAdd);
      }
    }

    //Rewrite more optimal
    public IFSharpDocCommentNode AddDocCommentAfter(IFSharpDocCommentNode nodeToAdd, IFSharpDocCommentNode anchor)
    {
      using (WriteLockCookie.Create(IsPhysical()))
      {
        if (anchor != null)
          return ModificationUtil.AddChildAfter(anchor, nodeToAdd);

        return DocComments.Count > 0
          ? ModificationUtil.AddChildBefore(DocComments[0], nodeToAdd)
          : ModificationUtil.AddChild(this, nodeToAdd);
      }
    }

    public void RemoveDocComment(IFSharpDocCommentNode docCommentNode)
    {
      //Assertion.Assert(this == DocCommentBlockNodeNavigator.GetByDocCommentNode(docCommentNode), string.Empty);

      using (WriteLockCookie.Create(IsPhysical()))
      {
        ModificationUtil.DeleteChild(docCommentNode);
      }
    }

    public XmlNode GetXML(ITypeMember element)
    {
      var lines = FSharpDocCommentXmlPsi.GetCommentLines(this);

      //if (!DocCommentBlockUtil.TryGetXml(lines, element, out var node))
      // return node;

      // lock (this)
      // {
      //   if (MajorReferences == null)
      //     Parse();
      // }

      DocCommentBlockUtil.TryGetXml(lines, element, out var node);
      return node;
    }


    public IReadOnlyCollection<DocCommentError> GetErrors() => EmptyList<DocCommentError>.Instance;
  }
}
