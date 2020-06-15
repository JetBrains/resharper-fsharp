open System

let inline CallInstanceProp<'T when 'T : (member Two : 'T)> (x : 'T) =
    (^T : (member Two : 'T) (x))
let inline CallInstanceMethod<'T when 'T : (member Two : unit -> 'T)> (x : 'T) =
    (^T : (member Two : unit -> 'T) (x))
let inline CallStaticProp<'T when 'T : (static member Two : 'T)> =
    (^T : (static member Two : 'T) ())
let inline CallStaticMethod<'T when 'T : (static member Two : unit -> 'T)> =
    (^T : (static member Two : unit -> 'T) ())
    
let inline arraysAndStaticMethod<'T when 'T : (static member Two : unit -> 'T)> =
    let arr = Array.zeroCreate<Type> 10
    for i = 0 to arr.Length do
        arr.[i] <- typeof<'T>
    let element = (^T : (static member Two : unit -> 'T) ())
    let arr2 = Enumerable.Repeat(element, 10) |> Seq.toArray
    arr |> Array.zip arr2
    
let inline arraysAndStaticProp<'T when 'T : (static member Two : 'T)> =
    (^T : (static member Two : 'T) ()) |> Seq.replicate 10
    |> Seq.toArray
    |> Array.zip ((Enumerable.Repeat(typedefof<'T>, 10)).ToArray())