﻿module Fable.Transforms.DartPrinter

open System.Text.RegularExpressions
open Fable
open Fable.AST
open Fable.AST.Dart
open Fable.Transforms.Printer

type ListPos =
    | IsFirst
    | IsMiddle
    | IsLast
    | IsSingle

module PrinterExtensions =
    type Printer with
        member this.AddError(msg, ?range) =
            this.AddLog(msg, Severity.Error, ?range=range)

        member this.AddWarning(msg, ?range) =
            this.AddLog(msg, Severity.Warning , ?range=range)

        member printer.PrintBlock(nodes: 'a list, printNode: Printer -> 'a -> unit, ?printSeparator: Printer -> unit, ?skipNewLineAtEnd) =
            let printSeparator = defaultArg printSeparator (fun _ -> ())
            let skipNewLineAtEnd = defaultArg skipNewLineAtEnd false
            printer.Print("{")
            printer.PrintNewLine()
            printer.PushIndentation()
            for node in nodes do
                printNode printer node
                printSeparator printer
            printer.PopIndentation()
            printer.Print("}")
            if not skipNewLineAtEnd then
                printer.PrintNewLine()

        member printer.PrintBlock(nodes: Statement list, ?skipNewLineAtEnd) =
            printer.PrintBlock(nodes,
                               (fun p s -> p.PrintProductiveStatement(s)),
                               (fun p -> p.PrintStatementSeparator()),
                               ?skipNewLineAtEnd=skipNewLineAtEnd)

        member printer.PrintStatementSeparator() =
            if printer.Column > 0 then
                printer.Print(";")
                printer.PrintNewLine()

        member this.HasSideEffects(e: Expression) = // TODO
            match e with
            | _ -> true

        member this.IsProductiveStatement(s: Statement) =
            match s with
            | ExpressionStatement(expr) -> this.HasSideEffects(expr)
            | _ -> true

        member printer.PrintProductiveStatement(s: Statement, ?printSeparator) =
            if printer.IsProductiveStatement(s) then
                printer.Print(s)
                printSeparator |> Option.iter (fun f -> f printer)

        // TODO: Most of this code matches BabelPrinter.PrintEmitExpression, can we refactor it?
        member printer.PrintEmitExpression(value: string, args: Expression list) =
            let inline replace pattern (f: Match -> string) input =
                Regex.Replace(input, pattern, f)

            let printSegment (printer: Printer) (value: string) segmentStart segmentEnd =
                let segmentLength = segmentEnd - segmentStart
                if segmentLength > 0 then
                    let segment = value.Substring(segmentStart, segmentLength)

                    let subSegments = Regex.Split(segment, @"\r?\n")
                    for i = 1 to subSegments.Length do
                        let subSegment =
                            // Remove whitespace in front of new lines,
                            // indent will be automatically applied
                            if printer.Column = 0 then subSegments.[i - 1].TrimStart()
                            else subSegments.[i - 1]
                        if subSegment.Length > 0 then
                            printer.Print(subSegment)
                            if i < subSegments.Length then
                                printer.PrintNewLine()

            // Macro transformations
            // https://fable.io/docs/communicate/js-from-fable.html#Emit-when-F-is-not-enough
            let value =
                value
                |> replace @"\$(\d+)\.\.\." (fun m ->
                    let rep = ResizeArray()
                    let i = int m.Groups.[1].Value
                    for j = i to args.Length - 1 do
                        rep.Add("$" + string j)
                    String.concat ", " rep)

                |> replace @"\{\{\s*\$(\d+)\s*\?(.*?)\:(.*?)\}\}" (fun m ->
                    let i = int m.Groups.[1].Value
                    match args.[i] with
                    | Literal(BooleanLiteral(value=value)) when value -> m.Groups.[2].Value
                    | _ -> m.Groups.[3].Value)

                |> replace @"\{\{([^\}]*\$(\d+).*?)\}\}" (fun m ->
                    let i = int m.Groups.[2].Value
                    match List.tryItem i args with
                    | Some _ -> m.Groups.[1].Value
                    | None -> "")

                // If placeholder is followed by !, emit string literals as native code: "let $0! = $1"
                |> replace @"\$(\d+)!" (fun m ->
                    let i = int m.Groups.[1].Value
                    match List.tryItem i args with
                    | Some(Literal(StringLiteral value)) -> value
                    | _ -> "")

            let matches = Regex.Matches(value, @"\$\d+")
            if matches.Count > 0 then
                for i = 0 to matches.Count - 1 do
                    let m = matches.[i]
                    let isSurroundedWithParens =
                        m.Index > 0
                        && m.Index + m.Length < value.Length
                        && value.[m.Index - 1] = '('
                        && value.[m.Index + m.Length] = ')'

                    let segmentStart =
                        if i > 0 then matches.[i-1].Index + matches.[i-1].Length
                        else 0

                    printSegment printer value segmentStart m.Index

                    let argIndex = int m.Value.[1..]
                    match List.tryItem argIndex args with
                    | Some e when isSurroundedWithParens -> printer.Print(e)
                    | Some e -> printer.PrintWithParensIfComplex(e)
                    | None -> ()

                let lastMatch = matches.[matches.Count - 1]
                printSegment printer value (lastMatch.Index + lastMatch.Length) value.Length
            else
                printSegment printer value 0 value.Length

        member printer.PrintList(left: string, right: string, items: 'a list, printItemAndSeparator: ListPos -> 'a -> unit, ?skipIfEmpty) =
            let skipIfEmpty = defaultArg skipIfEmpty false
            let rec printList isFirst = function
                | [] -> ()
                | [item] ->
                    let pos = if isFirst then IsSingle else IsLast
                    printItemAndSeparator pos item
                | item::items ->
                    let pos = if isFirst then IsFirst else IsMiddle
                    printItemAndSeparator pos item
                    printList false items
            match skipIfEmpty, items with
            | true, [] -> ()
            | _, items ->
                printer.Print(left)
                printList true items
                printer.Print(right)

        member printer.PrintList(left: string, separator: string, right: string, items: 'a list, printItem: 'a -> unit, ?skipIfEmpty) =
            let printItem pos item =
                printItem item
                match pos with
                | IsSingle | IsLast -> ()
                | IsFirst | IsMiddle -> printer.Print(separator)
            printer.PrintList(left, right, items, printItem, ?skipIfEmpty=skipIfEmpty)

        member printer.PrintList(left, idents: Ident list, right, ?printType: bool) =
            printer.PrintList(left, ", ", right, idents, fun x ->
                printer.PrintIdent(x, ?printType=printType)
            )

        member printer.PrintList(left, items: string list, right, ?skipIfEmpty) =
            printer.PrintList(left, ", ", right, items, (fun (x: string) -> printer.Print(x)), ?skipIfEmpty=skipIfEmpty)

        member printer.PrintList(left, items: Expression list, right) =
            printer.PrintList(left, ", ", right, items, fun (x: Expression) -> printer.Print(x))

        member printer.PrintCallArgAndSeparator (pos: ListPos) ((name, expr): CallArg) =
            let isNamed =
                match name with
                | None -> false
                | Some name ->
                    match pos with
                    | IsFirst | IsSingle ->
                        printer.PrintNewLine()
                        printer.PushIndentation()
                    | IsMiddle | IsLast -> ()
                    printer.Print(name + ": ")
                    true
            printer.Print(expr)
            match pos with
            | IsSingle | IsLast ->
                if isNamed then
                    printer.PrintNewLine()
                    printer.PopIndentation()
                else ()
            | IsFirst | IsMiddle ->
                if isNamed then
                    printer.Print(",")
                    printer.PrintNewLine()
                else
                    printer.Print(", ")

        member printer.PrintType(t: Type) =
            match t with
            | Void -> printer.Print("void")
            | MetaType -> printer.Print("Type")
            | Boolean -> printer.Print("bool")
            | String -> printer.Print("String")
            | Integer -> printer.Print("int")
            | Double -> printer.Print("double")
            | Object -> printer.Print("Object")
            | Dynamic -> printer.Print("dynamic")
            | List t ->
                printer.Print("List<")
                printer.PrintType(t)
                printer.Print(">")
            | Nullable t ->
                printer.PrintType(t)
                printer.Print("?")
            | Generic name ->
                printer.Print(name)
            | TypeReference(ref, gen) ->
                printer.PrintIdent(ref)
                printer.PrintList("<", ", ", ">", gen, printer.PrintType, skipIfEmpty=true)
            | Function(argTypes, returnType) ->
                match returnType with
                | Void -> ()
                | returnType ->
                    printer.PrintType(returnType)
                    printer.Print(" ")
                // Probably this won't work if we have multiple args
                let argTypes = argTypes |> List.filter (function Void -> false | _ -> true)
                printer.PrintList("Function(", ", ", ")", argTypes, printer.PrintType)

        member printer.PrintWithParens(expr: Expression) =
            printer.Print("(")
            printer.Print(expr)
            printer.Print(")")

        member printer.PrintWithParensIfNotIdent(expr: Expression) =
            match expr with
            | IdentExpression _
            | PropertyAccess _ -> printer.Print(expr)
            | _ -> printer.PrintWithParens(expr)

        member printer.PrintWithParensIfComplex(expr: Expression) =
            match expr with
            | ThisExpression
            | SuperExpression
            | Literal _
            | TypeLiteral _
            | IdentExpression _
            | PropertyAccess _
            | IndexExpression _
            | AsExpression _
            | IsExpression _
            | InvocationExpression _
            | SequenceExpression _
            | UpdateExpression _
            | UnaryExpression _
            | BinaryExpression _
            | LogicalExpression _
            | ThrowExpression _
            | RethrowExpression _
                -> printer.Print(expr)

            | ConditionalExpression _
            | AnonymousFunction _
            | AssignmentExpression _
            | EmitExpression _
                -> printer.PrintWithParens(expr)

        member printer.PrintBinaryExpression(operator: BinaryOperator, left: Expression, right: Expression, isInt) =
            printer.PrintWithParensIfComplex(left)
            // TODO: review
            match operator with
            | BinaryEqual -> printer.Print(" == ")
            | BinaryUnequal -> printer.Print(" != ")
            | BinaryLess -> printer.Print(" < ")
            | BinaryLessOrEqual -> printer.Print(" <= ")
            | BinaryGreater -> printer.Print(" > ")
            | BinaryGreaterOrEqual -> printer.Print(" >= ")
            | BinaryShiftLeft -> printer.Print(" << ")
            | BinaryShiftRightSignPropagating -> printer.Print(" >> ")
            | BinaryShiftRightZeroFill -> printer.Print(" >>> ")
            | BinaryMinus -> printer.Print(" - ")
            | BinaryPlus -> printer.Print(" + ")
            | BinaryMultiply -> printer.Print(" * ")
            | BinaryDivide -> printer.Print(if isInt then " ~/ " else " / ")
            | BinaryModulus -> printer.Print(" % ")
            | BinaryExponent -> printer.Print(" ** ")
            | BinaryOrBitwise -> printer.Print(" | ")
            | BinaryXorBitwise -> printer.Print(" ^ ")
            | BinaryAndBitwise -> printer.Print(" & ")
            printer.PrintWithParensIfComplex(right)

        member printer.PrintLogicalExpression(operator: LogicalOperator, left: Expression, right: Expression) =
            printer.PrintWithParensIfComplex(left)
            match operator with
            | LogicalAnd -> printer.Print(" && ")
            | LogicalOr -> printer.Print(" || ")
            printer.PrintWithParensIfComplex(right)

        member printer.PrintLiteral(kind: Literal) =
            match kind with
            | NullLiteral -> printer.Print(null)
            | ListLiteral(values, isConst) ->
                if isConst then
                    printer.Print("const ")
                printer.PrintList("[", values, "]")
            | BooleanLiteral v -> printer.Print(if v then "true" else "false")
            | StringLiteral value ->
                printer.Print("'")
                printer.Print(printer.EscapeStringLiteral(value))
                printer.Print("'")
            | IntegerLiteral value ->
                printer.Print(value.ToString())
            | DoubleLiteral value ->
                let value =
                    match value.ToString(System.Globalization.CultureInfo.InvariantCulture) with
                    | "∞" -> "double.infinity"
                    | "-∞" -> "-double.infinity"
                    | value -> value
                printer.Print(value)

        member printer.PrintIdent(ident: Ident, ?printType) =
            let printType = defaultArg printType false
            if printType then
                printer.PrintType(ident.Type)
                printer.Print(" ")
            match ident.Prefix with
            | None -> ()
            | Some p -> printer.Print(p + ".")
            printer.Print(ident.Name)

        member printer.PrintIfStatment(test: Expression, consequent, alternate) =
            printer.Print("if (")
            printer.Print(test)
            printer.Print(") ")
            printer.PrintBlock(consequent, skipNewLineAtEnd=true)
            match alternate with
            | [] -> ()
            | alternate ->
                match alternate with
                | [IfStatement(test, consequent, alternate)] ->
                    printer.Print(" else ")
                    printer.PrintIfStatment(test, consequent, alternate)
                | alternate ->
                    // Get productive statements and skip `else` if they're empty
                    alternate
                    |> List.filter printer.IsProductiveStatement
                    |> function
                        | [] -> ()
                        | statements ->
                            printer.Print(" else ")
                            printer.PrintBlock(statements)
            if printer.Column > 0 then
                printer.PrintNewLine()

        member printer.Print(statement: Statement) =
            match statement with
            | IfStatement(test, consequent, alternate) ->
                printer.PrintIfStatment(test, consequent, alternate)

            | ForStatement(init, test, update, body) ->
                printer.Print("for (")
                match init with
                | None -> ()
                | Some(ident, value) ->
                    printer.Print("final " + ident.Name + " = ")
                    printer.Print(value)
                printer.Print("; ")
                match test with
                | None -> ()
                | Some test -> printer.Print(test)
                printer.Print("; ")
                match update with
                | None -> ()
                | Some update -> printer.Print(update)
                printer.Print(") ")
                printer.PrintBlock(body)

            | ForInStatement(param, iterable, body) ->
                printer.Print("for (final " + param.Name + " in ")
                printer.PrintWithParensIfComplex(iterable)
                printer.Print(") ")
                printer.PrintBlock(body)

            | WhileStatement(test, body) ->
                printer.Print("while (")
                printer.Print(test)
                printer.Print(") ")
                printer.PrintBlock(body)

            | TryStatement(body, handlers, finalizer) ->
                printer.Print("try ")
                printer.PrintBlock(body, skipNewLineAtEnd=true)
                for handler in handlers do
                    match handler.Test with
                    | None -> ()
                    | Some test ->
                        printer.Print(" on ")
                        printer.PrintType(test)
                    match handler.Param with
                    | None -> ()
                    | Some param -> printer.Print(" catch (" + param.Name + ")")
                    printer.Print(" ")
                    printer.PrintBlock(handler.Body, skipNewLineAtEnd=true)
                match finalizer with
                | [] -> ()
                | finalizer ->
                    printer.Print(" finally ")
                    printer.PrintBlock(finalizer, skipNewLineAtEnd=true)
                printer.PrintNewLine()

            | ReturnStatement e ->
                printer.Print("return ")
                printer.Print(e)

            | BreakStatement(label, ignoreDeadCode) ->
                if ignoreDeadCode then
                    printer.Print("// ignore: dead_code")
                    printer.PrintNewLine()
                match label with
                | None -> printer.Print("break")
                | Some label -> printer.Print("break " + label)

            | ContinueStatement label ->
                match label with
                | None -> printer.Print("continue")
                | Some label -> printer.Print("continue " + label)

            | LabeledStatement(label, body) ->
                printer.Print(label + ":")
                printer.PrintNewLine()
                printer.Print(body)

            | LocalFunctionDeclaration f ->
                printer.PrintFunctionDeclaration(f.ReturnType, f.Name, f.GenericArgs, f.Args, f.Body)

            | ExpressionStatement e ->
                printer.Print(e)

            | LocalVariableDeclaration(ident, kind, value) ->
                printer.PrintVariableDeclaration(ident, kind, ?value=value)

            | SwitchStatement(discriminant, cases, defaultCase) ->
                printer.Print("switch (")
                printer.Print(discriminant)
                printer.Print(") ")

                let cases = [
                    yield! List.map Choice1Of2 cases
                    match defaultCase with
                    | Some c -> Choice2Of2 c
                    | None -> ()
                ]

                printer.PrintBlock(cases, fun p c ->
                    match c with
                    | Choice1Of2 c ->
                        for g in c.Guards do
                            p.Print("case ")
                            p.Print(g)
                            p.Print(":")
                            p.PrintNewLine()

                        p.PushIndentation()
                        for s in c.Body do
                            p.Print(s)
                            p.Print(";")
                            p.PrintNewLine()

                        match List.tryLast c.Body with
                        | Some(ContinueStatement _)
                        | Some(BreakStatement _)
                        | Some(ReturnStatement _) -> ()
                        | _ ->
                            p.Print("break;")
                            p.PrintNewLine()

                        p.PopIndentation()

                    | Choice2Of2 def ->
                        p.Print("default:")
                        p.PrintNewLine()
                        p.PushIndentation()
                        for s in def do
                            p.Print(s)
                            p.Print(";")
                            p.PrintNewLine()
                        p.PopIndentation()
                )

        member printer.Print(expr: Expression) =
            match expr with
            | SequenceExpression(seqExprFn, exprs) ->
                let rec flatten result = function
                    | [] -> List.rev result
                    | SequenceExpression(_, exprs)::restExprs ->
                        let exprs = flatten [] exprs |> List.fold (fun r e -> e::r) result
                        flatten exprs restExprs
                    | expr::restExprs -> flatten (expr::result) restExprs
                let exprs, returnExpr = flatten [] exprs |> List.splitLast
                let exprs = ListLiteral(exprs, false) |> Literal
                Expression.invocationExpression(Expression.identExpression seqExprFn, [exprs; returnExpr])
                |> printer.Print

            | EmitExpression(value, args) -> printer.PrintEmitExpression(value, args)

            | ThrowExpression e ->
                printer.Print("throw ")
                printer.Print(e)

            | RethrowExpression -> printer.Print("rethrow")

            | SuperExpression -> printer.Print("super")

            | ThisExpression -> printer.Print("this")

            | Literal kind -> printer.PrintLiteral(kind)

            | TypeLiteral t -> printer.PrintType(t)

            | IdentExpression i -> printer.PrintIdent(i)

            | ConditionalExpression(test, consequent, alternate) ->
                match test, consequent, alternate with
                | Literal(BooleanLiteral(value=value)), _, _ ->
                    if value then printer.Print(consequent)
                    else printer.Print(alternate)
                | test, Literal(BooleanLiteral(true)), Literal(BooleanLiteral(false)) ->
                    printer.Print(test)
                | test, Literal(BooleanLiteral(false)), Literal(BooleanLiteral(true)) ->
                    printer.Print("!")
                    printer.PrintWithParensIfComplex(test)
                | test, _, Literal(BooleanLiteral(false)) ->
                    printer.PrintWithParensIfComplex(test)
                    printer.Print(" && ")
                    printer.PrintWithParensIfComplex(consequent)
                | _ ->
                    printer.PrintWithParensIfComplex(test)
                    printer.Print(" ? ")
                    printer.PrintWithParensIfComplex(consequent)
                    printer.Print(" : ")
                    printer.PrintWithParensIfComplex(alternate)

            | UpdateExpression(op, isPrefix, expr) ->
                let printOp = function
                    | UpdateMinus -> printer.Print("--")
                    | UpdatePlus -> printer.Print("++")
                if isPrefix then
                    printOp op
                    printer.PrintWithParensIfComplex(expr)
                else
                    printer.PrintWithParensIfComplex(expr)
                    printOp op

            | UnaryExpression(op, expr) ->
                let printUnaryOp (op: string) (expr: Expression) =
                    printer.Print(op)
                    printer.PrintWithParensIfNotIdent(expr)
                match op with
                | UnaryMinus -> printUnaryOp "-" expr
                | UnaryNot -> printUnaryOp "!" expr
                | UnaryNotBitwise -> printUnaryOp "~" expr
                // TODO: I think Dart doesn't accept + prefix, check
                | UnaryPlus
                | UnaryAddressOf -> printer.Print(expr)

            | BinaryExpression(op, left, right, isInt) ->
                printer.PrintBinaryExpression(op, left, right, isInt)

            | LogicalExpression(op, left, right) ->
                printer.PrintLogicalExpression(op, left, right)

            | AssignmentExpression(target, kind, value) ->
                let op =
                    // TODO: Copied from Babel, review
                    match kind with
                    | AssignEqual -> " = "
                    | AssignMinus -> " -= "
                    | AssignPlus -> " += "
                    | AssignMultiply -> " *= "
                    | AssignDivide -> " /= "
                    | AssignModulus -> " %= "
                    | AssignShiftLeft -> " <<= "
                    | AssignShiftRightSignPropagating -> " >>= "
                    | AssignShiftRightZeroFill -> " >>>= "
                    | AssignOrBitwise -> " |= "
                    | AssignXorBitwise -> " ^= "
                    | AssignAndBitwise -> " &= "
                printer.Print(target)
                printer.Print(op)
                printer.Print(value)

            | PropertyAccess(expr, prop) ->
                printer.PrintWithParensIfComplex(expr)
                printer.Print("." + prop)

            | IndexExpression(expr, index) ->
                printer.PrintWithParensIfComplex(expr)
                printer.Print("[")
                printer.Print(index)
                printer.Print("]")

            | AsExpression(expr, typ) ->
                printer.PrintWithParensIfComplex(expr)
                printer.Print(" as ")
                printer.PrintType(typ)

            | IsExpression(expr, typ, isNot) ->
                printer.PrintWithParensIfComplex(expr)
                if isNot then
                    printer.Print(" !is ")
                else
                    printer.Print(" is ")
                printer.PrintType(typ)

            | InvocationExpression(caller, genArgs, args, isConst) ->
                if isConst then
                    printer.Print("const ")
                printer.PrintWithParensIfNotIdent(caller)
                printer.PrintList("<", ", ", ">", genArgs, printer.PrintType, skipIfEmpty=true)
                printer.PrintList("(", ")", args, printer.PrintCallArgAndSeparator)

            | AnonymousFunction(args, body, genArgs) ->
                printer.PrintList("<", genArgs, ">", skipIfEmpty=true)
                printer.PrintList("(", args, ") ", printType=true)
                printer.PrintFunctionBody(body, isExpression=true)

        member printer.PrintClassDeclaration(decl: Class) =
            if decl.IsAbstract then
                printer.Print("abstract ")
            printer.Print("class " + decl.Name + " ")
            let callSuper =
                match decl.Extends with
                | None -> false
                | Some t ->
                    printer.Print("extends ")
                    printer.PrintType(t)
                    printer.Print(" ")
                    true

            printer.PrintList("implements ", ", ", " ", decl.Implements, printer.PrintType, skipIfEmpty=true)

            let members = [
                yield! decl.InstanceVariables |> List.map Choice1Of3
                match decl.Constructor with
                | Some c -> Choice2Of3 c
                | None -> ()
                yield! decl.InstanceMethods |> List.map Choice3Of3
            ]

            printer.PrintBlock(members, (fun p m ->
                match m with
                | Choice1Of3 v ->
                    if v.IsOverride then
                        p.Print("@override")
                        p.PrintNewLine()
                    p.PrintVariableDeclaration(v.Ident, v.Kind, ?value=v.Value)
                    p.Print(";")

                // Constructor
                | Choice2Of3 c ->
                    if c.IsConst then
                        p.Print("const ")
                    if c.IsFactory then
                        p.Print("factory ")
                    p.Print(decl.Name)
                    printer.PrintList("(", ", ", ")", c.Args, function
                        | ConsThisArg name ->
                            printer.Print("this.")
                            printer.Print(name)
                        | ConsArg i ->
                            printer.PrintIdent(i, printType=true)
                    )

                    if callSuper then
                        p.Print(": super")
                        printer.PrintList("(", ")", c.SuperArgs, printer.PrintCallArgAndSeparator)
                    match c.Body with
                    | [] -> p.Print(";")
                    | body ->
                        p.Print(" ")
                        p.PrintBlock(body)
                | Choice3Of3 m ->
                    if m.IsOverride then
                        p.Print("@override")
                        p.PrintNewLine()

                    match m.Kind with
                    | IsGetter ->
                        p.PrintType(m.ReturnType)
                        p.Print(" get " + m.Name)
                        p.PrintFunctionBody(?body=m.Body)
                    | IsSetter ->
                        p.PrintType(m.ReturnType)
                        p.Print(" set " + m.Name)
                        let argIdents = m.Args |> List.map (fun a -> a.Ident)
                        printer.PrintList("(", argIdents, ") ", printType=true)
                        p.PrintFunctionBody(?body=m.Body)
                    | IsMethod ->
                        p.PrintFunctionDeclaration(m.ReturnType, m.Name, m.GenericArgs, m.Args, ?body=m.Body)
            ), fun p -> p.PrintNewLine())

        member printer.PrintFunctionBody(?body: Statement list, ?isExpression: bool) =
            let isExpression = defaultArg isExpression false
            match body with
            | None -> printer.Print(";")
            | Some [ReturnStatement expr] ->
                printer.Print(" => ")
                printer.Print(expr)
                if not isExpression then
                    printer.Print(";")
            | Some body ->
                printer.Print(" ")
                printer.PrintBlock(body, skipNewLineAtEnd=isExpression)

        member printer.PrintFunctionDeclaration(returnType: Type, name: string, genArgs: string list, args: FunctionArg list, ?body: Statement list) =
            printer.PrintType(returnType)
            printer.Print(" ")
            printer.Print(name)
            printer.PrintList("<", genArgs, ">", skipIfEmpty=true)

            let mutable prevArg: FunctionArg option = None
            printer.PrintList("(", ")", args, fun pos arg ->
                if arg.IsNamed then
                    match prevArg with
                    | None -> printer.Print("{")
                    | Some a when not a.IsNamed -> printer.Print("{")
                    | Some _ -> ()
                elif arg.IsOptional then
                    match prevArg with
                    | None -> printer.Print("[")
                    | Some a when not a.IsOptional -> printer.Print("[")
                    | Some _ -> ()
                else
                    ()

                printer.PrintIdent(arg.Ident, printType=true)

                match pos with
                | IsSingle | IsLast ->
                    if arg.IsNamed then
                        printer.Print("}")
                    elif arg.IsOptional then
                        printer.Print("]")
                    else ()
                | IsFirst | IsMiddle ->
                    printer.Print(", ")

                prevArg <- Some arg
            )
            printer.PrintFunctionBody(?body=body)

        member printer.PrintVariableDeclaration(ident: Ident, kind: VariableDeclarationKind, ?value: Expression) =
            match value with
            | None ->
                match kind with
                | Final -> printer.Print("final ")
                | _ -> ()
                printer.PrintType(ident.Type)
                printer.Print(" " + ident.Name)
            | Some value ->
                match kind with
                | Const -> printer.Print("const " + ident.Name + " = ")
                | Final -> printer.Print("final " + ident.Name + " = ")
                | Var -> printer.Print("var " + ident.Name + " = ")
                printer.Print(value)

