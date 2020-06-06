using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpElementFactory
  {
    IOpenStatement CreateOpenStatement(string ns);
    IWildPat CreateWildPat();

    IParenExpr CreateParenExpr();
    IParenExpr CreateParenExpr(IFSharpExpression fsExpr);

    IConstExpr CreateConstExpr(string text);

    IPrefixAppExpr CreateAppExpr(bool addSpace);
    IPrefixAppExpr CreateAppExpr(string funcName, IFSharpExpression arg);
    IPrefixAppExpr CreateAppExpr(IFSharpExpression funExpr, IFSharpExpression argExpr, bool addSpace);
    IFSharpExpression CreateBinaryAppExpr(string opName, IFSharpExpression left, IFSharpExpression right);
    IFSharpExpression CreateSetExpr(IFSharpExpression left, IFSharpExpression right);

    IFSharpExpression CreateExpr(string expr);
    IFSharpExpression CreateReferenceExpr(string expr);

    ILetOrUseExpr CreateLetBindingExpr(string bindingName);
    ILetModuleDecl CreateLetModuleDecl(string bindingName);

    IBinaryAppExpr CreateIgnoreApp(IFSharpExpression expr, bool newLine);
    IRecordExprBinding CreateRecordExprBinding(string fieldName, bool addSemicolon);

    IMatchExpr CreateMatchExpr(IFSharpExpression expr);
    IMatchClause CreateMatchClause();

    IForEachExpr CreateForEachExpr(IFSharpExpression expr);

    IReferenceExpr AsReferenceExpr(ITypeReferenceName typeReference);
    
    IAttributeList CreateEmptyAttributeList();
    IAttribute CreateAttribute(string attrName);
  }
}
