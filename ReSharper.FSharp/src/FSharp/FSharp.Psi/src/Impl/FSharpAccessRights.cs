using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Intentions.QuickFixes;
using JetBrains.ReSharper.Plugins.FSharp.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;

public struct FSharpAccessRights
{
  public bool IsFilePrivate;
  public AccessRights AccessRights;

  internal static readonly FSharpAccessRights Erased = new()
    { IsFilePrivate = true, AccessRights = AccessRights.PRIVATE };

  public override string ToString() =>
    IsFilePrivate
      ? $"file, {AccessRights.ToString().ToLowerInvariant()}"
      : AccessRights.ToString().ToLowerInvariant();
}

public static class FSharpAccessRightUtil
{
  private static bool IsFilePrivate(IFSharpSourceTypeElement typeElement)
  {
    var hasSigFile = false;

    var hasImplPart = false;
    var hasSigPart = false;

    for (var part = typeElement.Parts; part != null; part = part.NextPart)
    {
      var filePart = part.GetRoot() as FSharpProjectFilePart;
      if (filePart == null)
        continue;

      hasImplPart = hasImplPart || !filePart.IsSignature;
      hasSigFile = hasSigFile || filePart.HasPairSignaturePairFile;
      hasSigPart = hasSigPart || filePart.IsSignature;
    }

    return hasImplPart && hasSigFile && !hasSigPart;
  }

  public static FSharpAccessRights GetFSharpAccessRights(this FSharpMetadataEntity entity) =>
    entity != null
      ? new FSharpAccessRights { IsFilePrivate = false, AccessRights = entity.AccessRights }
      : FSharpAccessRights.Erased;

  public static FSharpAccessRights GetFSharpAccessRights(this IFSharpTypeElement typeElement)
  {
    if (typeElement is IFSharpCompiledTypeElement compiledTypeElement)
      return compiledTypeElement.FSharpAccessRights;

    if (typeElement is IFSharpSourceTypeElement sourceTypeElement)
    {
      var typePart = typeElement.GetPart<IFSharpTypePart>().NotNull();
      var accessRights = typePart.SourceAccessRights;
      return new FSharpAccessRights { IsFilePrivate = IsFilePrivate(sourceTypeElement), AccessRights = accessRights };
    }

    return FSharpAccessRights.Erased;
  }

  private static IEnumerable<IClrDeclaredElement> GetOwners(ITreeNode context)
  {
    var typeDecl = context.GetContainingNode<ITypeDeclaration>();
    if (typeDecl is { DeclaredElement: { } typeElement })
    {
      yield return typeElement;

      var containingType = typeElement.GetContainingType();
      while (containingType != null)
      {
        yield return containingType;
        containingType = containingType.GetContainingType();
      }

      foreach (var ns in GetContainingNamespaces(typeElement.GetContainingNamespace()))
        yield return ns;
    }
    else
    {
      var nsDecl = context.GetContainingNode<INamespaceDeclaration>();
      foreach (var ns in GetContainingNamespaces(nsDecl?.DeclaredElement))
        yield return ns;
    }

    IEnumerable<IClrDeclaredElement> GetContainingNamespaces([CanBeNull] INamespace ns)
    {
      while (ns != null)
      {
        yield return ns;
        ns = ns.GetContainingNamespace();
      }

    }
  }

  [NotNull] private static IClrDeclaredElement GetOwner(ITypeElement typeElement) =>
    (IClrDeclaredElement)typeElement.GetContainingType() ?? typeElement.GetContainingNamespace();

  private static bool IsTheSameOwner(ITypeElement typeElement, ITreeNode context)
  {
    var typeModule = typeElement.Module;
    var contextModule = context.GetPsiModule();
    if (!typeModule.Equals(contextModule))
      return false;

    var owner = GetOwner(typeElement);
    return GetOwners(context).Contains(owner);
  }

  private static IPsiSourceFile GetDefiningSourceFile(IFSharpSourceTypeElement fsSourceTypeElement)
  {
    return fsSourceTypeElement.DefiningDeclaration?.GetSourceFile();
  }

  // todo: recursive type groups
  // todo: recursive access
  private static bool IsAccessibleInsideTheSameFile(IDeclaration decl, ITreeNode context)
  {
    if (decl == null)
      return false;

    
    if (decl.GetDocumentStartOffset() < context.GetDocumentStartOffset())
      return true;

    var commonParent = decl.FindLCA(context);
    foreach (var moduleDecl in commonParent.ContainingNodes<IDeclaredModuleLikeDeclaration>(true))
    {
      if (moduleDecl.IsRecursive)
        return true;
    }

    return false;
  }

  public static bool IsAccessible(ITypeElement typeElement, ITreeNode context)
  {
    if (typeElement is IFSharpTypeElement fsTypeElement)
    {
      var fsAccessRights = fsTypeElement.GetFSharpAccessRights();
      if (fsAccessRights.IsFilePrivate &&
          context.GetSourceFile() is { } sourceFile && !fsTypeElement.HasDeclarationsIn(sourceFile))
        return false;

      var accessRights = fsAccessRights.AccessRights;
      if (accessRights == AccessRights.PRIVATE && !IsTheSameOwner(fsTypeElement, context))
        return false;

      if (fsTypeElement is IFSharpSourceTypeElement fsSourceTypeElement)
      {
        var typeModule = fsSourceTypeElement.Module;
        var contextModule = context.GetPsiModule();
        if (!typeModule.Equals(contextModule))
          return accessRights == AccessRights.PUBLIC ||
                 accessRights == AccessRights.INTERNAL && typeModule.AreInternalsVisibleTo(contextModule);

        var contextSourceFile = context.GetSourceFile();
        var definingSourceFile = GetDefiningSourceFile(fsSourceTypeElement);
        if (contextSourceFile != definingSourceFile)
        {
          var fcsProjectProvider = typeModule.GetSolution().GetComponent<IFcsProjectProvider>();

          var definingFileIndex = fcsProjectProvider.GetFileIndex(definingSourceFile);
          var contextFileIndex = fcsProjectProvider.GetFileIndex(contextSourceFile);
          return definingFileIndex < contextFileIndex;
        }

        return IsAccessibleInsideTheSameFile(fsSourceTypeElement.DefiningDeclaration, context);
      }

      return true;
    }

    if (!ImportTypeUtil.TypeIsVisible(typeElement, context))
      return false;

    return true;
  }
}
