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

    ///Build a query param with NpgsqlSqlDbType automatically detected and nulls and
    ///option types automatically handled.
    let P' (name:string, value: 'a) =
        let value = value :> obj
        match value with
        //atoms
        | :? string as v when v = null -> P(name, NpgsqlDbType.Text, DBNull.Value)
        | :? string as v ->  P(name, NpgsqlDbType.Text, v)
        | :? Guid as v -> P(name, NpgsqlDbType.Uuid, v)
        | :? Int32 as v -> P(name, NpgsqlDbType.Integer, v)
        | :? decimal as v -> P(name, NpgsqlDbType.Numeric, v)
        | :? DateTime as v -> P(name, NpgsqlDbType.Timestamp, v)
        | :? DateTimeOffset as v -> P(name, NpgsqlDbType.TimestampTz, v)
        //None options
        | :? option<string> as v when v.IsNone -> P(name, NpgsqlDbType.Text, DBNull.Value)
        | :? option<Guid> as v when v.IsNone -> P(name, NpgsqlDbType.Uuid, DBNull.Value)
        | :? option<Int32> as v when v.IsNone -> P(name, NpgsqlDbType.Integer, DBNull.Value)
        | :? option<decimal> as v when v.IsNone -> P(name, NpgsqlDbType.Numeric, DBNull.Value)
        | :? option<DateTime> as v when v.IsNone -> P(name, NpgsqlDbType.Timestamp, DBNull.Value)
        | :? option<DateTimeOffset> as v when v.IsNone -> P(name, NpgsqlDbType.TimestampTz, DBNull.Value)
        //Some options
        | :? option<string> as v when v.Value = null -> P(name, NpgsqlDbType.Text, DBNull.Value)
        | :? option<string> as v -> P(name, NpgsqlDbType.Text, v.Value)
        | :? option<Guid> as v -> P(name, NpgsqlDbType.Uuid, v.Value)
        | :? option<Int32> as v -> P(name, NpgsqlDbType.Integer, v.Value)
        | :? option<decimal> as v -> P(name, NpgsqlDbType.Numeric, v.Value)
        | :? option<DateTime> as v -> P(name, NpgsqlDbType.Timestamp, v.Value)
        | :? option<DateTimeOffset> as v -> P(name, NpgsqlDbType.TimestampTz, v.Value)
        //array-like
        | :? seq<string> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Text, v |> Seq.toArray)
        | :? seq<Guid> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Uuid, v |> Seq.toArray)
        | :? seq<Int32> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Integer, v |> Seq.toArray)
        | :? seq<decimal> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Numeric, v |> Seq.toArray)
        | :? seq<DateTime> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Timestamp, v |> Seq.toArray)
        | :? seq<DateTimeOffset> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.TimestampTz, v |> Seq.toArray)
        | v -> failwithf "Could not convert value to NpgsqlParameter: %A" v

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
