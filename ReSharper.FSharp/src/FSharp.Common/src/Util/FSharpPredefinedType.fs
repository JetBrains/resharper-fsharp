[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Common.Util.FSharpPredefinedType

open JetBrains.Metadata.Reader.Impl

[<CompiledName("SourceNameAttrTypeName")>]
let sourceNameAttrTypeName = ClrTypeName("Microsoft.FSharp.Core.CompilationSourceNameAttribute")

[<CompiledName("CompilationMappingAttrTypeName")>]
let compilationMappingAttrTypeName = ClrTypeName("Microsoft.FSharp.Core.CompilationMappingAttribute")

[<CompiledName("FSharpListTypeName")>]
let fsListTypeName = ClrTypeName("Microsoft.FSharp.Collections.FSharpList`1")

[<CompiledName("FSharpOptionTypeName")>]
let fsOptionTypeName = ClrTypeName("Microsoft.FSharp.Collections.FSharpOption`1")

[<CompiledName("FSharpRefTypeName")>]
let fsRefTypeName = ClrTypeName("Microsoft.FSharp.Collections.FSharpRef`1")

[<CompiledName("FSharpResultTypeName")>]
let fsResultTypeName = ClrTypeName("Microsoft.FSharp.Core.FSharpResult`2")

[<CompiledName("FSharpAsyncTypeName")>]
let fsAsyncTypeName = ClrTypeName("Microsoft.FSharp.Control.FSharpAsync")

[<CompiledName("FSharpAsyncGenericTypeName")>]
let fsAsyncGenericTypeName = ClrTypeName("Microsoft.FSharp.Control.FSharpAsync`1")
