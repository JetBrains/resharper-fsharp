// ${KIND:Overrides}
// ${SELECT0:ToString():System.String?}

// ${KIND:Overrides}

type MyClass() = class{caret}
  interface IA<int> with
    member x.Get() = 1

  // comment 1
  interface IA<string> with
    // comment 2
    member x.Get() = "hello"
end
