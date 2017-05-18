namespace FsToolkit

open System.Collections.Generic

type InsertionDictionary<'K, 'V when 'K : equality and 'V : equality>() = 
    let dictionary = new System.Collections.Specialized.OrderedDictionary()
    let entries = 
        let keys = seq { for k in dictionary.Keys -> k :?> 'K }
        let values = seq { for v in dictionary.Values -> v :?> 'V }
        Seq.zip keys values

    interface IDictionary<'K,'V> with
        member this.Add(key, value) = dictionary.Add(key,value)
        member this.Add(kvp) = dictionary.Add(kvp.Key, kvp.Value)
        member this.ContainsKey(key) = dictionary.Contains(key)
        member this.Contains(kvp) = entries |> Seq.exists (fun (k,v) -> kvp.Key = k && kvp.Value = v)
        member this.get_Item(key: 'K) = dictionary.Item(key :> obj) :?> 'V
        member this.set_Item(key: 'K, value: 'V) = dictionary.Item(key :> obj) <- value
        member this.Count with get() = dictionary.Count
        member this.IsReadOnly with get() = false
        member this.Keys = ResizeArray<'K>(entries |> Seq.map fst) :> ICollection<'K>
        member this.Remove(key: 'K) = 
            if dictionary.Contains(key) then
                dictionary.Remove(key)
                true
            else
                false
        member this.Remove(kvp : KeyValuePair<'K,'V>) = 
            if dictionary.Contains(kvp.Key) then
                dictionary.Remove(kvp.Key)
                true
            else
                false
        member this.TryGetValue(key:'K, value:byref<'V>) = 
            if dictionary.Contains(key) then
                value <- dictionary.Item(key :> obj) :?> 'V
                true
            else
                false
        member this.Values = ResizeArray<'V>(entries |> Seq.map snd) :> ICollection<'V>
        member this.Clear() = dictionary.Clear()
        member this.CopyTo(array, arrayIndex) = dictionary.CopyTo(array, arrayIndex)
        member this.GetEnumerator() = entries.GetEnumerator() :> System.Collections.IEnumerator
        member this.GetEnumerator() = (entries |> Seq.map KeyValuePair).GetEnumerator() :> IEnumerator<KeyValuePair<'K,'V>>

