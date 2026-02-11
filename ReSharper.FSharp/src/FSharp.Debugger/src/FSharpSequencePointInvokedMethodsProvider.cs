using System;
using System.Collections.Generic;
using System.Linq;
using Debugger.Common.ManagedSymbols;
using JetBrains.Debugger.CorApi.ComInterop;
using JetBrains.Metadata.Access;
using JetBrains.Metadata.Debug;
using JetBrains.Metadata.Debug.Pdb;
using JetBrains.Metadata.Debug.Pdb.DebugSubsection;
using JetBrains.Metadata.IL;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.Metadata.Utils.PE.Directories;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Win32;
using Mono.Debugging.Win32.Utils;

namespace JetBrains.ReSharper.Plugins.FSharp.Debugger;

[DebuggerSessionComponent(typeof(CorDebuggerType))]
public class FSharpSequencePointInvokedMethodsProvider(ILogger logger, CorDebuggerSession session)
  : CorSequencePointInvokedMethodsProviderBase
{
  public override int Priority => 100;

  public override bool IsApplicable(IManagedSymbolDocument symbolDocument) =>
    symbolDocument.LanguageId == Languages.FSharp;

  public override bool IgnoreHiddenSequencePoints => true;

  public override List<SmartStepIntoElement> GetSmartStepIntoElements(ICorDebugFunction function, uint startIp,
    uint endIp, IMetadataMethod metadataMethod, IMetadataAssemblyInternals metadata,
    TypeDecodeContext decodeContext, out bool shouldBreakSmartStepInto)
  {
    shouldBreakSmartStepInto = false;

    var module = function.GetModule();
    var moduleInfo = session.AppDomainsManager.GetOrCreateModuleInfo(module);

    var debugData = PdbReader.ReadPdb(moduleInfo.AssemblyPath.ChangeExtension(ExtensionConstants.Pdb),
      DebugInfoType.Portable);
    var nameProvider = debugData != null ? new LocalVariablesNameProvider(debugData) : null;

    var evalStack = new Stack<string>();
    var elements = new List<SmartStepIntoElement>();

    foreach (var instruction in function.EnumerateILInstructions(startIp, endIp))
    {
      switch (instruction.Code.Value)
      {
        case OpcodeValue.Call:
        case OpcodeValue.Callvirt:
        case OpcodeValue.Newobj:
          try
          {
            var methodSpec = metadata.GetMethodFromToken((MetadataToken)instruction.Operand, decodeContext);
            if (!CorSmartStepIntoProvider.TryCreateElement(methodSpec, instruction, logger, DebuggerHiddenProviders,
                  out var element))
              return elements;

            if (element == null)
              continue;

            elements.Add(element);

            var method = methodSpec.Method;
            if (method.IsFSharpTypeFuncSpecialize())
            {
              element.IsHidden = true;
              continue;
            }

            if (IsFSharpFuncInvoke(method))
            {
              var parametersCount = method.Parameters.Length;
              var parametersToPopCount = method.IsStatic ? parametersCount - 1 : parametersCount;
              for (var i = 0; i < parametersToPopCount; i++)
                evalStack.Pop();

              if (evalStack.Pop() is { } name)
                element.SourceName = name;
            }
            else
            {
              for (var i = 0; i < method.Parameters.Length; i++)
                evalStack.Pop();
            }

            if (!method.ReturnValue.Type.IsVoid() || method.IsConstructor())
              evalStack.Push(null);
          }
          catch (Exception e)
          {
            logger.Error(e);
          }

          break;
        case OpcodeValue.Calli:
          break;

        case OpcodeValue.Ldarg:
        case OpcodeValue.Ldarg_0:
        case OpcodeValue.Ldarg_1:
        case OpcodeValue.Ldarg_2:
        case OpcodeValue.Ldarg_3:
        case OpcodeValue.Ldarg_s:
          var variableIndex = instruction.GetVariableIndexOperand();
          var index = metadataMethod.IsStatic ? variableIndex : variableIndex - 1;
          var paramName = metadataMethod.Parameters.ElementAtOrDefault(index)?.Name;
          evalStack.Push(paramName);
          break;

        case OpcodeValue.Ldloc:
        case OpcodeValue.Ldloc_0:
        case OpcodeValue.Ldloc_1:
        case OpcodeValue.Ldloc_2:
        case OpcodeValue.Ldloc_3:
        case OpcodeValue.Ldloc_s:
          var varIndex = instruction.GetVariableIndexOperand();
          var variableName = nameProvider?.GetVariableName(metadataMethod, varIndex);
          evalStack.Push(variableName);
          break;

        case OpcodeValue.Ldfld:
        case OpcodeValue.Ldsfld:
          var fieldSpec = metadata.GetFieldFromToken(instruction.OperandToken, decodeContext);
          evalStack.Push(fieldSpec.Field.Name);
          break;

        case OpcodeValue.Add:
        case OpcodeValue.Add_ovf:
        case OpcodeValue.Add_ovf_un:
        case OpcodeValue.And:
        case OpcodeValue.Brfalse:
        case OpcodeValue.Brfalse_s:
        case OpcodeValue.Brtrue:
        case OpcodeValue.Brtrue_s:
        case OpcodeValue.Ceq:
        case OpcodeValue.Cgt:
        case OpcodeValue.Cgt_un:
        case OpcodeValue.Clt:
        case OpcodeValue.Clt_un:
        case OpcodeValue.Div:
        case OpcodeValue.Div_un:
        case OpcodeValue.Endfilter:
        case OpcodeValue.Initobj:
        case OpcodeValue.Ldelem:
        case OpcodeValue.Ldelem_i:
        case OpcodeValue.Ldelem_i1:
        case OpcodeValue.Ldelem_i2:
        case OpcodeValue.Ldelem_i4:
        case OpcodeValue.Ldelem_i8:
        case OpcodeValue.Ldelem_u1:
        case OpcodeValue.Ldelem_u2:
        case OpcodeValue.Ldelem_u4:
        case OpcodeValue.Ldelem_r4:
        case OpcodeValue.Ldelem_r8:
        case OpcodeValue.Ldelem_ref:
        case OpcodeValue.Ldelema:
        case OpcodeValue.Mul:
        case OpcodeValue.Mul_ovf:
        case OpcodeValue.Mul_ovf_un:
        case OpcodeValue.Or:
        case OpcodeValue.Pop:
        case OpcodeValue.Rem:
        case OpcodeValue.Rem_un:
        case OpcodeValue.Shl:
        case OpcodeValue.Shr:
        case OpcodeValue.Shr_un:
        case OpcodeValue.Starg:
        case OpcodeValue.Starg_s:
        case OpcodeValue.Stloc:
        case OpcodeValue.Stloc_0:
        case OpcodeValue.Stloc_1:
        case OpcodeValue.Stloc_2:
        case OpcodeValue.Stloc_3:
        case OpcodeValue.Stloc_s:
        case OpcodeValue.Sub:
        case OpcodeValue.Sub_ovf:
        case OpcodeValue.Sub_ovf_un:
        case OpcodeValue.Switch:
        case OpcodeValue.Throw:
        case OpcodeValue.Xor:
          evalStack.Pop();
          break;

        case OpcodeValue.Beq:
        case OpcodeValue.Beq_s:
        case OpcodeValue.Bge:
        case OpcodeValue.Bge_s:
        case OpcodeValue.Bge_un_s:
        case OpcodeValue.Bge_un:
        case OpcodeValue.Bgt:
        case OpcodeValue.Bgt_s:
        case OpcodeValue.Bgt_un:
        case OpcodeValue.Bgt_un_s:
        case OpcodeValue.Ble:
        case OpcodeValue.Ble_s:
        case OpcodeValue.Ble_un:
        case OpcodeValue.Ble_un_s:
        case OpcodeValue.Blt:
        case OpcodeValue.Blt_s:
        case OpcodeValue.Blt_un:
        case OpcodeValue.Blt_un_s:
        case OpcodeValue.Bne_un:
        case OpcodeValue.Bne_un_s:
        case OpcodeValue.Cpobj:
        case OpcodeValue.Stind_i1:
        case OpcodeValue.Stind_i2:
        case OpcodeValue.Stind_i4:
        case OpcodeValue.Stind_i8:
        case OpcodeValue.Stind_r4:
        case OpcodeValue.Stind_r8:
        case OpcodeValue.Stind_i:
        case OpcodeValue.Stind_ref:
        case OpcodeValue.Stfld:
        case OpcodeValue.Stobj:
          evalStack.Pop();
          evalStack.Pop();
          break;

        case OpcodeValue.Stelem:
        case OpcodeValue.Stelem_i:
        case OpcodeValue.Stelem_i1:
        case OpcodeValue.Stelem_i2:
        case OpcodeValue.Stelem_i4:
        case OpcodeValue.Stelem_i8:
        case OpcodeValue.Stelem_r4:
        case OpcodeValue.Stelem_r8:
        case OpcodeValue.Stelem_ref:
        case OpcodeValue.Cpblk:
        case OpcodeValue.Initblk:
          evalStack.Pop();
          evalStack.Pop();
          evalStack.Pop();
          break;

        case OpcodeValue.Arglist:
        case OpcodeValue.Dup:
        case OpcodeValue.Ldarga:
        case OpcodeValue.Ldarga_s:
        case OpcodeValue.Ldc_i4:
        case OpcodeValue.Ldc_i4_0:
        case OpcodeValue.Ldc_i4_1:
        case OpcodeValue.Ldc_i4_2:
        case OpcodeValue.Ldc_i4_3:
        case OpcodeValue.Ldc_i4_4:
        case OpcodeValue.Ldc_i4_5:
        case OpcodeValue.Ldc_i4_6:
        case OpcodeValue.Ldc_i4_7:
        case OpcodeValue.Ldc_i4_8:
        case OpcodeValue.Ldc_i4_m1:
        case OpcodeValue.Ldc_i4_s:
        case OpcodeValue.Ldc_i8:
        case OpcodeValue.Ldc_r4:
        case OpcodeValue.Ldc_r8:
        case OpcodeValue.Ldftn:
        case OpcodeValue.Ldloca:
        case OpcodeValue.Ldloca_s:
        case OpcodeValue.Ldnull:
        case OpcodeValue.Ldflda:
        case OpcodeValue.Ldsflda:
        case OpcodeValue.Ldstr:
        case OpcodeValue.Ldtoken:
        case OpcodeValue.Sizeof:
          evalStack.Push(null);
          break;
      }
    }

    return elements;
  }

  private static bool IsFSharpFuncInvoke(IMetadataMethod method)
  {
    if (method?.Name is not ("Invoke" or "InvokeFast"))
      return false;

    var declaringType = method.DeclaringType;
    if (declaringType.Assembly is not { AssemblyName.Name: "FSharp.Core" }) return false;

    return declaringType.TypeParametersCountStrippedShortName() == "FSharpFunc";
  }
}
