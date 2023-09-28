﻿using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public class DeclaredNamespacePart : NamespacePart
  {
    public DeclaredNamespacePart(INamedNamespaceDeclaration declaration)
      : base(declaration, declaration.GetNameRange().StartOffset, declaration.CompiledName)
    {
    }

    public DeclaredNamespacePart(IReader reader) : base(reader)
    {
    }

    protected override ICachedDeclaration2 FindDeclaration(IFile file, ICachedDeclaration2 candidateDeclaration)
    {
      if (Offset < TreeOffset.Zero) return null;
      if (candidateDeclaration is INamedNamespaceDeclaration) return candidateDeclaration;
      return null;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.DeclaredNamespace;

    public override string ToString() => $"FSharp.{GetType().Name}:{ShortName}";
  }
}
