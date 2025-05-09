module Fable.Tests.ComparisonTests

open Util.Testing
open System.Collections.Generic
open FSharp.Data.UnitSystems.SI.UnitSymbols

type UTest = A of int | B of int
type RTest = { a: int; b: int }
type STest = struct val A: int; new(a: int) = { A = a }; end
type OTest(a) = member val A = a with get, set

// [<CustomEquality; CustomComparison>]
// type UTest2 =
//     | String of string
//     override x.GetHashCode() = x.GetHashCode()
//     override x.Equals(yobj) =
//        match yobj with
//          | :? UTest2 as y ->
//             match x, y with
//             | String s1, String s2 -> (s1 + s1) = s2
//          | _ -> false
//     interface System.IEquatable<UTest2> with
//         member x.Equals(y) =
//             match x, y with
//             | String s1, String s2 -> (s1 + s1) = s2
//     interface System.IComparable with
//         member x.CompareTo(yobj) =
//             match yobj with
//             | :? UTest2 as y ->
//                 match x, y with
//                 | String s1, String s2 -> compare (s1 + s1) s2
//             | _ -> invalidArg "yobj" "cannot compare values of different types"

// exception Ex of int

// [<ReferenceEquality>]
// type RTest2 = { a2: int; b2: int }

// type Test(i: int) =
//     member x.Value = i
//     override x.GetHashCode() = i
//     override x.Equals(yobj) =
//        match yobj with
//          | :? Test as y -> y.Value + 1 = x.Value
//          | _ -> false
//     interface System.IComparable with
//         member x.CompareTo(yobj) =
//             match yobj with
//             | :? Test as y -> compare (y.Value + 1) x.Value
//             | _ -> invalidArg "yobj" "cannot compare values of different types"
//             // | _ -> -1

//     interface System.IEquatable<Test> with
//         member x.Equals(y) =
//             y.Value + 1 = x.Value

type Status =
    | CreateScenePicture
    | ReadingOldDevice
    | CreateOldMeterReadingPicture
    | SelectingNewDevice
    | ReadingNewDevice
    | CreateNewMeterReadingPicture
    | GetSignature
    | Done

type MyClass(v) =
    member val Value: int = v with get, set

// [<CustomEquality; NoComparison>]
// type FuzzyInt =
//     | FuzzyInt of int
//     override x.GetHashCode() =
//         let (FuzzyInt x) = x
//         x.GetHashCode()
//     override x.Equals(y: obj) =
//         match y with
//         | :? FuzzyInt as y ->
//             let (FuzzyInt x) = x
//             let (FuzzyInt y) = y
//             x - 2 <= y && y <= x + 2
//         | _ -> false

let genericEquals<'T when 'T: equality> (a: 'T) (b: 'T) : bool =
    let cmp = EqualityComparer<'T>.Default
    cmp.Equals(a, b)

