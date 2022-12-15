using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches.AnnotatedEntities;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Annotations
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpAnnotatedMembersCacheProcessor : IAnnotatedEntitiesCacheProcessor
  {
    public void Process(IFile file, HashSet<string> attributeNames, AnnotatedEntitiesSet context)
    {
      var fsFile = file as IFSharpFile;
      fsFile?.ProcessThisAndDescendants(new Processor(attributeNames, context));
    }

    private class Processor : TreeNodeVisitor, IRecursiveElementProcessor
    {
      private readonly HashSet<string> myAttributeNames;
      private readonly AnnotatedEntitiesSet myContext;

      public Processor(HashSet<string> attributeNames, AnnotatedEntitiesSet context)
      {
        myAttributeNames = attributeNames;
        myContext = context;
      }

      public bool ProcessingIsFinished => false;

      public bool InteriorShouldBeProcessed(ITreeNode element) => element is not IChameleonNode;

      public void ProcessBeforeInterior(ITreeNode element)
      {
        if (element is IFSharpTreeNode node)
          node.Accept(this);
      }

      public void ProcessAfterInterior(ITreeNode element)
      {
      }

      public override void VisitNode(ITreeNode node)
      {
        //TODO: inherit IAttributesOwnerDeclaration from IDeclaration?
        if (node is IAttributesOwnerDeclaration attributesOwnerDeclaration and IDeclaration decl)
          CollectAttributes(attributesOwnerDeclaration, decl.DeclaredName);
      }

      public override void VisitFSharpTypeDeclaration(IFSharpTypeDeclaration fSharpTypeDeclaration) =>
        CollectAttributes(fSharpTypeDeclaration, fSharpTypeDeclaration.CLRName);

      public override void VisitPrimaryConstructorDeclaration(IPrimaryConstructorDeclaration primaryConstructor) =>
        VisitConstructor(primaryConstructor);

      public override void VisitSecondaryConstructorDeclaration(ISecondaryConstructorDeclaration ctor) =>
        VisitConstructor(ctor);

      public override void VisitTopBinding(ITopBinding topBinding)
      {
        var headPattern = topBinding.HeadPattern;
        if (headPattern == null) return;

        foreach (var declaration in headPattern.Declarations)
        {
          if (declaration is not ITypeMemberDeclaration decl) continue;

          var declaredName = decl.DeclaredName;
          CollectAttributes(topBinding, declaredName);

          if (declaredName == SharedImplUtil.MISSING_DECLARATION_NAME) continue;

          VisitParametersOwner(topBinding, declaredName);

          if (topBinding.ChameleonExpression.IsLambdaExpression())
            VisitBindingNestedLambda(topBinding.Expression, declaredName);
        }
      }

      public override void VisitMemberDeclaration(IMemberDeclaration memberDeclaration)
      {
        var memberName = memberDeclaration.DeclaredName;
        VisitParametersOwner(memberDeclaration, memberName);

        foreach (var accessor in memberDeclaration.AccessorDeclarationsEnumerable)
          VisitParametersOwner(accessor, memberName);

        base.VisitMemberDeclaration(memberDeclaration);
      }

      private void VisitParametersOwner(IParameterOwnerMemberDeclaration decl, string memberName)
      {
        foreach (var parameterDeclaration in decl.ParameterPatterns)
          VisitPatternDeclaration(parameterDeclaration, memberName);
      }

      private void VisitConstructor(IConstructorDeclaration constructorDeclaration)
      {
        var typeDeclaration = constructorDeclaration.GetContainingTypeDeclaration();
        if (typeDeclaration == null) return;

        var defaultMemberName = typeDeclaration.DeclaredName;
        VisitParametersOwner(constructorDeclaration, defaultMemberName);

        if (AttributeUtil.HasAttributeSuffix(defaultMemberName, out var nameWithoutAttributeSuffix))
          VisitParametersOwner(constructorDeclaration, nameWithoutAttributeSuffix);

        VisitNode(constructorDeclaration);
      }

      private void CollectAttributes(IAttributesOwnerDeclaration owner, string memberName)
      {
        foreach (var attribute in owner.AttributesEnumerable)
        foreach (var attributeName in GetAttributeNames(attribute))
        {
          if (attributeName != null && myAttributeNames.TryGetValue(attributeName, out var internedName))
          {
            if (owner is ITypeDeclaration)
              myContext.AttributeToTypes.Add(internedName, memberName);
            else
            {
              var containingTypeName = (owner as ITypeMemberDeclaration)?.GetContainingTypeDeclaration()?.CLRName;
              myContext.AttributeToMembers.Add(internedName, memberName);
              if (!string.IsNullOrEmpty(containingTypeName))
                myContext.AttributeToFullMembers.Add(internedName,
                  new FullTypeMemberName(containingTypeName, memberName));
            }
          }
        }
      }

      private static IEnumerable<string> GetAttributeNames([NotNull] IAttribute attribute)
      {
        var referenceName = attribute.ReferenceName;
        if (referenceName?.ShortName is { } name && name != SharedImplUtil.MISSING_DECLARATION_NAME)
        {
          yield return name;
          yield return name + StandardTypeNames.AttributeSuffix;
        }
      }

      private void VisitBindingNestedLambda(IFSharpExpression expr, string bindingName)
      {
        if (expr is not LambdaExpr lambdaExpr) return;

        foreach (var parameterDeclaration in lambdaExpr.PatternsEnumerable)
          VisitPatternDeclaration(parameterDeclaration, bindingName);

        VisitBindingNestedLambda(lambdaExpr.Expression, bindingName);
      }

      private void VisitPatternDeclaration(IFSharpPattern patternDecl, string parametersOwnerName)
      {
        if (patternDecl.IgnoreInnerParens() is IAttribPat attributedParam)
          CollectAttributes(attributedParam, parametersOwnerName);
      }
    }
  }
}
