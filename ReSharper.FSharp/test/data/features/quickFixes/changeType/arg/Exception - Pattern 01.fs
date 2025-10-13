module Module

exception E of int

try () with E ""{caret} -> ()
