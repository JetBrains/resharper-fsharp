module Test

/// A signature
type A = struct end

/// A augment signature
type A with
    member M: unit -> unit
