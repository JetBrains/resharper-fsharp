module Module

let _ as (_, _) = 1, 2
let _ as ((_, _)) = 1, 2

let _ as ((_ :: _)) = []
let _ as (_ :: _) = []

let _ as (_ as _) = 1
let _ as ((_ as _)) = 1

let _ as (None | Some _) = None
let _ as ((None | Some _)) = None

let _ as ([]) = []
let _ as (([])) = []

let _ as (1) = 1
let _ as ((1)) = 1

let _ as (null) = null
let _ as ((null)) = null

let f (_ as ([<CompiledName "">] x)) = ()
let f (_ as (([<CompiledName "">] x))) = ()
