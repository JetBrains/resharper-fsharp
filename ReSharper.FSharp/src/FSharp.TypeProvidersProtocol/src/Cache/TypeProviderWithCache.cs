using System;
using System.Reflection;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class TypeProviderWithCache : ITypeProvider
  {
    private readonly ITypeProvider myTypeProvider;
    private IProvidedNamespace[] myProvidedNamespaces;

    public TypeProviderWithCache(ITypeProvider typeProvider)
    {
      myTypeProvider = typeProvider;
      myTypeProvider.Invalidate += TypeProviderOnInvalidate;
    }

    public void Dispose()
    {
      myTypeProvider.Dispose();
    }

    public IProvidedNamespace[] GetNamespaces() =>
      myProvidedNamespaces ?? (myProvidedNamespaces = myTypeProvider.GetNamespaces());

    public ParameterInfo[] GetStaticParameters(Type typeWithoutArguments)
    {
      return myTypeProvider.GetStaticParameters(typeWithoutArguments);
    }

    public Type ApplyStaticArguments(Type typeWithoutArguments, string[] typePathWithArguments,
      object[] staticArguments)
    {
      return myTypeProvider.ApplyStaticArguments(typeWithoutArguments, typePathWithArguments, staticArguments);
    }

    public FSharpExpr GetInvokerExpression(MethodBase syntheticMethodBase, FSharpExpr[] parameters)
    {
      return myTypeProvider.GetInvokerExpression(syntheticMethodBase, parameters);
    }

    public byte[] GetGeneratedAssemblyContents(Assembly assembly)
    {
      return myTypeProvider.GetGeneratedAssemblyContents(assembly);
    }

    private void TypeProviderOnInvalidate(object sender, EventArgs e)
    {
      ClearCaches();
      Invalidate?.Invoke(sender, e);
    }

    private void ClearCaches()
    {
      myProvidedNamespaces = null;
    }

    public event EventHandler Invalidate;
  }
}