let genericHash<'T when 'T: equality> (x: 'T) : int =
    let cmp = EqualityComparer<'T>.Default
    cmp.GetHashCode(x)

let genericCompare<'T when 'T: comparison> (a: 'T) (b: 'T) : int =
    let cmp = Comparer<'T>.Default
    cmp.Compare(a, b)

[<Fact>]
let ``Typed array equality works`` () =
    let xs1 = [| 1; 2; 3 |]
    let xs2 = [| 1; 2; 3 |]
    let xs3 = [| 1; 2; 4 |]
    let xs4 = [| 1; 2 |]
    equal true (xs1 = xs2)
    equal false (xs1 = xs3)
    equal true (xs1 <> xs3)
    equal false (xs1 <> xs2)
    equal true (xs1 <> xs4)

[<Fact>]
let ``Array equality works`` () =
    let xs1 = [| "1"; "2"; "3" |]
    let xs2 = [| "1"; "2"; "3" |]
    let xs3 = [| "1"; "2"; "4" |]
    let xs4 = [| "1"; "2" |]
    equal true (xs1 = xs2)
    equal false (xs1 = xs3)
    equal true (xs1 <> xs3)
    equal false (xs1 <> xs2)
    equal true (xs1 <> xs4)

// [<Fact>]
// let ``Array custom equality works`` () =
//     let xs = [| FuzzyInt 3; FuzzyInt 5 |]
//     let ys = [| FuzzyInt 2; FuzzyInt 4 |]
//     let zs = [| FuzzyInt 2; FuzzyInt 8 |]
//     xs = ys |> equal true
//     xs = zs |> equal false

[<Fact>]
let ``Tuple equality works`` () =
    let xs1 = ( 1, 2, 3 )
    let xs2 = ( 1, 2, 3 )
    let xs3 = ( 1, 2, 4 )
    equal true (xs1 = xs2)
    equal false (xs1 = xs3)
    equal true (xs1 <> xs3)
    equal false (xs1 <> xs2)

[<Fact>]
let ``List equality works`` () =
    let xs1 = [ 1; 2; 3 ]
    let xs2 = [ 1; 2; 3 ]
    let xs3 = [ 1; 2; 4 ]
    equal true (xs1 = xs2)
    equal false (xs1 = xs3)
    equal true (xs1 <> xs3)
    equal false (xs1 <> xs2)

[<Fact>]
let ``Set equality works`` () =
    let xs1 = Set [ 1; 2; 3 ]
    let xs2 = Set [ 1; 2; 3 ]
    let xs3 = Set [ 1; 2; 4 ]
    let xs4 = Set [ 3; 2; 1 ]
    let xs5 = Set [ 1; 2; 3; 1 ]
    equal true (xs1 = xs2)
    equal false (xs1 = xs3)
    equal true (xs1 <> xs3)
    equal false (xs1 <> xs2)
    equal true (xs1 = xs4)
    equal false (xs1 <> xs5)

[<Fact>]
let ``Map equality works`` () =
    let xs1 = Map [ ("a", 1); ("b", 2); ("c", 3) ]
    let xs2 = Map [ ("a", 1); ("b", 2); ("c", 3) ]
    let xs3 = Map [ ("a", 1); ("b", 2); ("c", 4) ]
    let xs4 = Map [ ("c", 3); ("b", 2); ("a", 1) ]
    equal true (xs1 = xs2)
    equal false (xs1 = xs3)
    equal true (xs1 <> xs3)
    equal false (xs1 <> xs2)
    equal true (xs1 = xs4)

[<Fact>]
let ``Union equality works`` () =
    let u1 = A 2
    let u2 = A 2
    let u3 = A 4
    let u4 = B 2
    equal true (u1 = u2)
    equal false (u1 = u3)
    equal true (u1 <> u3)
    equal false (u1 <> u2)
    equal false (u1 = u4)
    System.Object.ReferenceEquals(u1, u1) |> equal true
    System.Object.ReferenceEquals(u1, u2) |> equal false

// [<Fact>]
// let ``Union custom equality works`` () =
//     let u1 = String "A"
//     let u2 = String "A"
//     let u3 = String "AA"
//     equal false (u1 = u2)
//     equal true (u1 = u3)

[<Fact>]
let ``Record equality works`` () =
    let r1 = { a = 1; b = 2 }
    let r2 = { a = 1; b = 2 }
    let r3 = { a = 1; b = 4 }
    equal true (r1 = r2)
    equal false (r1 = r3)
    equal true (r1 <> r3)
    equal false (r1 <> r2)
    System.Object.ReferenceEquals(r1, r1) |> equal true
    System.Object.ReferenceEquals(r1, r2) |> equal false

// [<Fact>]
// let ``Exception equality works`` () =
//     equal true ((Ex 1) = (Ex 1))
//     equal false ((Ex 1) = (Ex 2))

// TODO: If we want to make this work in Fable 2 we'll have
// to access reflection info for records

// #if FABLE_COMPILER
// [<Fact>]
// let ``Record equality ignores dynamic fields`` () =
//     let r1 = { a = 1; b = 2 }
//     r1?c <- 1
//     let r2 = { a = 1; b = 2 }
//     r2?c <- 2
//     equal true (r1 = r2)
//     equal false (r1 <> r2)

// [<Fact>]
// let ``Record comparison ignores dynamic fields`` () =
//     let r1 = { a = 1; b = 2 }
//     r1?c <- 1
//     let r2 = { a = 1; b = 2 }
//     r2?c <- 2
//     equal 0 (compare r1 r2)
// #endif

// [<Fact>]
// let ``Equality with objects implementing IEquatable works`` () =
//     let c1 = Test(5)
//     let c2 = Test(4)
//     let c3 = Test(5)
//     equal true (c1 = c2)
//     equal false (c1 = c3)
//     equal true (c1 <> c3)
//     equal false (c1 <> c2)
//     System.Object.ReferenceEquals(c1, c1) |> equal true
//     System.Object.ReferenceEquals(c1, c2) |> equal false

[<Fact>]
let ``Typed array comparison works`` () =
    let xs1 = [| 1; 2; 3 |]
    let xs2 = [| 1; 2; 3 |]
    let xs3 = [| 1; 2; 4 |]
    let xs4 = [| 1; 2; 2 |]
    let xs5 = [| 1; 2 |]
    let xs6 = [| 1; 2; 3; 1 |]
    equal 0 (compare xs1 xs2)
    equal -1 (compare xs1 xs3)
    equal true (xs1 < xs3)
    equal 1 (compare xs1 xs4)
    equal false (xs1 < xs4)
    equal 1 (compare xs1 xs5)
    equal true (xs1 > xs5)
    equal -1 (compare xs1 xs6)
    equal false (xs1 > xs6)

[<Fact>]
let ``Array comparison works`` () =
    let xs1 = [| "1"; "2"; "3" |]
    let xs2 = [| "1"; "2"; "3" |]
    let xs3 = [| "1"; "2"; "4" |]
    let xs4 = [| "1"; "2"; "2" |]
    let xs5 = [| "1"; "2" |]
    let xs6 = [| "1"; "2"; "3"; "1" |]
    equal 0 (compare xs1 xs2)
    equal -1 (compare xs1 xs3)
    equal true (xs1 < xs3)
    equal 1 (compare xs1 xs4)
    equal false (xs1 < xs4)
    equal 1 (compare xs1 xs5)
    equal true (xs1 > xs5)
    equal -1 (compare xs1 xs6)
    equal false (xs1 > xs6)

[<Fact>]
let ``Tuple comparison works`` () =
    let xs1 = ( 1, 2, 3 )
    let xs2 = ( 1, 2, 3 )
    let xs3 = ( 1, 2, 4 )
    let xs4 = ( 1, 2, 2 )
    equal 0 (compare xs1 xs2)
    equal -1 (compare xs1 xs3)
    equal true (xs1 < xs3)
    equal 1 (compare xs1 xs4)
    equal false (xs1 < xs4)

[<Fact>]
let ``List comparison works`` () =
    let xs1 = [ 1; 2; 3 ]
    let xs2 = [ 1; 2; 3 ]
    let xs3 = [ 1; 2; 4 ]
    let xs4 = [ 1; 2; 2 ]
    let xs5 = [ 1; 2 ]
    let xs6 = [ 1; 2; 3; 1 ]
    equal 0 (compare xs1 xs2)
    equal -1 (compare xs1 xs3)
    equal true (xs1 < xs3)
    equal 1 (compare xs1 xs4)
    equal false (xs1 < xs4)
    equal 1 (compare xs1 xs5)
    equal true (xs1 > xs5)
    equal -1 (compare xs1 xs6)
    equal false (xs1 > xs6)

[<Fact>]
let ``Set comparison works`` () =
    let xs1 = Set [ 1; 2; 3 ]
    let xs2 = Set [ 1; 2; 3 ]
    let xs3 = Set [ 1; 2; 4 ]
    let xs4 = Set [ 1; 2; 2 ]
    let xs5 = Set [ 1; 2 ]
    let xs6 = Set [ 1; 2; 3; 1 ]
    equal 0 (compare xs1 xs2)
    equal -1 (compare xs1 xs3)
    equal true (xs1 < xs3)
    equal 1 (compare xs1 xs4)
    equal false (xs1 < xs4)
    equal 1 (compare xs1 xs5)
    equal true (xs1 > xs5)
    equal 0 (compare xs1 xs6)

[<Fact>]
let ``Map comparison works`` () =
    let xs1 = Map [ ("a", 1); ("b", 2); ("c", 3) ]
    let xs2 = Map [ ("a", 1); ("b", 2); ("c", 3) ]
    let xs3 = Map [ ("a", 1); ("b", 2); ("c", 4) ]
    let xs4 = Map [ ("a", 1); ("b", 2); ("c", 2) ]
    let xs5 = Map [ ("a", 1); ("b", 2) ]
    let xs6 = Map [ ("a", 1); ("b", 2); ("c", 3); ("d", 1) ]
    equal 0 (compare xs1 xs2)
    equal -1 (compare xs1 xs3)
    equal true (xs1 < xs3)
    equal 1 (compare xs1 xs4)
    equal false (xs1 < xs4)
    equal 1 (compare xs1 xs5)
    equal true (xs1 > xs5)
    equal -1 (compare xs1 xs6)
    equal false (xs1 > xs6)

[<Fact>]
let ``Union comparison works`` () =
    let u1 = A 2
    let u2 = A 2
    let u3 = A 4
    let u4 = A 1
    let u5 = B 2
    equal 0 (compare u1 u2)
    equal -1 (compare u1 u3)
    equal true (u1 < u3)
    equal 1 (compare u1 u4)
    equal false (u1 < u4)
    (compare u1 u5) = 0 |> equal false

// [<Fact>]
// let ``Union custom comparison works`` () =
//     let u1 = String "A"
//     let u2 = String "A"
//     let u3 = String "AA"
//     equal 0 (compare u1 u3)
//     equal true (compare u1 u2 > 0)

[<Fact>]
let ``Record comparison works`` () =
    let r1 = { a = 1; b = 2 }
    let r2 = { a = 1; b = 2 }
    let r3 = { a = 1; b = 4 }
    equal 0 (compare r1 r2)
    (compare r1 r3) = 0 |> equal false

// [<Fact>]
// let ``Comparison with objects implementing IComparable works`` () =
//     let c1 = Test(5)
//     let c2 = Test(4)
//     let c3 = Test(5)
//     equal 0 (compare c1 c2)
//     equal 1 (compare c1 c3)
//     equal true (c1 > c3)

[<Fact>]
let ``max works with primitives`` () =
    max 1 2 |> equal 2
    max 10m 2m |> equal 10m
    System.Math.Max(1, 2) |> equal 2
    max "a" "b" |> equal "b"

[<Fact>]
let ``max works with records`` () =
    let r1 = {a=1; b=1}
    let r2 = {a=1; b=2}
    max r1 r2 |> equal r2

// [<Fact>]
// let ``max with objects implementing IComparable works`` () =
//     let c1 = Test(5)
//     let c2 = Test(5)
//     System.Object.ReferenceEquals(max c1 c2, c1) |> equal true

[<Fact>]
let ``min works with primitives`` () =
    min 1 2 |> equal 1
    min 10m 2m |> equal 2m
    System.Math.Min(1, 2) |> equal 1
    min "a" "b" |> equal "a"

[<Fact>]
let ``min works with records`` () =
    let r1 = {a=1; b=1}
    let r2 = {a=1; b=2}
    min r1 r2 |> equal r1

// [<Fact>]
// let ``min with objects implementing IComparable works`` () =
//     let c1 = Test(5)
//     let c2 = Test(5)
//     System.Object.ReferenceEquals(min c1 c2, c2) |> equal true

// [<Fact>]
// let ``nullArg works`` () =
//     try
//         nullArg null
//         true
//     with _ex ->
//         false
//     |> equal false

// [<Fact>]
// let ``using function disposes the resource when action finishes`` () =
//     let mutable disposed = false
//     let resource = { new System.IDisposable with member _.Dispose() = disposed <- true }
//     using resource (fun _resource -> ())
//     equal true disposed

// [<Fact>]
// let ``using function disposes the resource when action fails`` () =
//     let mutable disposed = false
//     let resource = { new System.IDisposable with member _.Dispose() = disposed <- true }
//     try
//         using resource (fun _resource -> failwith "action failed")
//     with
//     | _ -> () // ignore
//     equal true disposed

// [<Fact>]
// let ``isNull with primitives works`` () =
//     isNull null |> equal true
//     isNull (box 5) |> equal false
//     isNull "" |> equal false
//     isNull "0" |> equal false
//     isNull "hello" |> equal false

// [<Fact>]
// let ``isNull with objects works`` () =
//     let s1: System.String = null
//     isNull s1 |> equal true
//     let s2: System.String = "hello"
//     isNull s2 |> equal false

// [<Fact>]
// let ``Classes must use identity hashing by default`` () = // See #2291
//     let x = MyClass(5)
//     let y = MyClass(5)
//     let h1 = hash(box x)
//     let h2 = hash(box y)
//     x.Value <- 8
//     let h3 = hash(box x)
//     h1 = h2 |> equal false
//     h1 = h3 |> equal true

[<Fact>]
let ``GetHashCode with arrays works`` () =
    let o1 = [|1; 2|]
    let o2 = [|1; 2|]
    let o3 = [|2; 1|]
    (o1.GetHashCode(), o1.GetHashCode()) ||> equal
    (o2.GetHashCode(), o1.GetHashCode()) ||> notEqual
    (o3.GetHashCode(), o1.GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with lists works`` () =
    ([1; 2].GetHashCode(), [1; 2].GetHashCode()) ||> equal
    ([2; 1].GetHashCode(), [1; 2].GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with tuples works`` () =
    ((1, 2).GetHashCode(), (1, 2).GetHashCode()) ||> equal
    ((2, 1).GetHashCode(), (1, 2).GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with options works`` () =
    let None_0: int option option = Some None
    ((Some 1).GetHashCode(), (Some 1).GetHashCode()) ||> equal
    ((Some 2).GetHashCode(), (Some 1).GetHashCode()) ||> notEqual
    ((None_0).GetHashCode(), (Some 1).GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with unions works`` () =
    ((UTest.A 1).GetHashCode(), (UTest.A 1).GetHashCode()) ||> equal
    ((UTest.A 2).GetHashCode(), (UTest.A 1).GetHashCode()) ||> notEqual
    ((UTest.B 1).GetHashCode(), (UTest.A 1).GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with records works`` () =
    ({a=1; b=2}.GetHashCode(), {a=1; b=2}.GetHashCode()) ||> equal
    ({a=2; b=1}.GetHashCode(), {a=1; b=2}.GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with structs works`` () =
    (STest(1).GetHashCode(), STest(1).GetHashCode()) ||> equal
    (STest(2).GetHashCode(), STest(1).GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with objects works`` () =
    let o1 = OTest(1)
    let o2 = OTest(1)
    let o3 = OTest(2)
    (o1.GetHashCode(), o1.GetHashCode()) ||> equal
    (o2.GetHashCode(), o1.GetHashCode()) ||> notEqual
    (o3.GetHashCode(), o1.GetHashCode()) ||> notEqual

// [<Fact>]
// let ``GetHashCode with objects that overwrite it works`` () =
//     (Test(1).GetHashCode(), Test(1).GetHashCode()) ||> equal
//     (Test(2).GetHashCode(), Test(1).GetHashCode()) ||> notEqual

[<Fact>]
let ``GetHashCode with same object works`` () =
    let o = OTest(1)
    let h1 = o.GetHashCode()
    o.A <- 2
    let h2 = o.GetHashCode()
    (h1, h2) ||> equal

[<Fact>]
let ``GetHashCode with primitives works`` () =
    ((1).GetHashCode(), (1).GetHashCode()) ||> equal
    ((2).GetHashCode(), (1).GetHashCode()) ||> notEqual
    ("1".GetHashCode(), "1".GetHashCode()) ||> equal
    ("2".GetHashCode(), "1".GetHashCode()) ||> notEqual

[<Fact>]
let ``Equals with primitives works`` () =
    (true).Equals(true) |> equal true
    ('1').Equals('1') |> equal true
    (1y).Equals(1y) |> equal true
    (1uy).Equals(1uy) |> equal true
    (1s).Equals(1s) |> equal true
    (1).Equals(1) |> equal true
    (1L).Equals(1L) |> equal true
    (1u).Equals(1u) |> equal true
    (1us).Equals(1us) |> equal true
    (1ul).Equals(1ul) |> equal true
    (1uL).Equals(1uL) |> equal true
    (1.f).Equals(1.f) |> equal true
    (1.).Equals(1.) |> equal true
    (1.m).Equals(1.m) |> equal true
    ("1").Equals("1") |> equal true

[<Fact>]
let ``CompareTo with primitives works`` () =
    (true).CompareTo(true) |> equal 0
    ('1').CompareTo('1') |> equal 0
    (1y).CompareTo(1y) |> equal 0
    (1uy).CompareTo(1uy) |> equal 0
    (1s).CompareTo(1s) |> equal 0
    (1).CompareTo(1) |> equal 0
    (1L).CompareTo(1L) |> equal 0
    (1u).CompareTo(1u) |> equal 0
    (1us).CompareTo(1us) |> equal 0
    (1ul).CompareTo(1ul) |> equal 0
    (1uL).CompareTo(1uL) |> equal 0
    (1.f).CompareTo(1.f) |> equal 0
    (1.).CompareTo(1.) |> equal 0
    (1.m).CompareTo(1.m) |> equal 0
    ("1").CompareTo("1") |> equal 0

// // This is going to give different results in .NET and JS
// // Just check no exception is thrown
// [<Fact>]
// let ``hash works with JS objects`` () = // See #2281
//     let _ = obj () |> hash
//     ()

[<Fact>]
let ``hash with arrays works`` () =
    (hash [|1; 2|], hash [|1; 2|]) ||> equal
    (hash [|2; 1|], hash [|1; 2|]) ||> notEqual

[<Fact>]
let ``hash with lists works`` () =
    (hash [1; 2], hash [1; 2]) ||> equal
    (hash [2; 1], hash [1; 2]) ||> notEqual

[<Fact>]
let ``hash with tuples works`` () =
    (hash (1, 2), hash (1, 2)) ||> equal
    (hash (2, 1), hash (1, 2)) ||> notEqual

[<Fact>]
let ``hash with options works`` () =
    let None_0: int option option = Some None
    (hash (Some 1), hash (Some 1)) ||> equal
    (hash (Some 2), hash (Some 1)) ||> notEqual
    (hash (None_0), hash (Some 1)) ||> notEqual

[<Fact>]
let ``hash with unions works`` () =
    (hash (UTest.A 1), hash (UTest.A 1)) ||> equal
    (hash (UTest.A 2), hash (UTest.A 1)) ||> notEqual
    (hash (UTest.B 1), hash (UTest.A 1)) ||> notEqual

[<Fact>]
let ``hash with records works`` () =
    (hash {a=1; b=2}, hash {a=1; b=2}) ||> equal
    (hash {a=2; b=1}, hash {a=1; b=2}) ||> notEqual

[<Fact>]
let ``hash with structs works`` () =
    (hash (STest(1)), hash (STest(1))) ||> equal
    (hash (STest(2)), hash (STest(1))) ||> notEqual

[<Fact>]
let ``hash with objects works`` () =
    // In Release mode for Rust, sequentially allocated objects that
    // are immediately released can get allocated at the same address.
    // This breaks referential equality, so delaying their release by
    // increasing their scope makes it work. See ReferenceEquals tests.
    //
    // (hash (OTest(1)), hash (OTest(1))) ||> notEqual // broken in Release mode
    // (hash (OTest(2)), hash (OTest(1))) ||> notEqual // broken in Release mode
    let o1 = OTest(1)
    let o2 = OTest(1)
    let o3 = OTest(2)
    (hash o1, hash o1) ||> equal
    (hash o2, hash o1) ||> notEqual
    (hash o3, hash o1) ||> notEqual

[<Fact>]
let ``hash with same object works`` () =
    let o = OTest(1)
    let h1 = hash o
    o.A <- 2
    let h2 = hash o
    (h1, h2) ||> equal

[<Fact>]
let ``hash with longs works`` () =
    (hash (1L<<<33), hash (1L<<<33)) ||> equal
    (hash (1L<<<34), hash (1L<<<33)) ||> notEqual
    (hash 3L, hash (3L + (1L<<<33))) ||> notEqual
    (hash (-3L), hash (3L))          ||> notEqual

[<Fact>]
let ``hash with primitives works`` () =
    (hash 111, hash 111) ||> equal
    (hash 222, hash 111) ||> notEqual
    (hash "1", hash "1") ||> equal
    (hash "2", hash "1") ||> notEqual

[<Fact>]
let ``Unchecked.hash with primitives works`` () =
    (Unchecked.hash 111, Unchecked.hash 111) ||> equal
    (Unchecked.hash 222, Unchecked.hash 333) ||> notEqual
    (Unchecked.hash "1", Unchecked.hash "1") ||> equal
    (Unchecked.hash "2", Unchecked.hash "3") ||> notEqual

[<Fact>]
let ``Unchecked.hash with lists works`` () =
    (Unchecked.hash [1;2], Unchecked.hash [1;2]) ||> equal
    (Unchecked.hash [2;1], Unchecked.hash [1;2]) ||> notEqual

[<Fact>]
let ``Unchecked.hash with arrays works`` () =
    (Unchecked.hash [|1;2|], Unchecked.hash [|1;2|]) ||> equal
    (Unchecked.hash [|2;1|], Unchecked.hash [|1;2|]) ||> notEqual

[<Fact>]
let ``Unchecked.hash with tuples works`` () =
    (Unchecked.hash (1,2), Unchecked.hash (1,2)) ||> equal
    (Unchecked.hash (2,1), Unchecked.hash (1,2)) ||> notEqual

[<Fact>]
let ``Unchecked.equals works`` () =
    Unchecked.equals 111 111 |> equal true
    Unchecked.equals 222 333 |> equal false
    Unchecked.equals "1" "1" |> equal true
    Unchecked.equals "2" "3" |> equal false
    Unchecked.equals [1] [1] |> equal true
    Unchecked.equals [2] [3] |> equal false

[<Fact>]
let ``Unchecked.compare works`` () =
    Unchecked.compare 111 111 |> equal 0
    Unchecked.compare 222 333 |> equal -1
    Unchecked.compare 333 222 |> equal 1
    Unchecked.compare "1" "1" |> equal 0
    Unchecked.compare "2" "3" |> equal -1
    Unchecked.compare "3" "2" |> equal 1
    Unchecked.compare [1] [1] |> equal 0
    Unchecked.compare [2] [3] |> equal -1
    Unchecked.compare [3] [2] |> equal 1

[<Fact>]
let ``DU comparison works`` () =
    let hasStatusReached expectedStatus status =
        status >= expectedStatus
    Status.CreateNewMeterReadingPicture >= Status.SelectingNewDevice
    |> equal true
    hasStatusReached Status.SelectingNewDevice Status.CreateNewMeterReadingPicture
    |> equal true

[<Fact>]
let ``LanguagePrimitives.GenericHash with primitives works`` () =
    (LanguagePrimitives.GenericHash 111, LanguagePrimitives.GenericHash 111) ||> equal
    (LanguagePrimitives.GenericHash 222, LanguagePrimitives.GenericHash 111) ||> notEqual
    (LanguagePrimitives.GenericHash "1", LanguagePrimitives.GenericHash "1") ||> equal
    (LanguagePrimitives.GenericHash "2", LanguagePrimitives.GenericHash "1") ||> notEqual

[<Fact>]
let ``LanguagePrimitives.GenericHash with lists works`` () =
    (LanguagePrimitives.GenericHash [1;2], LanguagePrimitives.GenericHash [1;2]) ||> equal
    (LanguagePrimitives.GenericHash [2;1], LanguagePrimitives.GenericHash [1;2]) ||> notEqual

[<Fact>]
let ``LanguagePrimitives.GenericHash with arrays works`` () =
    (LanguagePrimitives.GenericHash [|1;2|], LanguagePrimitives.GenericHash [|1;2|]) ||> equal
    (LanguagePrimitives.GenericHash [|2;1|], LanguagePrimitives.GenericHash [|1;2|]) ||> notEqual

[<Fact>]
let ``LanguagePrimitives.GenericHash with tuples works`` () =
    (LanguagePrimitives.GenericHash (1,2), LanguagePrimitives.GenericHash (1,2)) ||> equal
    (LanguagePrimitives.GenericHash (2,1), LanguagePrimitives.GenericHash (1,2)) ||> notEqual

[<Fact>]
let ``LanguagePrimitives.PhysicalHash with primitives works`` () =
    (LanguagePrimitives.PhysicalHash "1", LanguagePrimitives.PhysicalHash "1") ||> equal
    (LanguagePrimitives.PhysicalHash "2", LanguagePrimitives.PhysicalHash "1") ||> notEqual

[<Fact>]
let ``LanguagePrimitives.PhysicalHash with lists works`` () =
    let o1 = [1; 2]
    let o2 = [1; 2]
    let o3 = [2; 1]
    (LanguagePrimitives.PhysicalHash o1, LanguagePrimitives.PhysicalHash o1) ||> equal
    (LanguagePrimitives.PhysicalHash o2, LanguagePrimitives.PhysicalHash o1) ||> notEqual
    (LanguagePrimitives.PhysicalHash o3, LanguagePrimitives.PhysicalHash o1) ||> notEqual

[<Fact>]
let ``LanguagePrimitives.PhysicalHash with arrays works`` () =
    let o1 = [|1; 2|]
    let o2 = [|1; 2|]
    let o3 = [|2; 1|]
    (LanguagePrimitives.PhysicalHash o1, LanguagePrimitives.PhysicalHash o1) ||> equal
    (LanguagePrimitives.PhysicalHash o2, LanguagePrimitives.PhysicalHash o1) ||> notEqual
    (LanguagePrimitives.PhysicalHash o3, LanguagePrimitives.PhysicalHash o1) ||> notEqual

[<Fact>]
let ``LanguagePrimitives.PhysicalHash with tuples works`` () =
    let o1 = (1, 2)
    let o2 = (1, 2)
    let o3 = (2, 1)
    (LanguagePrimitives.PhysicalHash o1, LanguagePrimitives.PhysicalHash o1) ||> equal
    (LanguagePrimitives.PhysicalHash o2, LanguagePrimitives.PhysicalHash o1) ||> notEqual
    (LanguagePrimitives.PhysicalHash o3, LanguagePrimitives.PhysicalHash o1) ||> notEqual

[<Fact>]
let ``LanguagePrimitives.GenericComparison works`` () =
    LanguagePrimitives.GenericComparison 111 111 |> equal 0
    LanguagePrimitives.GenericComparison 222 333 |> equal -1
    LanguagePrimitives.GenericComparison 333 222 |> equal 1
    LanguagePrimitives.GenericComparison "1" "1" |> equal 0
    LanguagePrimitives.GenericComparison "2" "3" |> equal -1
    LanguagePrimitives.GenericComparison "3" "2" |> equal 1
    LanguagePrimitives.GenericComparison [1] [1] |> equal 0
    LanguagePrimitives.GenericComparison [2] [3] |> equal -1
    LanguagePrimitives.GenericComparison [3] [2] |> equal 1

[<Fact>]
let ``LanguagePrimitives.GenericEquality works`` () =
    LanguagePrimitives.GenericEquality 111 111 |> equal true
    LanguagePrimitives.GenericEquality 222 333 |> equal false
    LanguagePrimitives.GenericEquality "1" "1" |> equal true
    LanguagePrimitives.GenericEquality "2" "3" |> equal false
    LanguagePrimitives.GenericEquality [1] [1] |> equal true
    LanguagePrimitives.GenericEquality [2] [3] |> equal false

[<Fact>]
let ``LanguagePrimitives.PhysicalEquality works`` () =
    LanguagePrimitives.PhysicalEquality "1" "1" |> equal true
    LanguagePrimitives.PhysicalEquality "2" "3" |> equal false
    LanguagePrimitives.PhysicalEquality [1] [1] |> equal false
    LanguagePrimitives.PhysicalEquality [2] [3] |> equal false

[<Fact>]
let ``LanguagePrimitives.SByteWithMeasure works`` () =
    let distance: sbyte<m> = LanguagePrimitives.SByteWithMeasure 1y
    distance |> equal 1y<m>

[<Fact>]
let ``LanguagePrimitives.Int16WithMeasure works`` () =
    let distance: int16<m> = LanguagePrimitives.Int16WithMeasure 1s
    distance |> equal 1s<m>

[<Fact>]
let ``LanguagePrimitives.Int32WithMeasure works`` () =
    let distance: int<m> = LanguagePrimitives.Int32WithMeasure 1
    distance |> equal 1<m>

[<Fact>]
let ``LanguagePrimitives.Int64WithMeasure works`` () =
    let distance: int64<m> = LanguagePrimitives.Int64WithMeasure 1L
    distance |> equal 1L<m>

[<Fact>]
let ``LanguagePrimitives.Float32WithMeasure works`` () =
    let distance: float32<m> = LanguagePrimitives.Float32WithMeasure 1.0f
    distance |> equal 1.0f<m>

[<Fact>]
let ``LanguagePrimitives.FloatWithMeasure works`` () =
    let distance: float<m> = LanguagePrimitives.FloatWithMeasure 1.0
    distance |> equal 1.0<m>

[<Fact>]
let ``LanguagePrimitives.DecimalWithMeasure works`` () =
    let distance: decimal<m> = LanguagePrimitives.DecimalWithMeasure 1.0m
    distance |> equal 1.0m<m>

[<Fact>]
let ``EqualityComparer.Create works`` () =
    let cmp = EqualityComparer.Create((<>), hash)
    cmp.Equals(1, 1) |> equal false
    cmp.Equals(1, 2) |> equal true

[<Fact>]
let ``EqualityComparer.Equals works`` () =
    genericEquals 1 1 |> equal true
    genericEquals 1 2 |> equal false
    genericEquals "1" "1" |> equal true
    genericEquals "1" "2" |> equal false

[<Fact>]
let ``EqualityComparer.GetHashCode works`` () =
    genericHash 1 |> equal ((1).GetHashCode())
    genericHash "1" |> equal ("1".GetHashCode())

[<Fact>]
let ``Comparer.Compare works`` () =
    genericCompare 1 1 |> equal 0
    genericCompare 1 2 |> equal -1
    genericCompare 2 1 |> equal 1

[<Fact>]
let ``Comparer.Create works`` () =
    let cmp = Comparer.Create(fun x y -> -(compare x y))
    cmp.Compare(1, 1) |> equal 0
    cmp.Compare(1, 2) |> equal 1
    cmp.Compare(2, 1) |> equal -1
