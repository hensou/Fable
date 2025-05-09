// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

// This component is used by the 'fsharpqa' tests for faster in-memory compilation.  It should be removed and the
// proper compiler service API used instead.

namespace FSharp.Compiler.CodeAnalysis.Hosted

open System
open System.IO
open System.Text.RegularExpressions
open FSharp.Compiler.Diagnostics
open FSharp.Compiler.Driver
open FSharp.Compiler.DiagnosticsLogger
open FSharp.Compiler.CompilerConfig
open FSharp.Compiler.CompilerDiagnostics
open FSharp.Compiler.AbstractIL.ILBinaryReader
open Internal.Utilities.Library

/// Part of LegacyHostedCompilerForTesting
///
/// Yet another DiagnosticsLogger implementation, capturing the messages but only up to the maxerrors maximum
type internal InProcDiagnosticsLoggerProvider() =
    let errors = ResizeArray()
    let warnings = ResizeArray()

    member _.Provider =
        { new IDiagnosticsLoggerProvider with

            member _.CreateLogger(tcConfigB, exiter) =

                { new DiagnosticsLoggerUpToMaxErrors(tcConfigB, exiter, "InProcCompilerDiagnosticsLoggerUpToMaxErrors") with

                    member _.HandleTooManyErrors text =
                        warnings.Add(FormattedDiagnostic.Short(FSharpDiagnosticSeverity.Warning, text))

                    member _.HandleIssue(tcConfig, err, severity) =
                        // 'true' is passed for "suggestNames", since we want to suggest names with fsc.exe runs and this doesn't affect IDE perf
                        let diagnostics = CollectFormattedDiagnostics(tcConfig, severity, err, true)

                        match severity with
                        | FSharpDiagnosticSeverity.Error -> errors.AddRange(diagnostics)
                        | FSharpDiagnosticSeverity.Warning -> warnings.AddRange(diagnostics)
                        | _ -> ()
                }
                :> DiagnosticsLogger
        }

    member _.CapturedErrors = errors.ToArray()

    member _.CapturedWarnings = warnings.ToArray()

/// build issue location
type internal Location =
    {
        StartLine: int
        StartColumn: int
        EndLine: int
        EndColumn: int
    }

type internal CompilationIssueType =
    | Warning
    | Error

/// build issue details
type internal CompilationIssue =
    {
        Location: Location
        Subcategory: string
        Code: string
        File: string
        Text: string
        Type: CompilationIssueType
    }

/// combined warning and error details
type internal FailureDetails =
    {
        Warnings: CompilationIssue list
        Errors: CompilationIssue list
    }

type internal CompilationResult =
    | Success of CompilationIssue list
    | Failure of FailureDetails

[<RequireQualifiedAccess>]
type internal CompilationOutput =
    {
        Errors: FormattedDiagnostic[]
        Warnings: FormattedDiagnostic[]
    }

type internal InProcCompiler(legacyReferenceResolver) =
    member _.Compile(argv) =

        // Explanation: Compilation happens on whichever thread calls this function.
        let ctok = AssumeCompilationThreadWithoutEvidence()

        let loggerProvider = InProcDiagnosticsLoggerProvider()
        let exiter = StopProcessingExiter()

        try
            CompileFromCommandLineArguments(
                ctok,
                argv,
                legacyReferenceResolver,
                false,
                ReduceMemoryFlag.Yes,
                CopyFSharpCoreFlag.Yes,
                exiter,
                loggerProvider.Provider,
                None,
                None
            )
        with
        | StopProcessing -> ()
        | ReportedError _
        | WrappedError(ReportedError _, _) ->
            exiter.ExitCode <- 1
            ()

        let output: CompilationOutput =
            {
                Warnings = loggerProvider.CapturedWarnings
                Errors = loggerProvider.CapturedErrors
            }

        (exiter.ExitCode = 0), output

/// in-proc version of fsc.exe
type internal FscCompiler(legacyReferenceResolver) =
    let compiler = InProcCompiler(legacyReferenceResolver)

    let emptyLocation =
        {
            StartColumn = 0
            EndColumn = 0
            StartLine = 0
            EndLine = 0
        }

    /// Converts short and long issue types to the same CompilationIssue representation
    let convert issue =
        match issue with
        | FormattedDiagnostic.Short(severity, text) ->
            {
                Location = emptyLocation
                Code = ""
                Subcategory = ""
                File = ""
                Text = text
                Type =
                    if (severity = FSharpDiagnosticSeverity.Error) then
                        CompilationIssueType.Error
                    else
                        CompilationIssueType.Warning
            }
        | FormattedDiagnostic.Long(severity, details) ->
            let loc, file =
                match details.Location with
                | Some l when not l.IsEmpty ->
                    {
                        StartColumn = l.Range.StartColumn
                        EndColumn = l.Range.EndColumn
                        StartLine = l.Range.StartLine
                        EndLine = l.Range.EndLine
                    },
                    l.File
                | _ -> emptyLocation, ""

            {
                Location = loc
                Code = sprintf "FS%04d" details.Canonical.ErrorNumber
                Subcategory = details.Canonical.Subcategory
                File = file
                Text = details.Message
                Type =
                    if (severity = FSharpDiagnosticSeverity.Error) then
                        CompilationIssueType.Error
                    else
                        CompilationIssueType.Warning
            }

    /// test if --test:ErrorRanges flag is set
    let errorRangesArg =
