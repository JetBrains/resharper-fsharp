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
using JetBrains.ReSharper.Psi.FSharp.Impl;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [SolutionComponent]
  public class FSharpIdentifierTooltipProvider : IdentifierTooltipProvider<FSharpLanguage>
  {
    private readonly ISolution mySolution;
    private readonly ILogger myLogger;

    public FSharpIdentifierTooltipProvider(Lifetime lifetime, ISolution solution,
      IDeclaredElementDescriptionPresenter presenter, ILogger logger) : base(lifetime, solution, presenter)
    {
      mySolution = solution;
      myLogger = logger;
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
      var checkResults = psiFile?.GetParseAndCheckResults()?.Value.CheckResults;
      var token = psiFile?.FindTokenAt(documentRange.StartOffset) as FSharpIdentifierToken;
      if (checkResults == null || token == null) return string.Empty;

      var coords = document.GetCoordsByOffset(token.GetTreeEndOffset().Offset);
      var names = FSharpImplUtil.GetQualifiersAndName(token);
      var lineText = sourceFile.Document.GetLineText(coords.Line);

      try
      {
        return GetTooltip(checkResults, names, coords, lineText);
      }
      catch (TimeoutException)
      {
        Logger.LogError("Getting tooltip: {0}: {1}", sourceFile.GetLocation().FullPath, coords);
        return string.Empty;
      }
    }

    [NotNull]
    private string GetTooltip([NotNull] FSharpCheckFileResults checkResults, [NotNull] string[] names,
      DocumentCoords coords, [NotNull] string lineText)
    {
      // todo: provide tooltip for #r strings in fsx, should pass String tag
      var getTooltipAsync = checkResults.GetToolTipText((int) coords.Line + 1,
        (int) coords.Column, lineText, ListModule.OfArray(names), FSharpTokenTag.Identifier, FSharpOption<string>.None);
        var tooltips = FSharpAsyncUtil.RunSynchronouslySafe(getTooltipAsync, myLogger, "Getting F# tooltip", 2000)?.Item;
      
        if (tooltips == null)
          return string.Empty;

        var tooltipsTexts = new List<string>();
        foreach (var tooltip in tooltips)
        {
          var overloads = tooltip as FSharpToolTipElement<string>.Group;
          if (overloads != null)
            tooltipsTexts.AddRange(overloads.Item.Select(overload => GetTooltipText(overload.MainDescription, overload.XmlDoc)));//, overload.Item2)));
        }
        return tooltipsTexts.Join("_RIDER_HORIZONTAL_LINE_TOOLTIP_SEPARATOR_");
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