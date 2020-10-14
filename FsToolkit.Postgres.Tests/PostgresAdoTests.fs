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
        
        
