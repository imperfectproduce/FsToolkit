namespace FsToolkit.Postgres
open System
open Npgsql
open NpgsqlTypes

[<AutoOpen>]
module PostgresAdo =
    module Dynamic =
        let readOption (r : NpgsqlDataReader) (n : string) : 't option =
            let ordinal = r.GetOrdinal(n)
            if r.IsDBNull(ordinal) then
                None
            else
                let value = r.GetFieldValue<'t>(ordinal)
                if Object.ReferenceEquals(value, null) then
                    None
                else
                    Some(value)

        let readObj (r : NpgsqlDataReader) (n : string) : 't =
            let ordinal = r.GetOrdinal(n)
            if r.IsDBNull(ordinal) then
                null : 't
            else
                r.GetFieldValue<'t>(ordinal)

        let readValue (r : NpgsqlDataReader) (n : string) : 't =
            let ordinal = r.GetOrdinal(n)
            r.GetFieldValue<'t>(ordinal)

        let readDynamic (r : NpgsqlDataReader) (n : string) : 't =
            let tty = typeof<'t>
            if tty = typeof<string> then
                (readObj r n : string) :> obj :?> 't
            elif tty = typeof<Guid> then
                (readValue r n : Guid) :> obj :?> 't
            elif tty = typeof<Int32> then
                (readValue r n : Int32) :> obj :?> 't
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
            elif tty = typeof<decimal option> then
                (readOption r n : decimal option) :> obj :?> 't
            elif tty = typeof<bool option> then
                (readOption r n : bool option) :> obj :?> 't
            elif tty = typeof<DateTime option> then
                (readOption r n : DateTime option) :> obj :?> 't
            elif tty = typeof<DateTimeOffset option> then
                (readOption r n : DateTimeOffset option) :> obj :?> 't
            else
                failwithf "Unexpected type encountered when reading NpgsqlDataReader value: %A" tty

        let inline (?) (r : NpgsqlDataReader) (n : string) : 't =
            readDynamic r n

    ///Build a query param
    let inline P (name:string,ty:NpgsqlDbType,value:'a) =
        let np = new NpgsqlParameter(name, ty)
        np.Value <- value :> obj
        np

    ///Build a query param with NpgsqlSqlDbType automatically detected and nulls and
    ///option types automatically handled.
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
        | null when aty = typeof<option<decimal>> -> P(name, NpgsqlDbType.Numeric, DBNull.Value)
        | null when aty = typeof<option<bool>> -> P(name, NpgsqlDbType.Boolean, DBNull.Value)
        | null when aty = typeof<option<DateTime>> -> P(name, NpgsqlDbType.Timestamp, DBNull.Value)
        | null when aty = typeof<option<DateTimeOffset>> -> P(name, NpgsqlDbType.TimestampTz, DBNull.Value)
        //atoms
        | :? string as v ->  P(name, NpgsqlDbType.Text, v)
        | :? Guid as v -> P(name, NpgsqlDbType.Uuid, v)
        | :? Int32 as v -> P(name, NpgsqlDbType.Integer, v)
        | :? decimal as v -> P(name, NpgsqlDbType.Numeric, v)
        | :? bool as v -> P(name, NpgsqlDbType.Boolean, v)
        | :? DateTime as v -> P(name, NpgsqlDbType.Timestamp, v)
        | :? DateTimeOffset as v -> P(name, NpgsqlDbType.TimestampTz, v)
        //Some options
        | :? option<string> as v when v.Value = null -> P(name, NpgsqlDbType.Text, DBNull.Value)
        | :? option<string> as v -> P(name, NpgsqlDbType.Text, v.Value)
        | :? option<Guid> as v -> P(name, NpgsqlDbType.Uuid, v.Value)
        | :? option<Int32> as v -> P(name, NpgsqlDbType.Integer, v.Value)
        | :? option<decimal> as v -> P(name, NpgsqlDbType.Numeric, v.Value)
        | :? option<bool> as v -> P(name, NpgsqlDbType.Boolean, v.Value)
        | :? option<DateTime> as v -> P(name, NpgsqlDbType.Timestamp, v.Value)
        | :? option<DateTimeOffset> as v -> P(name, NpgsqlDbType.TimestampTz, v.Value)
        //array-like
        //TODO Q: if we have a seq of strings that contains nulls, do we need to map to DbNull.Value?
        | :? seq<string> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Text, v |> Seq.toArray)
        | :? seq<Guid> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Uuid, v |> Seq.toArray)
        | :? seq<Int32> as v -> P(name, NpgsqlDbType.Array ||| NpgsqlDbType.Integer, v |> Seq.toArray)
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
        if result = null || result.GetType() = typeof<DBNull>
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
