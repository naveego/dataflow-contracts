# Datapoints <!-- omit in toc --> 
This document defines everything you need to know about data points.  

- [Is it one word or two?](#is-it-one-word-or-two)
- [Identity](#identity)
  - [`id`](#id)
  - [`rid`](#rid)
    - [Generating the `rid`](#generating-the-rid)
    - [How/Where are id's generated?](#howwhere-are-ids-generated)

Is it one word or two?
------------------------
Many hours of time have been lost over this discussion.  Finally we tried to document how we see it in [Things That Should Be Obvious, But Are Not](https://github.com/naveegoinc/developers/blob/master/docs/obvious_but_not.md).  Bottom line is that it is probably two words, but we find it very hard to put a capital 'P' in our variable names `dataPoint`. 

Identity
-------------------------
It is important that we can identify the datapoints as they flow through the system.  Since we are mutli-tenant it is also important that we provide tenant encapulation of the data.  There are two identities to be aware of when identifying a data point, which are the `id` and `rid`.

### `id`
Each and every datapoint **MUST** have a globally unique id.  This `id` identifies the data point, but it does not define the identity of the data it is transferring.  

### `rid`
The `rid` is used to uniquely identiy the source data that is being transferred by the datapoint.  While there should never be two datapoints with the same `id` it is common to have datapoints with the same `rid`.  The system uses the `rid` to establish the identity of the data from the source system.  The `rid` is a deterministic key based off the `tenantId`, `schemaId`, and the keys of the data.  This makes it globally unique.

#### Generating the `rid`
The `rid` is an [HMAC](https://en.wikipedia.org/wiki/HMAC) using the `SHA256` encryption algorithm. The formula generating the signing key and the payload use string representations encoded using `UTF-8`.

**The signing key**
The signing key is comprised of three parts all separated by `:`.  The local keys is a string representation of the keys inside of `[]`.  This is mostly because of the string representation of the `[]string` type in Go. To genreate the signing key use the following:

  - `tenantId` - The tenant's identifier (NOTE: The `tid` claim on the auth token)
  - `schemaId` - The id of the schema used to pull the data
  - local keys - A string array of the key values as identified by the schema
  
**Example**
```
// Format: {tenantId}:{schemaId}:[{local keys}]
// Values: tenantId = vandelay, schemaId = schema1, local Keys=[123 456]
vandelay:schema1:[123 456]
```

**The payload**
The payload is a string represntation of the key values as strings encoded as `UTF-8` and separated by `|`.  The payload string starts with a `|`, so be careful if you use a `join` function with a `|` separator, because those functions will only place the `|` between the items.

**Example**
```
// Golang keys: [123 456]
|123|456
```

**Golang Example**
```go
func NewRidComputer(tenant string, keys []string) RidComputer {
	localKeys := make([]string,len(keys), len(keys))
	copy(localKeys, keys)
	sort.Strings(localKeys)

	return RidComputer{
		signingKey: []byte(fmt.Sprintf("%s:%v",tenant, localKeys)),
		keyIDs:localKeys,
	}


}

func (r RidComputer) ComputeRid(record Record) string {
	h := hmac.New(sha256.New, r.signingKey)
	for _, key := range r.keyIDs {
		v := record[key]
		fmt.Fprintf(h, "|%v", v)
	}
	b := h.Sum(nil)
	k := base64.RawURLEncoding.EncodeToString(b)
	return k
}
```

**C# Example**
```csharp
var rid = "";
var keyValue = (string) record.SOURCE_REFERENCE_NUM;
var signingKey = $"{_tenantId}:{job.SchemaId}[{keyValue}]";
var signingKeyBytes = System.Text.Encoding.UTF8.GetBytes(signingKey);

using (var hmac = new HMACSHA256(signingKeyBytes))
{
    var ridBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"|{keyValue}"));
    rid = Base64UrlTextEncoder.Encode(ridBytes);
}

```

#### How/Where are id's generated?
We use [Xid]'s as our globally unique identifiers.  Identifiers are expected to be genreated on the client, and not the server.  