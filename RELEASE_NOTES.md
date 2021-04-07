# Release notes

## 2021.1

### Refactorings

* **New**: Inline Variable refactoring for local let bindings
* Rename: when renaming a type that has an associated module with the same name, the module is also suggested to be renamed (and vice versa)
* Introduce Variable: redundant parens are removed for some replaced expressions

### Language version support

* **New**: Allowed language level is calculated based on a project and the compiler used in the build
    * Allows implementing analyzers and other features that use or suggest newer language features


### Analyzers:

* To Interpolated string suggestion + quick fix for F# 5.0+, by [@saul](https://github.com/saul) ([#221](https://github.com/JetBrains/fsharp-support/pull/221))
* Replace `__` with `_` analyzer + quick fix for F# 4.7+
* Redundant parens analyzer + quick fix for parens in patterns, types, and simple expressions
* Various improvements to other analyzers and quick fixes

### Quick fixes

* Specify parameter type when it's inferred below
* Use `;` list separator
* Replace type abbreviation with abbreviated type in type augmentation

### Type providers

* Host type providers out-of-process
    * Uses the same runtime as the build, fixes various issues previously occurred with the Rider runtime

### Misc

* Run icon for `main` methods
* Use .NET Core references for scripts by default
* Fixes in File Structure for interface/extension members
* Various fixes in C# interop
* Typing assists for braces in string interpolations

## 2020.1

### Inspections / analyzers

