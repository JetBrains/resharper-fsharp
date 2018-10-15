using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpPreprocessor
  {
    private abstract class Expression
    {
      public abstract bool Accept(FSharpPreprocessor fSharpPreprocessor);
      public virtual bool IsError => false;

      public Expression Not() => new NotExpression(this);
      public Expression And(Expression expr) => new AndExpression(this, expr);
      public Expression Or(Expression expr) => new OrExpression(this, expr);
    }

    private sealed class ErrorExpression : Expression
    {
      public override bool Accept(FSharpPreprocessor fSharpPreprocessor) => fSharpPreprocessor.Visit(this);
      public override bool IsError => true;
    }

    private sealed class SymbolExpression : Expression
    {
      public readonly string Symbol;
    
      public SymbolExpression(string symbol)
      {
        Symbol = symbol;
      }

      public override bool Accept(FSharpPreprocessor fSharpPreprocessor) => fSharpPreprocessor.Visit(this);
    }

    private sealed class NotExpression : Expression
    {
      public readonly Expression Expr;

      public NotExpression(Expression expr)
      {
        Expr = expr;
      }

      public override bool Accept(FSharpPreprocessor fSharpPreprocessor) => fSharpPreprocessor.Visit(this);
    }

    private sealed class AndExpression : Expression
    {
      public readonly Expression Left;
      public readonly Expression Right;

      public AndExpression(Expression left, Expression right)
      {
        Left = left;
        Right = right;
      }

      public override bool Accept(FSharpPreprocessor fSharpPreprocessor) => fSharpPreprocessor.Visit(this);
    }

    private sealed class OrExpression : Expression
    {
      public readonly Expression Left;
      public readonly Expression Right;

      public OrExpression(Expression left, Expression right)
      {
        Left = left;
        Right = right;
      }

      public override bool Accept(FSharpPreprocessor fSharpPreprocessor) => fSharpPreprocessor.Visit(this);
    }

    private ExpressionParser myParser;
    private HashSet<string> myDefinedConststants;

    public bool Preprocess(ILexer lexer, HashSet<string> definedConstants)
    {
      myParser = new ExpressionParser();
      myDefinedConststants = definedConstants;
      var expr =  myParser.Parse(lexer);
      return expr.Accept(this);
    }

    private bool Visit(NotExpression notExpr) => !notExpr.Expr.Accept(this);
    private bool Visit(AndExpression andExpr) => andExpr.Left.Accept(this) && andExpr.Right.Accept(this);
    private bool Visit(OrExpression orExpr) => orExpr.Left.Accept(this) || orExpr.Right.Accept(this);
    private bool Visit(SymbolExpression symbolExpr) => myDefinedConststants.Contains(symbolExpr.Symbol);
    private bool Visit(ErrorExpression errorExpr) => false;
    private class ExpressionParser
    {    
      private static readonly NodeTypeSet mySkippedToken = new NodeTypeSet(
        FSharpTokenType.WHITESPACE,
        FSharpTokenType.PP_IF_SECTION);

      private TokenNodeType TokenType()
      {
        while (mySkippedToken[myLexer.TokenType])
          myLexer.Advance();
        return myLexer.TokenType;
      }

      private ILexer myLexer;

      public Expression Parse(ILexer lexer)
      {
        myLexer = lexer;
        return ParseExpression();
      }

      private Expression ParseExpression()
      {
        var expr = ParseNotExpression();
        var andExpr = ParseAndExpression(expr);
        var orExpr = ParseOrExpression(andExpr);
        return orExpr;
      }

      private Expression ParseOrExpression(Expression expr)
      {
        while (true)
        {
          if (expr.IsError)
            return expr;
          switch (TokenType())
          {
            case var token when token == FSharpTokenType.PP_OR:
              myLexer.Advance();
              var notExpr = ParseNotExpression();
              var andExpr = ParseAndExpression(expr.Or(notExpr));
              expr = andExpr;
              continue;
            default:
              return expr;
          }
        }
      }

      private Expression ParseAndExpression(Expression expr)
      {
        while (true)
        {
          if (expr.IsError)
            return expr;
          switch (TokenType())
          {
            case var token when token == FSharpTokenType.PP_AND:
              myLexer.Advance();
              var notExpr = ParseNotExpression();
              expr = expr.And(notExpr);
              continue;
            default:
              return expr;
          }
        }
      }

      private Expression ParseNotExpression()
      {
        switch (TokenType())
        {
          case var token when token == null: return new ErrorExpression(); 
          case var token when token == FSharpTokenType.PP_NOT:
            myLexer.Advance();
            return ParseNotExpression().Not();
          default: return ParseAtomExpression();
        }
      }

      private Expression ParseAtomExpression()
      {
        switch (TokenType())
        {
          case var token when token == FSharpTokenType.PP_CONDITIONAL_SYMBOL:
            var sym = new SymbolExpression(myLexer.GetCurrTokenText());
            myLexer.Advance();
            return sym;
          case var token when token == FSharpTokenType.PP_LPAR:
            myLexer.Advance();
            var expr = ParseExpression();
            if (TokenType() != FSharpTokenType.PP_RPAR)
              return new ErrorExpression();
            myLexer.Advance();
            return expr;
          default:
            myLexer.Advance();
            return new ErrorExpression();
        }
      }
    }
  }
}
