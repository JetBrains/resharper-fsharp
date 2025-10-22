using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpTypeOwnerDeclaration : IFSharpDeclaration, IFSharpTypeUsageOwnerNode;

public interface IFSharpTypeOwnerDeclaration2 : IFSharpTypeOwnerDeclaration, ITypeOwnerDeclaration;
