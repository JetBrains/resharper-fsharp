module Module

let m = dict [123, ""]
for KeyValue{on} _ in m do ()
for KeyValue _ in m do ()
