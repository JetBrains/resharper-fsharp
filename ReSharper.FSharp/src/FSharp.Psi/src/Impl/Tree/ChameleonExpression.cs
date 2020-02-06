using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using static FSharp.Compiler.Ast;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ChameleonExpression : IChameleonNode
  {
    [CanBeNull] public SynExpr SynExpr { get; }

    [NotNull] private readonly object mySyncObject = new object();
    private bool myOpened;

    public ChameleonExpression([CanBeNull] SynExpr expr) =>
      SynExpr = expr;

    public IChameleonNode ReSync(CachingLexer cachingLexer, TreeTextRange changedRange, int insertedTextLen) =>
      null; // No reparse for now.

    public bool IsOpened
    {
      get
      {
        lock (mySyncObject)
        {
          return myOpened;
        }
      }
    }

    public override ITreeNode FirstChild
    {
      get
      {
        lock (mySyncObject)
        {
          if (!myOpened)
            OpenChameleon();

          return firstChild;
        }
      }
    }

    public override ITreeNode LastChild
    {
      get
      {
        lock (mySyncObject)
        {
          if (!myOpened)
            OpenChameleon();

          return lastChild;
        }
      }
    }

    private void OpenChameleon()
    {
      Assertion.Assert(!myOpened, "!myOpened");
      Assertion.Assert(firstChild == lastChild && firstChild is IClosedChameleonBody,
        "One ChameleonElement child but found also {0}", lastChild.NodeType);

      var node = ((IClosedChameleonBody) firstChild).Parse(parser =>
        ((IFSharpParser) parser).ParseExpression(this, FSharpFile.StandaloneDocument));

      var oldLength = firstChild.GetTextLength();
      var newLength = node.GetTextLength();
      Assertion.Assert(oldLength == newLength,
        "Chameleon length is different after opening; old: {0}, new: {1}", oldLength, newLength);

      DeleteChildRange(firstChild, lastChild);
      AppendNewChild((TreeElement) node);

      myOpened = true;
    }

    public override int GetTextLength()
    {
      lock (mySyncObject)
      {
        return base.GetTextLength();
      }
    }

    public override StringBuilder GetText(StringBuilder to)
    {
      lock (mySyncObject)
      {
        return base.GetText(to);
      }
    }

    public override IBuffer GetTextAsBuffer()
    {
      lock (mySyncObject)
      {
        return base.GetTextAsBuffer();
      }
    }

    protected override TreeElement DeepClone(TreeNodeCopyContext context)
    {
      lock (mySyncObject)
      {
        return base.DeepClone(context);
      }
    }


    public override IChameleonNode FindChameleonWhichCoversRange(TreeTextRange textRange)
    {
      lock (mySyncObject)
      {
        if (textRange.ContainedIn(TreeTextRange.FromLength(GetTextLength())))
        {
          if (!myOpened)
            return this;

          return base.FindChameleonWhichCoversRange(textRange) ?? this;
        }
      }

      return null;
    }

    public override ITreeNode FindNodeAt(TreeTextRange treeRange)
    {
      if (treeRange.IntersectsOrContacts(TreeTextRange.FromLength(GetTextLength())))
      {
        return base.FindNodeAt(treeRange);
      }

      return null;
    }

    public override void FindNodesAtInternal(TreeTextRange relativeRange, List<ITreeNode> result,
      bool includeContainingNodes)
    {
      if (relativeRange.ContainedIn(TreeTextRange.FromLength(GetTextLength())))
      {
        base.FindNodesAtInternal(relativeRange, result, includeContainingNodes);
      }
    }
  }
}