open PrinterExtensions

let run (writer: Writer) (file: File): Async<unit> =
    let printDeclWithExtraLine extraLine (printer: Printer) (decl: Declaration) =
        match decl with
        | ClassDeclaration decl ->
            printer.PrintClassDeclaration(decl)

        | FunctionDeclaration d ->
            printer.PrintFunctionDeclaration(d.ReturnType, d.Name, d.GenericArgs, d.Args, d.Body)
            printer.PrintNewLine()

        | VariableDeclaration(ident, kind, value) ->
            printer.PrintVariableDeclaration(ident, kind, value)
            printer.Print(";")
            printer.PrintNewLine()

        if extraLine then
            printer.PrintNewLine()

    async {
        use printerImpl = new PrinterImpl(writer)
        let printer = printerImpl :> Printer

        printer.Print("// ignore_for_file: non_constant_identifier_names")
        printer.PrintNewLine()

        for i in file.Imports do
            let path = printer.MakeImportPath(i.Path)
            match i.LocalIdent with
            | None -> printer.Print("import '" + path + "';")
            | Some localId -> printer.Print("import '" + path + "' as " + localId + ";")
            printer.PrintNewLine()

        printer.PrintNewLine()
        do! printerImpl.Flush()

        for decl in file.Declarations do
            printDeclWithExtraLine true printer decl
            do! printerImpl.Flush()
    }