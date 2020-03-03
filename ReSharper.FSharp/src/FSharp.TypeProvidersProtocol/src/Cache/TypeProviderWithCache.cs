using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.FSharp.Core.CompilerServices;
using Microsoft.FSharp.Quotations;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class TypeProviderWithCache : ITypeProvider
  {
    private readonly ITypeProvider myTypeProvider;
    private Lazy<IProvidedNamespace[]> myProvidedNamespaces;
    private Dictionary<string, byte[]> myGeneratedAssemblyContents;

    public TypeProviderWithCache(ITypeProvider typeProvider)
    {
      myTypeProvider = typeProvider;
      myTypeProvider.Invalidate += TypeProviderOnInvalidate;
      InitCaches();
    }

    public void Dispose()
    {
      myTypeProvider.Dispose();
    }

    public IProvidedNamespace[] GetNamespaces() => myProvidedNamespaces.Value;

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
      if (!myGeneratedAssemblyContents.TryGetValue(assembly.FullName, out var contents))
      {
        contents = myTypeProvider.GetGeneratedAssemblyContents(assembly);
        myGeneratedAssemblyContents.Add(assembly.FullName, contents);
      }

      return contents;
    }

    private void TypeProviderOnInvalidate(object sender, EventArgs e)
    {
      InitCaches();
      Invalidate?.Invoke(sender, e);
    }

    private void InitCaches()
    {
      myProvidedNamespaces = new Lazy<IProvidedNamespace[]>(() => myTypeProvider.GetNamespaces());
      myGeneratedAssemblyContents = new Dictionary<string, byte[]>();
    }

    public event EventHandler Invalidate;
  }
}
