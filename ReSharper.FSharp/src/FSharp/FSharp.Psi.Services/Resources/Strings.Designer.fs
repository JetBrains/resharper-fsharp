﻿namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Resources

open System
open JetBrains.Application.I18n
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Util
open JetBrains.Util.Logging

[<global.System.Diagnostics.DebuggerNonUserCode>]
[<global.System.Runtime.CompilerServices.CompilerGenerated>]
type public Strings() =
    static let logger = Logger.GetLogger("JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Resources.Strings")

    static let mutable resourceManager = null

    static do
        CultureContextComponent.Instance.WhenNotNull(Lifetime.Eternal, fun lifetime instance ->
            lifetime.Bracket(
                (fun () ->
                    resourceManager <-
                        lazy
                            instance.CreateResourceManager("JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Resources.Strings",
                                typeof<Strings>.Assembly)),
                (fun () -> resourceManager <- null)
            )
        )

    [<global.System.ComponentModel.EditorBrowsable(global.System.ComponentModel.EditorBrowsableState.Advanced)>]
    static member ResourceManager: JetResourceManager =
        match resourceManager with
            | null -> ErrorJetResourceManager.Instance
            | _ -> resourceManager.Value

    static member ApplicationIsRedundantAndCanBeReplacedWithItsArgument = Strings.ResourceManager.GetString("ApplicationIsRedundantAndCanBeReplacedWithItsArgument")
    static member AsPatternCanBeReplacedWithItsName = Strings.ResourceManager.GetString("AsPatternCanBeReplacedWithItsName")
    static member AttributeSuffixIsRedundantMessage = Strings.ResourceManager.GetString("AttributeSuffixIsRedundantMessage")
    static member CanBeRemovedInIndexers = Strings.ResourceManager.GetString("CanBeRemovedInIndexers")
    static member CanTTakeAddressOfExpressionMessage = Strings.ResourceManager.GetString("CanTTakeAddressOfExpressionMessage")
    static member ClassLocalBindingsCannotBeInlineMessage = Strings.ResourceManager.GetString("ClassLocalBindingsCannotBeInlineMessage")
    static member ConvertToUseBindingMessage = Strings.ResourceManager.GetString("ConvertToUseBindingMessage")
    static member EnumTypeAlwaysRequiresQualifiedAccess = Strings.ResourceManager.GetString("EnumTypeAlwaysRequiresQualifiedAccess")
    static member ExplicitFieldsMatchingIsRedundantAndCanBeReplacedWith = Strings.ResourceManager.GetString("ExplicitFieldsMatchingIsRedundantAndCanBeReplacedWith")
    static member ExtensionAttributeIsRedundantMessage = Strings.ResourceManager.GetString("ExtensionAttributeIsRedundantMessage")
    static member ExtensionMemberInNonExtensionTypeLooksSuspiciousMessage = Strings.ResourceManager.GetString("ExtensionMemberInNonExtensionTypeLooksSuspiciousMessage")
    static member ExtensionMemberShouldBeStaticMessage = Strings.ResourceManager.GetString("ExtensionMemberShouldBeStaticMessage")
    static member ExtensionTypeDoesnTHaveAnyExtensionMembersMessage = Strings.ResourceManager.GetString("ExtensionTypeDoesnTHaveAnyExtensionMembersMessage")
    static member FormatStringCanBeReplacedWithAnInterpolatedString = Strings.ResourceManager.GetString("FormatStringCanBeReplacedWithAnInterpolatedString")
    static member FormatStringCanBeReplacedWithAnInterpolatedString7 = Strings.ResourceManager.GetString("FormatStringCanBeReplacedWithAnInterpolatedString7")
    static member FormatStringCanBeReplacedWithAnInterpolatedStringMessage = Strings.ResourceManager.GetString("FormatStringCanBeReplacedWithAnInterpolatedStringMessage")
    static member IfExpressionCanBeReplacedWithItsCondition = Strings.ResourceManager.GetString("IfExpressionCanBeReplacedWithItsCondition")
    static member IfExpressionCanBeReplacedWithItsCondition8 = Strings.ResourceManager.GetString("IfExpressionCanBeReplacedWithItsCondition8")
    static member InstanceMemberRequiresAParameterToRepresentTheObjectMessage = Strings.ResourceManager.GetString("InstanceMemberRequiresAParameterToRepresentTheObjectMessage")
    static member IsBoundMultipleTimesMessage = Strings.ResourceManager.GetString("IsBoundMultipleTimesMessage")
    static member IsMissingExpressionMessage = Strings.ResourceManager.GetString("IsMissingExpressionMessage")
    static member IsNotMutableMessage = Strings.ResourceManager.GetString("IsNotMutableMessage")
    static member IsStaticMessage = Strings.ResourceManager.GetString("IsStaticMessage")
    static member LambdaCanBeSimplifiedMessage = Strings.ResourceManager.GetString("LambdaCanBeSimplifiedMessage")
    static member LambdaExpressionCanBeReplacedWithBuiltInFunction = Strings.ResourceManager.GetString("LambdaExpressionCanBeReplacedWithBuiltInFunction")
    static member LambdaExpressionCanBeReplacedWithBuiltInFunction5 = Strings.ResourceManager.GetString("LambdaExpressionCanBeReplacedWithBuiltInFunction5")
    static member LambdaExpressionCanBeReplacedWithInnerExpression = Strings.ResourceManager.GetString("LambdaExpressionCanBeReplacedWithInnerExpression")
    static member LambdaExpressionCanBeReplacedWithInnerExpression4 = Strings.ResourceManager.GetString("LambdaExpressionCanBeReplacedWithInnerExpression4")
    static member LambdaExpressionCanBeSimplified = Strings.ResourceManager.GetString("LambdaExpressionCanBeSimplified")
    static member LambdaExpressionCanBeSimplified3 = Strings.ResourceManager.GetString("LambdaExpressionCanBeSimplified3")
    static member LookupOnObjectOfIndeterminateTypeBasedOnInformationPriorToThisProgramPointATypeAnnotationMayBeNeededConstrainTheTypeOfTheObjectMessage = Strings.ResourceManager.GetString("LookupOnObjectOfIndeterminateTypeBasedOnInformationPriorToThisProgramPointATypeAnnotationMayBeNeededConstrainTheTypeOfTheObjectMessage")
    static member Message = Strings.ResourceManager.GetString("Message")
    static member MissingNameAttributeForParameterOrParameterReferenceMessage = Strings.ResourceManager.GetString("MissingNameAttributeForParameterOrParameterReferenceMessage")
    static member MultipleDocumentationEntriesForParameterMessage = Strings.ResourceManager.GetString("MultipleDocumentationEntriesForParameterMessage")
    static member NamespacesCannotContainBindingsMessage = Strings.ResourceManager.GetString("NamespacesCannotContainBindingsMessage")
    static member NamespacesCannotContainExpressionsMessage = Strings.ResourceManager.GetString("NamespacesCannotContainExpressionsMessage")
    static member NewKeywordIsNotRequiredAndCanBeSafelyRemoved = Strings.ResourceManager.GetString("NewKeywordIsNotRequiredAndCanBeSafelyRemoved")
    static member NewKeywordIsRedundantMessage = Strings.ResourceManager.GetString("NewKeywordIsRedundantMessage")
    static member NoDocumentationForParameterMessage = Strings.ResourceManager.GetString("NoDocumentationForParameterMessage")
    static member OnlyClassAndStructTypesMayHaveConstructorsMessage = Strings.ResourceManager.GetString("OnlyClassAndStructTypesMayHaveConstructorsMessage")
    static member OpenDirectiveIsNotRequiredByTheCodeAndCanBeSafelyRemoved = Strings.ResourceManager.GetString("OpenDirectiveIsNotRequiredByTheCodeAndCanBeSafelyRemoved")
    static member OpenDirectiveIsNotRequiredByTheCodeAndCanBeSafelyRemovedMessage = Strings.ResourceManager.GetString("OpenDirectiveIsNotRequiredByTheCodeAndCanBeSafelyRemovedMessage")
    static member ParenthesesAreRedundantIfAttributeHasNoArguments = Strings.ResourceManager.GetString("ParenthesesAreRedundantIfAttributeHasNoArguments")
    static member ParenthesesAreRedundantIfAttributeHasNoArgumentsMessage = Strings.ResourceManager.GetString("ParenthesesAreRedundantIfAttributeHasNoArgumentsMessage")
    static member ParenthesesCanBeSafelyRemovedWithoutChangingCodeSemantics = Strings.ResourceManager.GetString("ParenthesesCanBeSafelyRemovedWithoutChangingCodeSemantics")
    static member PatternCanBeSimplifiedMessage = Strings.ResourceManager.GetString("PatternCanBeSimplifiedMessage")
    static member PropertyCannotBeSetMessage = Strings.ResourceManager.GetString("PropertyCannotBeSetMessage")
    static member ProtectedMembersCannotBeAccessedFromClosuresMessage = Strings.ResourceManager.GetString("ProtectedMembersCannotBeAccessedFromClosuresMessage")
    static member QualifierIsRedundantMessage = Strings.ResourceManager.GetString("QualifierIsRedundantMessage")
    static member RedundantApplication = Strings.ResourceManager.GetString("RedundantApplication")
    static member RedundantApplicationMessage = Strings.ResourceManager.GetString("RedundantApplicationMessage")
    static member RedundantAsPattern = Strings.ResourceManager.GetString("RedundantAsPattern")
    static member RedundantAsPatternMessage = Strings.ResourceManager.GetString("RedundantAsPatternMessage")
    static member RedundantAttributeParenthesesArgument = Strings.ResourceManager.GetString("RedundantAttributeParenthesesArgument")
    static member RedundantAttributeSuffix = Strings.ResourceManager.GetString("RedundantAttributeSuffix")
    static member RedundantAttributeSuffix1 = Strings.ResourceManager.GetString("RedundantAttributeSuffix1")
    static member RedundantConcatenationWithEmptyList = Strings.ResourceManager.GetString("RedundantConcatenationWithEmptyList")
    static member RedundantConcatenationWithEmptyList2 = Strings.ResourceManager.GetString("RedundantConcatenationWithEmptyList2")
    static member RedundantIdentifierEscaping = Strings.ResourceManager.GetString("RedundantIdentifierEscaping")
    static member RedundantIdentifierEscapingMessage = Strings.ResourceManager.GetString("RedundantIdentifierEscapingMessage")
    static member RedundantInIndexer = Strings.ResourceManager.GetString("RedundantInIndexer")
    static member RedundantMessage = Strings.ResourceManager.GetString("RedundantMessage")
    static member RedundantNameQualifier = Strings.ResourceManager.GetString("RedundantNameQualifier")
    static member RedundantNewKeyword = Strings.ResourceManager.GetString("RedundantNewKeyword")
    static member RedundantOpenDirective = Strings.ResourceManager.GetString("RedundantOpenDirective")
    static member RedundantParenthesesMessage = Strings.ResourceManager.GetString("RedundantParenthesesMessage")
    static member RedundantRequireQualifiedAccessAttribute = Strings.ResourceManager.GetString("RedundantRequireQualifiedAccessAttribute")
    static member RedundantRequireQualifiedAccessAttributeMessage = Strings.ResourceManager.GetString("RedundantRequireQualifiedAccessAttributeMessage")
    static member RedundantStringInterpolation = Strings.ResourceManager.GetString("RedundantStringInterpolation")
    static member RedundantStringInterpolationMessage = Strings.ResourceManager.GetString("RedundantStringInterpolationMessage")
    static member RedundantUnionCaseFieldsMatching = Strings.ResourceManager.GetString("RedundantUnionCaseFieldsMatching")
    static member RedundantUnionCaseFieldsMatchingMessage = Strings.ResourceManager.GetString("RedundantUnionCaseFieldsMatchingMessage")
    static member RedundantUseOfEscapingSequences = Strings.ResourceManager.GetString("RedundantUseOfEscapingSequences")
    static member RedundantUseOfQualifierForName = Strings.ResourceManager.GetString("RedundantUseOfQualifierForName")
    static member RemoveRedundantParentheses = Strings.ResourceManager.GetString("RemoveRedundantParentheses")
    static member ReturnMayOnlyBeUsedWithinComputationExpressionsMessage = Strings.ResourceManager.GetString("ReturnMayOnlyBeUsedWithinComputationExpressionsMessage")
    static member StringInterpolationExpressionWithoutArgumentsIsRedundant = Strings.ResourceManager.GetString("StringInterpolationExpressionWithoutArgumentsIsRedundant")
    static member SuccessiveArgumentsShouldBeSeparatedBySpacesTupledOrParenthesizedMessage = Strings.ResourceManager.GetString("SuccessiveArgumentsShouldBeSeparatedBySpacesTupledOrParenthesizedMessage")
    static member TheDeclarationFormLetAndIsOnlyAllowedForRecursiveBindingsConsiderUsingASequenceOfLetBindingsMessage = Strings.ResourceManager.GetString("TheDeclarationFormLetAndIsOnlyAllowedForRecursiveBindingsConsiderUsingASequenceOfLetBindingsMessage")
    static member TheOperatorExprIdxHasBeenUsedOnAnObjectOfIndeterminateTypeBasedOnInformationPriorToThisProgramPointMessage = Strings.ResourceManager.GetString("TheOperatorExprIdxHasBeenUsedOnAnObjectOfIndeterminateTypeBasedOnInformationPriorToThisProgramPointMessage")
    static member TheSelfReferenceIsUnusedAndAddsRuntimeInitializationChecksToMembersInThisAndDerivedTypesMessage = Strings.ResourceManager.GetString("TheSelfReferenceIsUnusedAndAddsRuntimeInitializationChecksToMembersInThisAndDerivedTypesMessage")
    static member TheValueIsUnusedMessage = Strings.ResourceManager.GetString("TheValueIsUnusedMessage")
    static member ThisLiteralPatternDoesNotTakeArgumentsMessage = Strings.ResourceManager.GetString("ThisLiteralPatternDoesNotTakeArgumentsMessage")
    static member ThisRuleWillNeverBeMatchedMessage = Strings.ResourceManager.GetString("ThisRuleWillNeverBeMatchedMessage")
    static member ThisUnionCaseDoesNotTakeArgumentsMessage = Strings.ResourceManager.GetString("ThisUnionCaseDoesNotTakeArgumentsMessage")
    static member TypeAbbreviationsCannotHaveAugmentationsMessage = Strings.ResourceManager.GetString("TypeAbbreviationsCannotHaveAugmentationsMessage")
    static member UnknownParameterNameMessage = Strings.ResourceManager.GetString("UnknownParameterNameMessage")
    static member UpcastIsUnnecessaryMessage = Strings.ResourceManager.GetString("UpcastIsUnnecessaryMessage")
    static member UseBindingsAreNotPermittedInPrimaryConstructorsMessage = Strings.ResourceManager.GetString("UseBindingsAreNotPermittedInPrimaryConstructorsMessage")
    static member UseBindingsAreTreatedAsLetBindingsInModulesMessage = Strings.ResourceManager.GetString("UseBindingsAreTreatedAsLetBindingsInModulesMessage")
    static member UseMessage = Strings.ResourceManager.GetString("UseMessage")
    static member UseSelfId = Strings.ResourceManager.GetString("UseSelfId")
    static member UseSelfId6 = Strings.ResourceManager.GetString("UseSelfId6")
    static member XMLCommentIsNotPlacedOnAValidLanguageElementMessage = Strings.ResourceManager.GetString("XMLCommentIsNotPlacedOnAValidLanguageElementMessage")
    static member YieldMayOnlyBeUsedWithinListArrayAndSequenceExpressionsMessage = Strings.ResourceManager.GetString("YieldMayOnlyBeUsedWithinListArrayAndSequenceExpressionsMessage")