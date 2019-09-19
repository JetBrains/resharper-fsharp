module Module

exception Exn of int

try ()
with Exn n -> ()
