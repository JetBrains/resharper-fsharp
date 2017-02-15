using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.RichText;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [SolutionComponent]
  public class FSharpIdentifierTooltipProvider : IdentifierTooltipProvider<FSharpLanguage>
  {
    private readonly ISolution mySolution;

    public FSharpIdentifierTooltipProvider(Lifetime lifetime, ISolution solution,
      IDeclaredElementDescriptionPresenter presenter) : base(lifetime, solution, presenter)
    {
      mySolution = solution;
    }

    [NotNull]
    public override string GetTooltip(IHighlighter highlighter)
    {
      if (!ShouldShowTooltip(highlighter)) return string.Empty;
      var psiServices = mySolution.GetPsiServices();
      if (!psiServices.Files.AllDocumentsAreCommitted || psiServices.Caches.HasDirtyFiles) return string.Empty;

      var document = highlighter.Document;
      var sourceFile = document.GetPsiSourceFile(mySolution);
      if (sourceFile == null || !sourceFile.IsValid()) return string.Empty;

      var documentRange = new DocumentRange(document, highlighter.Range);
      var psiFile = GetPsiFile(sourceFile, documentRange) as IFSharpFile;
      var checkResults = psiFile?.CheckResults;
      var token = psiFile?.FindTokenAt(documentRange.StartOffset) as FSharpToken;
      if (checkResults == null || token == null) return string.Empty;

      var coords = document.GetCoordsByOffset(highlighter.Range.EndOffset);
      var names = new[] {token.GetText()}; // todo: provide qualifiers
      return GetTooltip(checkResults, names, coords);
    }


    private static string GetTooltip(FSharpCheckFileResults checkResults, string[] names, DocumentCoords coords)
    {
      // todo: provide tooltip for #r strings in fsx, should pass String tag
      var getTooltipAsync = checkResults.GetToolTipTextAlternate((int) coords.Line + 1,
        (int) coords.Column, string.Empty, ListModule.OfArray(names), FSharpTokenTag.Identifier);

      var tooltips = FSharpAsync.RunSynchronously(getTooltipAsync, null, null).Item;
      var tooltipsTexts = new List<string>();
      foreach (var tooltip in tooltips)
      {
        var singleTooltip = tooltip as FSharpToolTipElement<string>.Single;
        if (singleTooltip != null)
          tooltipsTexts.Add(GetTooltipText(singleTooltip.Item1, singleTooltip.Item2));

        var overloads = tooltip as FSharpToolTipElement<string>.Group;
        if (overloads != null)
          tooltipsTexts.AddRange(overloads.Item.Select(overload => GetTooltipText(overload.Item1, overload.Item2)));
      }
      return tooltipsTexts.Join("\n----\n");
    }

    [CanBeNull]
    public static string GetXmlDocText(FSharpXmlDoc xmlDoc)
    {
      if (xmlDoc.IsNone) return null;
      if (xmlDoc.IsText) return ((FSharpXmlDoc.Text) xmlDoc).Item;
      if (xmlDoc.IsXmlDocFileSignature)
      {
        var sig = xmlDoc as FSharpXmlDoc.XmlDocFileSignature;
        if (sig == null) return null;
        var s1 = sig.Item1;
        var s2 = sig.Item2;
        // todo: get doc from xml
      }
      return null;
    }

    [NotNull]
    public static string GetTooltipText(string text, FSharpXmlDoc xmlDoc)
    {
      var xmlDocText = GetXmlDocText(xmlDoc);
      return xmlDocText != null ? text + xmlDocText : text;
    }

    public override RichTextBlock GetRichTooltip(IHighlighter highlighter)
    {
      return new RichTextBlock(GetTooltip(highlighter));
    }
  }
}