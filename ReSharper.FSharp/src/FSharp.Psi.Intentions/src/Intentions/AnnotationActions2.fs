module JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions.AnnotationActions2

// This file will a be a base for new work

// Value is ether local or parameter
// Values can be: LocalRefs, Patterns, Arrays, Lists, Wilds

// ReferencePat is useful as it have it's own link to unique FSharpSymbol, others do not seem to

// Is there a common local value interface to look for?

// Functions are always bindings so always LetBinding
// Functions can be type functions, normal functions and values

// Members are always members so always MemberDeclaration
// Members can be properties and methods

// TODO's:
// 1. Decide how many different analyzers needs to be.
// Functions/Values/Members/Tuples - is there a need for specific Tuple analyzer?

// 2. Extract shared logic, cleanup

// 3. More tests