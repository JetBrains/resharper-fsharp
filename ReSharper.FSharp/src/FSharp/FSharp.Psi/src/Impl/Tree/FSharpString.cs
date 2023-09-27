using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
    public class FSharpString : FSharpToken, ILiteralExpression
    {
        public FSharpString(NodeType nodeType, string text) : base(nodeType, text)
        {
        }

        public ConstantValue ConstantValue => ClrConstantValueFactory.CreateStringValue(GetText(), GetPsiModule());

        public ExpressionAccessType GetAccessType() => ExpressionAccessType.Read;
        public bool IsConstantValue() => true;
        public ITokenNode Literal => this;

        public IType Type() => GetPsiModule().GetPredefinedType().String; // todo: ByteArray strings
        public IExpressionType GetExpressionType() => Type();
        public IType GetImplicitlyConvertedTo() => Type();
    }
}
