module Module

open System.Collections.Generic

Class.StringExt("")
"".StringExt()

Class.ObjSeq([1])
[obj()].ObjSeq()
[1].ObjSeq()


Class.GenericSeqExt<int>([1])
[1].GenericSeqExt<int>()

Class.GenericSeqExt<string>([""])
[""].GenericSeqExt<string>()

[""].GenericSeqExt<int>()
[1].GenericSeqExt<string>()

Class.StringSeqExt([""])
[""].StringSeqExt()

Class.StringSeqExt([1])
[1].StringSeqExt()
