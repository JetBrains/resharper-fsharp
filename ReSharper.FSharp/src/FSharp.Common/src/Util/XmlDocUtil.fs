module JetBrains.ReSharper.Plugins.FSharp.Util.XmlDocUtil

open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Xml.Impl.Tree
open JetBrains.ReSharper.Psi.Xml.Tree

type IXmlFile with
    /// doc comment without <summary> tag, but interpreted as it is
    member x.IsSimpleSummaryDoc =
        not (x.FindNextNode(fun x ->
              match x with
              | :? XmlWhitespaceToken -> TreeNodeActionType.CONTINUE
              | _ -> TreeNodeActionType.ACCEPT
          ) :? IXmlTag)
