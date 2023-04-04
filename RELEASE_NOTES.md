# Release notes

### 2023.1

### Code completion

* **New**: When starting a `match` expression, the new 'Match values' suggestion generates all cases for union, enum, bool and tuple values
* **New**: postfix templates:
  * `match`: rewrites the expressions and adds boilerplate needed to start a `match` expression
  * `for`: makes it easier to iterate over a sequence, suggests name for the loop variable, and suggests deconstrucing it
  * `with`: simplifies updating possibly nested record values, with initial implementation by [@ieviev](https://github.com/ieviev) ([#436](https://github.com/JetBrains/resharper-fsharp/pull/436))
* **New**: automatically insert ` = ` when completing record fields
* **New**: show completion popup automatically when writing subsequent record fields and starting a new match branch

### Code analysis

* **New**: Automatically inject Regex language inside string literals, with initial implementation by [@saul](https://github.com/saul) ([#134](https://github.com/JetBrains/resharper-fsharp/pull/134))
* **New**: support `WarningsNotAsWarnings` property
* **Fix**: Abstract properties with setters could be seen incorrectly by some features
* **Fix**: lambda analyzer would suggest simplifying invocations of methods with optional parameters, and active patterns
* **Fix**: some `xint` literals weren't highlighted properly, by [@En3Tho](https://github.com/En3Tho) ([#474](https://github.com/JetBrains/resharper-fsharp/commit/01ba40511c876744790c6dd63fb58ae73bd207f3))
* **Fix**: 'Parameter info' now shows correct signature for delegates
* **Fix**: 'Parameter info' could use wrong parameter documentation on extension methods
* **Fix**: `base` wouldn't be highlighted in some cases
* **Fix**: syntax highlighting didn't work for F# files included as content
* **Fix**: escaping of reserved keywords was considered redundant
* **Fix**: using `open type` could break 'Import type' completion and quick fix
* **Fix**: redundant parens analyzer now takes dymamic invocations and more indexer-like expressions into account
* **Fix**: better generics analysis is now used in redundant qualifier analyzer
* **Fix**: an empty tooltip could be shown when hovering punctuation symbols
* **Fix**: tooltips could be unavailable on multi-tarteting projects
* **Fix**: syntax highlighting could be broken after editing an unfinished escaped name
* **Improve**: redundant attribute analisys is updated for upcoming F# 8

### Quick fixes

* **New**: **FS0025**: new quick fix for generating missing branches in `match` expressions
* **New**: **FS0008**: a new quick fix annotates a parameter value with the base type when type checking it inside the function
* **New**: **FS0725**, **FS3191**: remove unexpected argument patterns, by [@edgarfgp](https://github.com/edgarfgp) ([#444](https://github.com/JetBrains/resharper-fsharp/pull/444))
* **New**: **FS0810**: add a setter to a property
* **Improve**: **FS0025**: Add missing `|` when generating `_` branch in a `match` expression
* **Improve**: **FS0001**: suggest fixing single tuple list to list of items (e.g. `[1,2,3]` to `[1;2;3]`) in more cases
* **Improve**: better check if parens are needed when simplifying a lambda expression
* **Fix**: **FS0365**, **FS0366**: fix broken overrides generation on empty type declaration bodies

### Misc

* **New**: deconstruction of `KeyValue` active pattern is suggested in 'Introduce Variable', `for` postfix template, and 'Deconstruct' context action
* **Improve**: keep the cursor on the correct place when reformatting code, thanks to the new Fantomas Cursor API
* **New**: auto-detect available Fantomas settings and their defaults
* **Improve**: use Server GC in Fantomas process
* **Fix**: 'Inline variable' refactoring worked incorrectly when inlining a named literal into a pattern
* **Fix**: 'Extend selection' feature wouldn't select closing paren in union case patterns, by [@nojaf](https://github.com/nojaf) ([#496](https://github.com/JetBrains/resharper-fsharp/pull/496))

## 2022.3

### Type providers

* **New**: Provided types from generative type providers are properly seen in C# code now
* **Fix**: Excetion or invalidation inside type provider could break analysis
* **Fix**: Host type providers in matching runtime when F# compiler is overridden in a project

### Code completion

* **New**: Typing `<` inside a comment will generate a xml documentation template
* **New**: Additional rule for record fields emphasizes fields from the inferred record type, hides already used fields, and fixes various cases where no suggestions would be shown
* **Fix**: Rule for union cases could add an unneeded union name for types with `RequireQualifiedAccess`
* **New**: Parameter info popup is available in patterns

### Code analysis

* **New**: A new analyzer for xml documentation highlights syntax and shows additional warnings
* **Fix**: Lambda analyzer suggested changes that would change meaning of the code
* **Fix**: Redundant parens analyzer suggested removing required parens
* **Fix**: Usages of some custom operators, indexers, and range expressions had incorrect highlighting range

### Quick fixes

* **New**: A new quick fix for updating a parameter name in a signature file, by [@nojaf](https://github.com/nojaf) ([#416](https://github.com/JetBrains/resharper-fsharp/pull/416))
* **New**: A new quick fix for updating a record field in a signature file, by [@nojaf](https://github.com/nojaf) ([#418](https://github.com/JetBrains/resharper-fsharp/pull/418))
* **Fix**: Generating missing record fields could break indentation
* **Fix**: Adding a match-all clause for enums didn't check if `ArgumentOutOfRangeException` is in scope
* **Fix**: When adding `|> ignore`, extra parens would be added in various cases
* **Improve**: Replace binding type quick fix is available in additional cases

### Typing assists

* **New**: When pressing Enter inside single-line lambda, the lambda will be reformatted according to `fsharp_multi_line_lambda_closing_newline` Fantomas setting
* **Fix**: When pressing Enter, an extra indentation would be added after `=` in some cases
* **New**: Support 'Start new line before' action

### F# 7 support

* FSharp.Compiler.Service is updated with F# 7 support
* Support for F# 7 features, like abstract static members, in C# interop
* **New**: When using F# 7, Rename refactoring allows using lowercase names for union cases defined in a type with `[<RequireQualifierAccess>]` attribute
* **Improve**: support reading compressed F# metadata produced by newer compilers

### Misc

* Update formatting settings to match newer Fantomas defaults
* **Fix**: Deconstruct pattern action wouldn't add required parens in some cases
* **Fix**: Find usages: references to a constructor could be found when it's used as a method group
* **Fix**: Find usage: usages of `let` values defined inside types could be shown as 'Write' access
* **Fix**: Find usages and Rename: types with same name and different type parameters couldn't be distinguished in some cases

## 2022.2

### Parameter info

The Parameter Info popup was completely rewritten to become available inside curried applications. In addition to that, the following was implemented:

* Highlight the currently resolved overload
* Show method/function and parameter descriptions from xml docs
* Show parameter default values
* Show info about extension methods
* Show CanBeNull/NotNull attributes

### Fantomas

* Automatically restore and use Fantomas version specified with `dotnet tool`

### Analysis

* **Fix**: Significantly improved analysis speed for F# scripts
* **Fix**: External file changes could be ignored by analysis, e.g. when switching branches in version control system or generating source and output files during build
* **Fix**: Types with measure type parameters were seen incorrectly in navigation and C# analysis
* **Fix**: `WarnAsError` could produce errors for disabled warnings
* **Fix**: Redundant parens analysis fixes for tuple types, operator usages, and interpolated strings
* **Fix**: `fsi` directive errors weren't reported for scripts
* **Fix**: Type providers analysis inside F# scripts
* **New**: Analyze redundant `sprintf` invocations

### Generate overrides

* **New**: Generate setters and accessor parameters
* Use better checks for existing member overrides in base types
* Use better placement of generated overrides

### Quick fixes

* **New**: Replace return type, by [@nojaf](https://github.com/nojaf) ([#367](https://github.com/JetBrains/resharper-fsharp/pull/367))
* **New**: Replace interpolated strings with triple-quote ones, by [@seclerp](https://github.com/seclerp) ([#364](https://github.com/JetBrains/resharper-fsharp/pull/364))
* Move caret and select generated code when generating overrides or match branches
* **Fix**: Import Type: don't try to import own namespace
* **New**: Import Type: enable for F# types with separate compiled and source names

### Code completion

* **Fix**: Postfix template didn't work inside `#if` blocks
* **New**: Hide more keywords when they aren't available

### Misc

* **Fix**: Find Usages: write usages weren't marked as Write in the new indexer syntax and when used as named arguments in method returns
* **Fix**: Navigation might not work for types with same full names in different assemblies
* **Fix**: Rename refactoring: renaming file with type produced an error
* **Fix**: F# object expressions could be visible inside C# code completion
* Typing assists: better caret placement after pressing Enter inside binary expressions
* **Fix**: Folding of xml doc comments

## 2022.1

### Project model

* **Fix**: race that could lead to Rider freeze during project loading or update
* Faster loading of solutions that contain F# scripts
* **Fix**: script package references and file includes might produce errors
* **Fix**: changes to fsproj files might not be reflected in analysis
* Use unique project stamps for instant project cache lookup in FSharp.Compiler.Service which improves features performance
* **Fix**: stack overflow exception when loading very big F# project graphs

### C# interop
* **New**: Support F#-defined InternalsVisibleTo in C# analysis
* **Fix**: F#-defined generic constraints are now properly seen in C# code

### Refactorings
* Inline var: produce cleaner code by preventing adding redundant parens in more cases

### Type providers
* **Fix**: Multiple instantiations of the same type provider could lead to analysis errors
* **Fix**: Performance could be decreased due to eagerly analysing provided namespaces

### Misc
* Quick fixes - Generate missing record fields: quick fix uses improved fields order, by [@seclerp](https://github.com/seclerp) ([#330](https://github.com/JetBrains/resharper-fsharp/pull/330))
* **Fix**: Run gutter icons for running entry point methods weren't shown
* **Fix**: Typing assist: typing inside escaped identifiers might not work in some cases
* Various fixes for redundant parens analysis
* **Fix**: Navigation - Go to Everything: performance regression in calculating type presentations


## 2021.3

### Refactorings: introduce variable

* Suggest deconstruction of tuples and single-case union types
* Suggest using computation type in computation expressions
* Suggest using `use` and `use!` keywords when applicable
* Improved placement of added binding

### Code completion

* Better completion suggestions order
* Rewrite getting completion context (allows adding new completion rules and better suggestions filtering)
* **New**: 'To recursive function' rule, adding `rec` to containing function
* When completing union case pattern, deconstucting its fields is suggested
* Improved `Attribute` suffix cutting
* Initial context-based keyword filtering (some keywords no longer show up when aren't applicable)

### Find usages

* **New**: Icons in Find Usages results help to distinguish invocation, partial application, pattern, and other occurrence kinds


### Quick fixes

* **New**: Deconstruct union case fields
* Enable 'Replace with assignment' and 'Replace with `_`' in more cases

### Extend selection

* **New**: Extend selection inside interpolated strings (by [@seclerp](https://github.com/seclerp) ([#316](https://github.com/JetBrains/resharper-fsharp/pull/316)))
* Improved selection for `_` and various brackets kinds

### Misc

* Improved performance of redundant parens analyzer and pipe types highlighting
* Some unused R# analyzers are not run on F# code anymore for improved performance
* Improved `open` placement when importing type in quick fixes, refactorings and other actions
* **New**: Deconstruct pattern: support struct tuples
* **Fix**: `WarningsAsErrors` property doesn't change warning severity in editor
* **Fix**: Surround with braces adds extra `}` in string interpolations
* **Fix**: Navigation to interface implementations might not work for implementations in union and record types
* **Fix**: Navigation to active patterns in signature doesn't work
* **Fix**: Projects with generative type providers may not be invalidated and use stale results
* **Fix**: C# interop: property accessors may show incorrect visibility


## 2021.2

### Code completion
* Postfix templates: now available for 'let' template, and more templates coming in future

### Actions

* Deconstruct pattern (tuples, union cases)
* Rearrange code: move elements up/down or left/right: enum/union cases, record/union case fields, match clauses, tuples, function parameters
* Optimize Imports action is implemented for F#

### Refactorings

* Inline variable: now also works for top level let bindings
* Introduce variable: better filtering for expression suggestions

### Quick fixes

* **New**: replace operator with built-in operator
* **New**: add missing member self identifier
* **New**: replace object instance with type name for static member access
* **New**: replace `if` expression with its condition for simple expressions
* Introduce variable: enabled for protected members access inside lambda, moves binding outside
* Import type: fixes for importing module attributes
* Add parens: now works in more cases (type check expressions, union constructors, etc)
* Convert module to namespace: enabled for more error cases
* Specify type: annotate inferred property types, values accessed via accessors

### Editor

* Significantly better XML documentation rendering
* Better highlighting of various compiler errors

### Misc

* Language version can be specified in project properties (and is written to fsproj)
* Enable FCS optimizations for skipping implementation files when signature files are available
* Hide VCS lenses for simple fields
* Better extend selection for identifiers

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
