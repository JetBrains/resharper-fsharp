// ${COMPLETE_ITEM:StaticExtension (in Module.Extensions)}

module Module

open System.Threading.Tasks

module Extensions =
    type Task with
        member x.InstanceExtension() = ()
        static member StaticExtension() = ()


ignore ()
Task<int>.{caret}

// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"