namespace FsToolkit.Postgres
open System
open Npgsql
open NpgsqlTypes

[<AutoOpen>]
module PostgresAdo =
    ///Build a query param
    let inline P (name:string,ty:NpgsqlDbType,value:'a) =
        let np = new NpgsqlParameter(name, ty)
        np.Value <- value :> obj
        np
        
    ///Execute a select query
    let execQuery (openConn:NpgsqlConnection) sql ps read =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        use reader = cmd.ExecuteReader()
        [ while reader.Read() do yield read reader ]

    ///Execute a select query
    let execQueryOption (openConn:NpgsqlConnection) sql ps read =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        use reader = cmd.ExecuteReader()
        if reader.HasRows
        then Some([ while reader.Read() do yield read reader ])
        else None

    ///Execute an insert or update statement and return the number of rows affected
    let execNonQuery (openConn:NpgsqlConnection) sql ps =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        cmd.ExecuteNonQuery()

    ///Execute query returning first column from first row of result set
    let execScalar (openConn:NpgsqlConnection) sql ps =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        cmd.ExecuteScalar()

    ///Execute query returning first column from first row of result set
    let execScalarOption (openConn:NpgsqlConnection) sql ps =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        let result = cmd.ExecuteScalar()
        if result = null
        then None
        else Some(result)

    let read1<'a> (reader:NpgsqlDataReader) =
        reader.GetFieldValue<'a>(0)

    let read2<'a,'b> (reader:NpgsqlDataReader) =
        reader.GetFieldValue<'a>(0), 
        reader.GetFieldValue<'b>(1)

    let read3<'a,'b,'c> (reader:NpgsqlDataReader) =
        reader.GetFieldValue<'a>(0), 
        reader.GetFieldValue<'b>(1), 
        reader.GetFieldValue<'c>(2)

    let read4<'a,'b,'c,'d> (reader:NpgsqlDataReader) =
        reader.GetFieldValue<'a>(0), 
        reader.GetFieldValue<'b>(1), 
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3)

    let read5<'a,'b,'c,'d,'e> (reader:NpgsqlDataReader) =
        reader.GetFieldValue<'a>(0), 
        reader.GetFieldValue<'b>(1), 
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3),
        reader.GetFieldValue<'e>(4)

    let read6<'a,'b,'c,'d,'e,'f> (reader:NpgsqlDataReader) =
        reader.GetFieldValue<'a>(0), 
        reader.GetFieldValue<'b>(1), 
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3),
        reader.GetFieldValue<'e>(4),
        reader.GetFieldValue<'f>(5)

    let read7<'a,'b,'c,'d,'e,'f,'g> (reader:NpgsqlDataReader) =
        reader.GetFieldValue<'a>(0), 
        reader.GetFieldValue<'b>(1), 
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3),
        reader.GetFieldValue<'e>(4),
        reader.GetFieldValue<'f>(5),
        reader.GetFieldValue<'g>(6)

    ///Execute the given function with an open connection
    let doWithOpenConn (getConn:unit -> NpgsqlConnection) f =
        use conn = getConn ()
        conn.Open()
        f conn
