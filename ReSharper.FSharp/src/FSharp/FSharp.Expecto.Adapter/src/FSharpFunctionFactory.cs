using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace JetBrains.ReSharper.Plugins.FSharp.Expecto.Adapter
{
  internal static class FSharpFunctionFactory
  {
    public static object Create(Type functionType, Action<object[]> callback, object result)
    {
      var function = CreateFunctionExpression(functionType, new List<ParameterExpression>(), callback, result);
      return Expression.Lambda<Func<object>>(function).Compile().Invoke();
    }

    public static object FromConverter(Type functionType, Delegate converter) =>
      GetFromConverter(functionType).Invoke(null, new object[] { converter });

    private static Expression CreateFunctionExpression(Type functionType, List<ParameterExpression> args, Action<object[]> callback, object result)
    {
      var typeArgs = functionType.GetGenericArguments();
      var argType = typeArgs[0];
      var resultType = typeArgs[1];

      var arg = Expression.Parameter(argType);
      args.Add(arg);

      var body = IsFSharpFunc(resultType)
        ? CreateFunctionExpression(resultType, args, callback, result)
        : Expression.Block(
          Expression.Invoke(
            Expression.Constant(callback),
            Expression.NewArrayInit(typeof(object), args.Select(functionArg => Expression.Convert(functionArg, typeof(object))))),
          Expression.Constant(result, resultType));

      var converterType = typeof(Converter<,>).MakeGenericType(argType, resultType);
      var converter = Expression.Lambda(converterType, body, arg);
      return Expression.Call(GetFromConverter(functionType), converter);
    }

    private static MethodInfo GetFromConverter(Type functionType) =>
      functionType.GetMethod("FromConverter", BindingFlags.Static | BindingFlags.Public);

    private static bool IsFSharpFunc(Type type) =>
      type.IsGenericType && type.GetGenericTypeDefinition().FullName == "Microsoft.FSharp.Core.FSharpFunc`2";
  }
}
