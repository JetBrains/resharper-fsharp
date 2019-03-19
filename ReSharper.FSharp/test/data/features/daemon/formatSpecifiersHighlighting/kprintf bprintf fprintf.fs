module Module

let _ = fprintf null "%A" 0
let _ = Microsoft.FSharp.Core.Printf.fprintf null "%A" 0
let _ = fprintfn null "%A" 0
let _ = Printf.kprintf (fun _ -> ()) "%A" 1
let _ = Printf.bprintf null "%A" 1