#if FABLE_COMPILER
        arg.Equals(@"/test:ErrorRanges", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals(@"--test:ErrorRanges", StringComparison.OrdinalIgnoreCase)
#else
        let regex =
            Regex(@"^(/|--)test:ErrorRanges$", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

        fun (arg: string) -> regex.IsMatch(arg)
#endif

    /// test if --vserrors flag is set
    let vsErrorsArg =
#if FABLE_COMPILER
        arg.Equals(@"/vserrors", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals(@"--vserrors", StringComparison.OrdinalIgnoreCase)
#else
        let regex =
            Regex(@"^(/|--)vserrors$", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

        fun (arg: string) -> regex.IsMatch(arg)
#endif

    /// test if an arg is a path to fsc.exe
    let fscExeArg =
#if FABLE_COMPILER
        arg.EndsWith(@"fsc", StringComparison.OrdinalIgnoreCase) ||
        arg.EndsWith(@"fsc.exe", StringComparison.OrdinalIgnoreCase)
#else
        let regex =
            Regex(@"fsc(\.exe)?$", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

        fun (arg: string) -> regex.IsMatch(arg)
#endif

    /// do compilation as if args was argv to fsc.exe
    member _.Compile(args: string[]) =
        // args.[0] is later discarded, assuming it is just the path to fsc.
        // compensate for this in case caller didn't know
        let args =
            match box args with
            | null -> [| "fsc" |]
            | _ ->
                match args with
                | [||] -> [| "fsc" |]
                | a when not <| fscExeArg a[0] -> Array.append [| "fsc" |] a
                | _ -> args

        let errorRanges = args |> Seq.exists errorRangesArg
        let vsErrors = args |> Seq.exists vsErrorsArg

        let ok, result = compiler.Compile(args)
        let exitCode = if ok then 0 else 1

        let lines =
            Seq.append result.Errors result.Warnings
            |> Seq.map convert
            |> Seq.map (fun issue ->
                let issueTypeStr =
                    match issue.Type with
                    | Error ->
                        if vsErrors then
                            sprintf "%s error" issue.Subcategory
                        else
                            "error"
                    | Warning ->
                        if vsErrors then
                            sprintf "%s warning" issue.Subcategory
                        else
                            "warning"

                let locationStr =
                    if vsErrors then
                        sprintf
                            "(%d,%d,%d,%d)"
                            issue.Location.StartLine
                            issue.Location.StartColumn
                            issue.Location.EndLine
                            issue.Location.EndColumn
                    elif errorRanges then
                        sprintf
                            "(%d,%d-%d,%d)"
                            issue.Location.StartLine
                            issue.Location.StartColumn
                            issue.Location.EndLine
                            issue.Location.EndColumn
                    else
                        sprintf "(%d,%d)" issue.Location.StartLine issue.Location.StartColumn

                sprintf "%s: %s %s: %s" locationStr issueTypeStr issue.Code issue.Text)
            |> Array.ofSeq

        (exitCode, lines)

module internal CompilerHelpers =

    /// splits a provided command line string into argv array
    /// currently handles quotes, but not escaped quotes
    let parseCommandLine (commandLine: string) =
        let folder (inQuote: bool, currArg: string, argLst: string list) ch =
            match (ch, inQuote) with
            | '"', _ -> (not inQuote, currArg, argLst)
            | ' ', false ->
                if currArg.Length > 0 then
                    (inQuote, "", currArg :: argLst)
                else
                    (inQuote, "", argLst)
            | _ -> (inQuote, currArg + (string ch), argLst)

        seq {
            yield! commandLine.ToCharArray()
            yield ' '
        }
        |> Seq.fold folder (false, "", [])
        |> (fun (_, _, args) -> args)
        |> List.rev
        |> Array.ofList

    /// runs in-proc fsc compilation, returns array consisting of exit code, then compiler output
    let fscCompile legacyReferenceResolver directory args =
        // in-proc compiler still prints banner to console, so need this to capture it
        let origOut = Console.Out
        let origError = Console.Error
        let sw = new StringWriter()
        Console.SetOut(sw)
        let ew = new StringWriter()
        Console.SetError(ew)

        try
            try
                Directory.SetCurrentDirectory directory
                let exitCode, output = FscCompiler(legacyReferenceResolver).Compile(args)

                let consoleOut =
                    sw.ToString().Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)

                let consoleError =
                    ew.ToString().Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)

                exitCode, [| yield! consoleOut; yield! output |], consoleError
            with e ->
                1,
                [|
                    "Internal compiler error"
                    e.ToString().Replace('\n', ' ').Replace('\r', ' ')
                |],
                [||]
        finally
            Console.SetOut(origOut)
            Console.SetError(origError)
