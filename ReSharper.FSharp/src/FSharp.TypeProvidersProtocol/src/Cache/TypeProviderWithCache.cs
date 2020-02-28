using System;
using System.Reflection;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Cache
{
  public class TypeProviderWithCache: ITypeProvider
  {
    private readonly ITypeProvider myTypeProvider;

    public TypeProviderWithCache(ITypeProvider typeProvider)
    {
      myTypeProvider = typeProvider;
    }
    public void Dispose()
    {
      myTypeProvider.Dispose();
    }

    public IProvidedNamespace[] GetNamespaces()
    {
      throw new NotImplementedException();
    }

    public ParameterInfo[] GetStaticParameters(Type typeWithoutArguments)
    {
      throw new NotImplementedException();
    }

    public Type ApplyStaticArguments(Type typeWithoutArguments, string[] typePathWithArguments, object[] staticArguments)
    {
      return myTypeProvider.ApplyStaticArguments(typeWithoutArguments, typePathWithArguments, staticArguments);
    }

    public FSharpExpr GetInvokerExpression(MethodBase syntheticMethodBase, FSharpExpr[] parameters)
    {
      throw new NotImplementedException();
    }

    public byte[] GetGeneratedAssemblyContents(Assembly assembly)
    {
      throw new NotImplementedException();
    }

    public event EventHandler Invalidate;
  }
}
