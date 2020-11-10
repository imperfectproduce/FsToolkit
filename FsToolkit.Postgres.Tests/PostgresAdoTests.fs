namespace FsToolkit.Postgres.Tests

open System
open NpgsqlTypes
open Swensen.Unquote
open NUnit.Framework
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
            P'("x", "hello"), (NpgsqlDbType.Text, "hello" :> obj)
            P'("x", (null:string)), (NpgsqlDbType.Text, DBNull.Value :> obj)
            P'("x", (None:string option)), (NpgsqlDbType.Text, DBNull.Value :> obj)
            P'("x", (Some(null):string option)), (NpgsqlDbType.Text, DBNull.Value :> obj)

            //integer scenarios
            P'("x", 3), (NpgsqlDbType.Integer, 3 :> obj)
            P'("x", Some(3)), (NpgsqlDbType.Integer, 3 :> obj)
            P'("x", (None:int option)), (NpgsqlDbType.Integer, DBNull.Value :> obj)

            //int64 scenarios
            P'("x", 3L), (NpgsqlDbType.Bigint, 3L :> obj)
            P'("x", Some(3L)), (NpgsqlDbType.Bigint, 3L :> obj)
            P'("x", (None:int64 option)), (NpgsqlDbType.Bigint, DBNull.Value :> obj)

            //decimal scenarios
            P'("x", 3.2m), (NpgsqlDbType.Numeric, 3.2m :> obj)
            P'("x", Some(3.2m)), (NpgsqlDbType.Numeric, 3.2m :> obj)
            P'("x", (None:decimal option)), (NpgsqlDbType.Numeric, DBNull.Value :> obj)

            //array scenarios
            let xyzArray = [|"x"; "y"; "z"|] :> obj
            P'("x", ["x"; "y"; "z"]), (NpgsqlDbType.Array ||| NpgsqlDbType.Text, xyzArray)
            P'("x", seq { "x"; "y"; "z" }), (NpgsqlDbType.Array ||| NpgsqlDbType.Text, xyzArray)
            P'("x", Set [ "x"; "y"; "z" ]), (NpgsqlDbType.Array ||| NpgsqlDbType.Text, xyzArray)
            P'("x", [| "x"; "y"; "z" |]), (NpgsqlDbType.Array ||| NpgsqlDbType.Text, xyzArray)
            P'("x", [1;2;3]), (NpgsqlDbType.Array ||| NpgsqlDbType.Integer, [|1;2;3|] :> obj)
        ]

        for actualDbParam, (expectedDbType, expectedDbValue) in scenarios do
            test <@ actualDbParam.NpgsqlDbType = expectedDbType @>
            test <@ actualDbParam.NpgsqlValue = expectedDbValue @>
