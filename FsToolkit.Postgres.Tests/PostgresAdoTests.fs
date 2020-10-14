namespace FsToolkit.Postgres.Tests

open System
open NpgsqlTypes
open Swensen.Unquote
open NUnit.Framework
open Newtonsoft.Json.Linq
open FsToolkit.Postgres

module PostgresAdoTests =

    [<Test>]
    let ``P' basic`` () =
        let p = P'("x", 3)
        test <@ p.ParameterName = "x" @>
        test <@ p.NpgsqlValue = (3 :> obj) @>
        test <@ p.NpgsqlDbType = NpgsqlDbType.Integer @>

    [<Test>]
    let ``P' type scenarios`` () =
        let scenarios = [
            //string scenarios
            P'("x", "hello"), NpgsqlDbType.Text
            P'("x", (null:string)), NpgsqlDbType.Text

            //integer scenarios
            P'("x", 3), NpgsqlDbType.Integer
            P'("x", Some(3)), NpgsqlDbType.Integer
            P'("x", (None:int option)), NpgsqlDbType.Integer

            //decimal scenarios
            P'("x", 3.2m), NpgsqlDbType.Numeric
            P'("x", Some(3.2m)), NpgsqlDbType.Numeric
            P'("x", (None:decimal option)), NpgsqlDbType.Numeric

            //array scenarios
            P'("x", ["x"; "y"; "z"]), NpgsqlDbType.Array ||| NpgsqlDbType.Text
            P'("x", seq { "x"; "y"; "z" }), NpgsqlDbType.Array ||| NpgsqlDbType.Text
            P'("x", Set [ "x"; "y"; "z" ]), NpgsqlDbType.Array ||| NpgsqlDbType.Text
            P'("x", [| "x"; "y"; "z" |]), NpgsqlDbType.Array ||| NpgsqlDbType.Text
            P'("x", [1;2;3]), NpgsqlDbType.Array ||| NpgsqlDbType.Integer
        ]

        for actualDbParam, expectedDbType in scenarios do
            test <@ actualDbParam.NpgsqlDbType = expectedDbType @>