* Attributes 
    * **Add** Redundant Attribute suffix analyzer and quick fix (by [@reacheight](https://github.com/reacheight) ([#109](https://github.com/JetBrains/fsharp-support/pull/109)))
    * **Add** Redundant attribute parens analyzer and quick fix (by [@reacheight](https://github.com/reacheight) ([#104](https://github.com/JetBrains/fsharp-support/pull/104)))

* Run analyzers in parallel
* Optimize spell-checker analyzer
* **Fix** Spell-checker didn't work on some declarations
* Improve reported ranges for unused sequential and `let` expressions

### Quick fixes

* **FS0039**: Undefined name
  * Better types filtering in Import Type quick fix
  * Escape module names when importing types
  * **Fix** various cases of incorrect `open` placement
* **FS0003**: Not a function / unexpected argument
  * **Add** Remore unexpected arguments (by [@DedSec256](https://github.com/DedSec256) ([#89](https://github.com/JetBrains/fsharp-support/pull/89)))
* **FS0026**: `match` rule is never matched
  * Remove never matching rules (by [@reacheight](https://github.com/reacheight) ([#74](https://github.com/JetBrains/fsharp-support/pull/74)))
* **FS0005**: Field not mutable
  * **Add** Make field mutable
* **FS0027**: Value not mutable
  * **Add** Make value mutable
* **FS1182**: Unused let binding
  * **Add** Rename with `_` prefix
* **FS0038**: Var bound twice
  * **Add** Replace with `_`

* Add parens to expression when applying quick fixes where needed

### Intentions / Context actions

* **Add** Elif to If, If to Elif actions
* **Add** Negate `If` expression condition

### Code vision

* **Add** Copy Inferred Type action
* **Add** Don't show parent namespaces for types
* **Fix** Nested tuple params could be shown wrong

### C# interop

* **Fix** Symbols from C# projects could not be seen after first build
* **Fix** Support for single case unions that are parsed as type abbreviations
* **Fix** Invalid accessibility for some types containing non-public members
* **Fix** Attributes containing `Attribute` wasn't seen properly from C# (e.g. `AttributeUsage`)

### F# Interactive

* **Add** Send project references to F# Interactive action
* **Fix** Send to F# Interactive is now available during initial file indexing

### Debugger

* **Add** More expressions are supported in evaluation on mouse hover:
  * self-reference qualifiers in methods and types (`this.Property`)
  * indexer expressions (`"foo".[1]`)

### Find usages and Rename

* **Fix** Types would not be found inside generic constraints, constructor attributes, and measures
* **Fix** New Instance Creation wasn't reported for `new` expressions
* Better naming style suggestions for literals

### Highlighting

**Add** Highlight more escape sequences in strings
**Add** Settings for F# preprocessor keywords 
**Fix** Highlighting for `const` keyword

### Misc

* Add `Type` file template
* Select whole `()` expression in Extend Selection
* **Fix** Plugin could not be built on case-sensitive file systems (by [@ mcon](https://github.com/mcon) ([#108](https://github.com/JetBrains/fsharp-support/pull/108)))

## 2019.3

### Tools update and internals

* Update FSharp.Compiler.Service with F# 4.7 support, most notably:
  * `_` self identifier in member definitions
  * Implicit `yield` in computational expressions
  * F# 5.0 features preview when enabled with compiler option
* Update Fantomas to 3.0 with various fixes
* Rewritten parse tree nodes for expressions and types
  * Allows significantly simplified code for features like quick fixes, analyzers, and others
  * Better context tracking, optimized references creation, references are now qualifier-aware
  * Improved infrastructure for upcoming refactorings, postfix templates, and other features

### Quick fixes

* **FS0039**: Undefined name
  * **Add** Initial Import Type quick fix
  * **Add** Make outer function recursive fix
* **FS0020**: Expression is unused
  * **Add** Introduce 'let' binding fix for local expressions
  * **Update** Ignore expression fix
     * Better `|> ignore` placement for multiline expressions
     * Option to ignore inner expression in `match` and `if` expressions
  * **Add** Remove subsequent expressions fix
* **FS1182**: Unused let binding
  * **Add** Remove unused local binding
* **FS0597**: Successive arguments should be separated by spaces, tupled, or parenthesized
  * **Add** Surround with parens fix
* **FS0001**: Unit type expected
  * **Add** Ignore expression fix for `if` expressions without `else` branch
* **FS0066**: Unnecessary upcast
  * **Add** Remove upcast fix
* **FS0588**: Expected expression after let
  * **Add** Replace `let` with bound expression fix
* **FS0576**: `and` is not allowed for non-recursive let bindings
  * **Add** To recursive let bindings fix
* **FS0894**: Let bindings inside class cannot be inline
  * **Add** Remove `inline` fix

### Inspections / Problem analyzers

* Redundant `new` analyzer
  * **Add** Remove redundant `new` fix
* Redundant identifier escaping
  * **Add** Remove backticks fix
* **Add** Extension attribute usage analyzer:
  * Extension member inside non-extension type
  * Extension type doesn't define extension members
  * Extension type should be static

### Rename

* Rename additional symbols:
  * Suggest renaming of a single case union when renaming its case and vice versa
* Suggest camelCase names for let bindings
* **Add** renaming anonymous records fields
* **Fix** renaming union case fields when used as named args
* **Fix** renaming typed parameters inside lambda functions
* Better naming sugestions for predefined types

### Intentions / Context actions

* **Add** To namespace/module action
* **Add** To recursive module or namespace action

### Extend selection

* Rewritten from scratch for the parse tree changes
  * Words are selected first when invoked inside comments and strings
  * Better selection for many language constructs, with contributions by [@reacheight](https://github.com/reacheight)
  * **Fix** declaration selection could miss starting keywords and attributes

### Find usages

* Parts of results grouping is rewritten from scratch for the parse tree changes
  * **Fix** various cases could be reported incorrectly

### Typing assistance

* Significantly improved undo/redo changes workflow
* **Add** Complete pair escape identifier backticks assist
* **Add** Escape identifier with backticks assist
* **Add** Erase trailing semicolon in Enter assist when enabled in Code Style settings
* **Fix** Pair quotes assists no longer applied inside comments
* **Update** Better indentation in Enter assist

### Highlighting

* Separate highlighting options for F#:
  * **Add** F#-specific symbols like Unions or Active Patterns can now be highligted differently
* **Add** Highlight `byref` values as mutable
* **Fix** unary operators highlighting

### Misc

* **Add** option to specify language version for scripts and F# Interactive
* **Fix** Don't use auto-detected code style settings in Reformat Code action
* **Add** helper for Quick Definition
* **Fix** record private representations resolve in C#
* **Fix** adding new file to a project could fail
* **Update** adding `open` now adds empty line when needed, by [@saul](https://github.com/saul) ([#65](https://github.com/JetBrains/fsharp-support/pull/65))
* **Update** Ignore `Folders on top` option in F# projects
* **Fix** getting F# Interactive paths
* **Fix** resolve of extension members with specified compiled names
* **Update** Don't show inferred type lenses inside object expressions

## 2019.2

### Code Vision support

* Inferred types are shown for functions, values and members
* Version control author is shown for declarations

### Code analysis

* R# spell checker now works for F# symbol declarations, strings and comments
* Escape sequences are highlighted in strings
* Better highlighting of never matched rules in `match` expressions

### Refactorings

* Context-based rename suggestions

### Quick Fixes

* Generate missing record fields
* Ignore unused expression
* Remove unused self identifier in types
* Remove unused `as` pattern
* Replace unused pattern with `_`
* Replace `use` with `let` in modules and types
* Add missing `rec` in `let ... and` bindings


### Find Usages & navigation

* Find Usages and Go to Declaration now work for record construction and copy-and-update expressions
* New Instance occurrence kind is shown for F# exception creation expressions

### Misc

* Allow running F# Interactive from .NET Core SDK 2.2.300+

### Fixes

* Completion for names starting with `_` is fixed
* Fix resolve for same type defined in different projects or assemblies
* Fix resolve for members with same signature in a type and interface implementation in it
* Rename wouldn't work for some local values
* Lexing of attributes inside type application was fixed by [@misonijnik](https://github.com/misonijnik) ([#51](https://github.com/JetBrains/fsharp-support/pull/51))

## 2019.1

### Refactorings

* Rename
	* Cross-language rename for F#-defined symbols
	* Name suggestions based on symbol type

### Navigation and cross-language interop

* Go to next/previous/containing member actions now work for top level `let` bindings
* Global navigation and Go to File Member action now work for type-private `let` bindings
* F# defined delegates are now supported and are properly seen by code in other languages
* Intrinsic type extensions are now considered type parts with navigation working for every part
* Navigation to compiler generated members declarations from usages in other languages now navigates to origin members
* Navigation to symbols with escaped names now places caret inside escaped identifiers
* `outref` and `inref` types are now properly seen in other languages

### Find usages

* Usages inside `let` bindings are properly grouped by containing members
* Separate grouping for new occurrence kinds:
	* Type argument
	* Base type
	* Type checking
	* Type conversions
	* Module or namespace import
	* Write
	* Attribute reference
* Highlight Usages in File action now highlights Write occurrences differently to other occurrences
* Find usages of union cases now also searches for compiler generated union members

### Code editing and completion

* An option for out-of-scope completion to place `open` statements to the top level was added and enabled by default by [@saul](https://github.com/saul) ([#39](https://github.com/JetBrains/fsharp-support/pull/39))
* Out-of-scope completion can be disabled now
* New Surround with Parens typing assist

### Code analysis

* FSharp.Compiler.Service updated with anonymous records support
* Format specifiers are now highlighted, by [@vasily-kirichenko](https://github.com/vasily-kirichenko) ([#40](https://github.com/JetBrains/fsharp-support/pull/40))
* Inferred generic arguments are shown in tooltips, by [@vasily-kirichenko](https://github.com/vasily-kirichenko) ([#44](https://github.com/JetBrains/fsharp-support/pull/44))
* Full member names are shown in tooltips, by [@vasily-kirichenko](https://github.com/vasily-kirichenko) ([#45](https://github.com/JetBrains/fsharp-support/pull/45))
* Debugger now shows local symbols values next to their uses
* Unused local values analysis is now turned on for all projects and scripts by default
* Support `TreatWarningsAsErrors` MSBuild property

### Misc

* Notification about F# projects having wrong project type guid in solution file
* F# Interactive in FSharp.Compiler.Tools is now automatically found in non-SDK projects
* FSharp.Compiler.Interactive.Settings.dll is now bundled to the plugin (so `fsi` object is available in scripts)
* F# breadcrumbs provider was added
* Add hotkey hint to Send to F# interactive action
* Provide F# project Azure scope for Azure plugin ([#46](https://github.com/JetBrains/fsharp-support/pull/46))

### Fixes

* Pair paren was not inserted in attributes list
* Tab left action (Shift+Tab) didn't work
* Tests defined as `let` bindings in types were not found
* Incremental lexers were not reused, by [@misonijnik](https://github.com/misonijnik)
* Type hierarchy action icon was shown in wrong places
* Usages of operators using their compiled names were highlighted incorrectly
* Find usages didn't work for compiled active patterns and some union cases
* Local rename didn't work properly for symbols defined in Or patterns
* Better highlighting of active pattern declarations
* Extend Selection works better for patterns and `match` expressions
* Resolve and navigation didn't work for optional extension members having the same name in a single module
* Secondary constructors resolve was broken in other languages
* Some reported errors were duplicated
