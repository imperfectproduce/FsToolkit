namespace FsToolkit.Postgres
open System
open System.Data.Common
open Npgsql
open NpgsqlTypes

[<AutoOpen>]
module PostgresAdo =
    module Dynamic =
        let readOption (r : DbDataReader) (n : string) : 't option =
            let ordinal = r.GetOrdinal(n)
            if r.IsDBNull(ordinal) then
                None
            else
                let value = r.GetFieldValue<'t>(ordinal)
                if Object.ReferenceEquals(value, null) then
                    None
                else
                    Some(value)

        let readObj (r : DbDataReader) (n : string) : 't =
            let ordinal = r.GetOrdinal(n)
            if r.IsDBNull(ordinal) then
                null : 't
            else
                r.GetFieldValue<'t>(ordinal)

        let readValue (r : DbDataReader) (n : string) : 't =
            let ordinal = r.GetOrdinal(n)
            r.GetFieldValue<'t>(ordinal)

        let readDynamic (r : DbDataReader) (n : string) : 't =
            let tty = typeof<'t>
            if tty = typeof<string> then
                (readObj r n : string) :> obj :?> 't
            elif tty = typeof<Guid> then
                (readValue r n : Guid) :> obj :?> 't
            elif tty = typeof<Int32> then
                (readValue r n : Int32) :> obj :?> 't
            elif tty = typeof<Int64> then
                (readValue r n : Int64) :> obj :?> 't
            elif tty = typeof<decimal> then
                (readValue r n : decimal) :> obj :?> 't
            elif tty = typeof<bool> then
                (readValue r n : bool) :> obj :?> 't
            elif tty = typeof<DateTime> then
                (readValue r n : DateTime) :> obj :?> 't
            elif tty = typeof<DateTimeOffset> then
                (readValue r n : DateTimeOffset) :> obj :?> 't
            elif tty = typeof<string option> then
                (readOption r n : string option) :> obj :?> 't
            elif tty = typeof<Guid option> then
                (readOption r n : Guid option) :> obj :?> 't
            elif tty = typeof<Int32 option> then
                (readOption r n : Int32 option) :> obj :?> 't
            elif tty = typeof<Int64 option> then
                (readOption r n : Int64 option) :> obj :?> 't
            elif tty = typeof<decimal option> then
                (readOption r n : decimal option) :> obj :?> 't
            elif tty = typeof<bool option> then
                (readOption r n : bool option) :> obj :?> 't
            elif tty = typeof<DateTime option> then
                (readOption r n : DateTime option) :> obj :?> 't
            elif tty = typeof<DateTimeOffset option> then
                (readOption r n : DateTimeOffset option) :> obj :?> 't
            else
                failwithf "Unexpected type encountered when reading DbDataReader value: %A" tty

        ///Dynamically read column value by name from an DbDataReader converting the value
        ///from the NpgsqlDbType to the generic argument type. Handles nulls and option types intuitively.
        let inline (?) (r : DbDataReader) (n : string) : 't =
            readDynamic r n

    ///Build a query param
    let inline P (name:string,ty:NpgsqlDbType,value:'a) =
        let np = new NpgsqlParameter(name, ty)
        np.Value <- value :> obj
        np

    ///Build a query param with NpgsqlDbType automatically detected and nulls,
    ///option, and sequence types automatically handled.
    let P' (name:string, value: 'a) =
        let aty = typeof<'a>
        let value = value :> obj
        match value with
        // nulls
        | null when aty = typeof<string> ->
            P(name, NpgsqlDbType.Text, DBNull.Value)
        //None options (note: for some reason, patterns like `:? option<Guid> as v when v.IsNone` do not work)
        | null when aty = typeof<option<string>> -> P(name, NpgsqlDbType.Text, DBNull.Value)
        | null when aty = typeof<option<Guid>> -> P(name, NpgsqlDbType.Uuid, DBNull.Value)
        | null when aty = typeof<option<Int32>> -> P(name, NpgsqlDbType.Integer, DBNull.Value)
        | null when aty = typeof<option<Int64>> -> P(name, NpgsqlDbType.Bigint, DBNull.Value)
        | null when aty = typeof<option<decimal>> -> P(name, NpgsqlDbType.Numeric, DBNull.Value)
        | null when aty = typeof<option<bool>> -> P(name, NpgsqlDbType.Boolean, DBNull.Value)
        | null when aty = typeof<option<DateTime>> -> P(name, NpgsqlDbType.Timestamp, DBNull.Value)
        | null when aty = typeof<option<DateTimeOffset>> -> P(name, NpgsqlDbType.TimestampTz, DBNull.Value)
        //atoms
        | :? string as v ->  P(name, NpgsqlDbType.Text, v)
        | :? Guid as v -> P(name, NpgsqlDbType.Uuid, v)
        | :? Int32 as v -> P(name, NpgsqlDbType.Integer, v)
        | :? Int64 as v -> P(name, NpgsqlDbType.Bigint, v)
        | :? decimal as v -> P(name, NpgsqlDbType.Numeric, v)
        | :? bool as v -> P(name, NpgsqlDbType.Boolean, v)
        | :? DateTime as v -> P(name, NpgsqlDbType.Timestamp, v)
        | :? DateTimeOffset as v -> P(name, NpgsqlDbType.TimestampTz, v)
        //Some options
        | :? option<string> as v when v.Value = null -> P(name, NpgsqlDbType.Text, DBNull.Value)
        | :? option<string> as v -> P(name, NpgsqlDbType.Text, v.Value)
        | :? option<Guid> as v -> P(name, NpgsqlDbType.Uuid, v.Value)
        | :? option<Int32> as v -> P(name, NpgsqlDbType.Integer, v.Value)
        | :? option<Int64> as v -> P(name, NpgsqlDbType.Bigint, v.Value)
        | :? option<decimal> as v -> P(name, NpgsqlDbType.Numeric, v.Value)
        | :? option<bool> as v -> P(name, NpgsqlDbType.Boolean, v.Value)
        | :? option<DateTime> as v -> P(name, NpgsqlDbType.Timestamp, v.Value)
        | :? option<DateTimeOffset> as v -> P(name, NpgsqlDbType.TimestampTz, v.Value)
        //array-like
        //note: null string values in arrays _are_ properly handled
        | :? seq<string> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Text, v |> Seq.toArray)
        | :? seq<Guid> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Uuid, v |> Seq.toArray)
        | :? seq<Int32> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Integer, v |> Seq.toArray)
        | :? seq<Int64> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Bigint, v |> Seq.toArray)
        | :? seq<decimal> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Numeric, v |> Seq.toArray)
        | :? seq<bool> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Boolean, v |> Seq.toArray)
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


    ///read rows from an open data reader to completion
    let private readRowsAsync (reader: DbDataReader) read = async {
        let rec loop rows = async {
            let! hasRow = reader.ReadAsync() |> Async.AwaitTask
            if hasRow then
                return! loop ((read reader)::rows)
            else
                return rows |> List.rev
        }
        return! loop []
    }

    ///Execute a select query asynchronously
    let execQueryAsync (openConn:NpgsqlConnection) sql ps read = async {
        //see https://docs.microsoft.com/en-us/archive/blogs/adonet/using-sqldatareaders-new-async-methods-in-net-4-5
        //for guidance on which async methods to use
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
        return! readRowsAsync reader read
    }

    ///Execute a select query, returning None if no results
    let execQueryOption (openConn:NpgsqlConnection) sql ps read =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        use reader = cmd.ExecuteReader()
        if reader.HasRows
        then Some([ while reader.Read() do yield read reader ])
        else None

    ///Execute a select query asynchronously, returning None if no results
    let execQueryOptionAsync (openConn:NpgsqlConnection) sql ps read = async {
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask
        let! rows = readRowsAsync reader read
        return
            match rows with
            | [] -> None
            | _ -> Some rows
    }

    ///Execute an insert or update statement and return the number of rows affected
    let execNonQuery (openConn:NpgsqlConnection) sql ps =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        cmd.ExecuteNonQuery()

    ///Execute an insert or update statement and return the number of rows affected asynchronsouly
    let execNonQueryAsync (openConn:NpgsqlConnection) sql ps = async {
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        return! cmd.ExecuteNonQueryAsync() |> Async.AwaitTask
    }

    ///Execute query returning first column from first row of result set
    let execScalar (openConn:NpgsqlConnection) sql ps =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        cmd.ExecuteScalar()

    ///Execute query asynchronously returning first column from first row of result set
    let execScalarAsync (openConn:NpgsqlConnection) sql ps = async {
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        return! cmd.ExecuteScalarAsync() |> Async.AwaitTask
    }

    ///Execute query returning first column from first row of result set
    ///Returns None if the result is null or DBNull.
    let execScalarOption (openConn:NpgsqlConnection) sql ps =
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        let result = cmd.ExecuteScalar()
        if result = null || result.GetType() = typeof<DBNull>
        then None
        else Some(result)

    ///Execute query asynchronously returning first column from first row of result set.
    ///Returns None if the result is null or DBNull.
    let execScalarOptionAsync (openConn:NpgsqlConnection) sql ps = async {
        use cmd = openConn.CreateCommand()
        cmd.CommandText <- sql
        for p in ps do
            cmd.Parameters.Add(p) |> ignore
        let! result = cmd.ExecuteScalarAsync() |> Async.AwaitTask
        return
            if result = null || result.GetType() = typeof<DBNull>
            then None
            else Some(result)
    }

    let read1<'a> (reader:DbDataReader) =
        reader.GetFieldValue<'a>(0)

    let read2<'a,'b> (reader:DbDataReader) =
        reader.GetFieldValue<'a>(0),
        reader.GetFieldValue<'b>(1)

    let read3<'a,'b,'c> (reader:DbDataReader) =
        reader.GetFieldValue<'a>(0),
        reader.GetFieldValue<'b>(1),
        reader.GetFieldValue<'c>(2)

    let read4<'a,'b,'c,'d> (reader:DbDataReader) =
        reader.GetFieldValue<'a>(0),
        reader.GetFieldValue<'b>(1),
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3)

    let read5<'a,'b,'c,'d,'e> (reader:DbDataReader) =
        reader.GetFieldValue<'a>(0),
        reader.GetFieldValue<'b>(1),
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3),
        reader.GetFieldValue<'e>(4)

    let read6<'a,'b,'c,'d,'e,'f> (reader:DbDataReader) =
        reader.GetFieldValue<'a>(0),
        reader.GetFieldValue<'b>(1),
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3),
        reader.GetFieldValue<'e>(4),
        reader.GetFieldValue<'f>(5)

    let read7<'a,'b,'c,'d,'e,'f,'g> (reader:DbDataReader) =
        reader.GetFieldValue<'a>(0),
        reader.GetFieldValue<'b>(1),
        reader.GetFieldValue<'c>(2),
        reader.GetFieldValue<'d>(3),
        reader.GetFieldValue<'e>(4),
        reader.GetFieldValue<'f>(5),
        reader.GetFieldValue<'g>(6)

    ///Execute the given function with an opened connection which is disposed after completion
    let doWithOpenConn (getConn:unit -> NpgsqlConnection) f =
        use conn = getConn ()
        conn.Open()
        f conn

    ///Execute the given function with an asynchronously opened connection which is disposed after completion
    let doWithOpenConnAsync (getConn:unit -> NpgsqlConnection) f = async {
        use conn = getConn ()
        do! conn.OpenAsync() |> Async.AwaitTask
        let! result = f conn
        do! conn.CloseAsync() |> Async.AwaitTask
        return result
    }
