module Mod =
    let log<'a> (x : 'a) = sprintf "%O" x

{selstart}Mod.log "hello"{selend}
