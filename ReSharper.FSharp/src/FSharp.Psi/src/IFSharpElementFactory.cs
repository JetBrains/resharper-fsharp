using System;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using Microsoft.FSharp.Collections;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public enum TypeUsageContext
  {
    TopLevel,
    ParameterSignature,
    Signature,
  }

  public interface IFSharpElementFactory
  {
    IOpenStatement CreateOpenStatement(string ns);

    IWildPat CreateWildPat();
    IMemberSelfId CreateSelfId(string name);

    IParenExpr CreateParenExpr();
    IConstExpr CreateConstExpr(string text);

    IPrefixAppExpr CreateAppExpr(string funcName, IFSharpExpression arg);
    IPrefixAppExpr CreateAppExpr(IFSharpExpression funExpr, IFSharpExpression argExpr, bool addSpace);
    IFSharpExpression CreateBinaryAppExpr(string opName, IFSharpExpression left, IFSharpExpression right);
    IFSharpExpression CreateSetExpr(IFSharpExpression left, IFSharpExpression right);

    IFSharpExpression CreateExpr(string expr);
    IReferenceExpr CreateReferenceExpr(string expr);

    ILetOrUseExpr CreateLetBindingExpr(string bindingName);
    ILetBindingsDeclaration CreateLetModuleDecl(string bindingName);

    IBinaryAppExpr CreateIgnoreApp(IFSharpExpression expr, bool newLine);
    IRecordFieldBinding CreateRecordFieldBinding(string fieldName, bool addSemicolon);
    IRecordFieldDeclaration CreateRecordFieldDeclaration(bool isMutable, string fieldName, ITypeUsage typeUsage);
    IFSharpPattern CreatePattern(string text, bool topLevel);
    IParenPat CreateParenPat();
    ITypedPat CreateTypedPat(IFSharpPattern pattern, ITypeUsage typeUsage);

    ITypeUsage CreateTypeUsage(string typeUsage, TypeUsageContext context);

    IReturnTypeInfo CreateReturnTypeInfo(ITypeUsage typeSignature);

    IMatchExpr CreateMatchExpr(IFSharpExpression expr);
    IMatchClause CreateMatchClause();

    IForEachExpr CreateForEachExpr(IFSharpExpression expr);

    IReferenceExpr AsReferenceExpr(ITypeReferenceName typeReference);

    IExpressionReferenceName CreateExpressionReferenceName(string referenceName);
    ITypeReferenceName CreateTypeReferenceName(string referenceName);

    IAttributeList CreateEmptyAttributeList();
    IAttribute CreateAttribute(string attrName);

    FSharpList<IParametersPatternDeclaration> CreateMemberParamDeclarations(FSharpList<FSharpList<Tuple<string, FSharpType>>> curriedParameterNames, bool isSpaceAfterComma, bool addTypes, bool preferNoParens, FSharpDisplayContext displayContext);
    IMemberDeclaration CreateMemberBindingExpr(string bindingName, FSharpList<string> typeParameters, FSharpList<IParametersPatternDeclaration> args);
    IMemberDeclaration CreatePropertyWithAccessor(string propertyName, string accessorName, FSharpList<IParametersPatternDeclaration> args);

    ITypeParameterDeclarationList CreateTypeParameterOfTypeList(FSharpList<string> names);

    IAccessorsNamesClause CreateAccessorsNamesClause(bool withGetter, bool withSetter);
  }
}
