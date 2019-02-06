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

The RecordID is a unique identifier associated with a datapoint when it is first obtained
from a source system. The RecordID is based on the tenant, schemaID, and the values of the
key(s) of the source record from the source schema (note: not the keys of the *shape* record,
the RecordID is computed before the record is mapped into a shape).

The algorithm for generating RecordIDs uses HMAC-SHA256:

- To generate the signing key:

	1. Sort the IDs of the schema's keys in alphabetical order.
	2. Join the sorted IDs into a string, delimited by `:`.
	3. Construct a string of `tenantID:schemaID:sortedJoinedKeys...`.

	**Example**
	```
	// format: {tenantId}:{schemaId}:{local keys}
	// input: tenantId = vandelay, schemaId = schema1, keys=[keyB keyA]
	// output: vandelay:schema1:keyA:keyB
	```
- To generate the input to the hash:

	1. Get the values of the keys, in alphabetical order *by key*.
	2. Convert the values to strings using the default Go formatting rules (https://golang.org/pkg/fmt/). 
	3. Create a string with the prefix `|` followed by the key values delimited by `|`.

	**Example**
	```
	// input: { "keyA": 123, "keyB": "xyz", "value": "some value" }
	// output: |123|xyz
	```

To generate the RecordID from the hash:

1. Base64 encode the hashed bytes using the Raw URL encoding protocol, using `-`, `_`' and no `=` padding characters. (https://tools.ietf.org/html/rfc4648#section-5)

Here is the Go implementation:

```go
import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/base64"
	"fmt"
	"sort"
	"strings"
)

type RidComputer struct {
	keyIDs []string
	signingKey []byte
}

func NewRidComputer(tenantID, schemaID string, keys []string) RidComputer {
	sortedKeys := make([]string,len(keys), len(keys))
	copy(sortedKeys, keys)
	sort.Strings(sortedKeys)
	joinedSortedKeys := strings.Join(sortedKeys, ":")
	return RidComputer{
		signingKey: []byte(fmt.Sprintf("%s:%s:%v", tenantID, schemaID, joinedSortedKeys)),
		keyIDs:     sortedKeys,
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
// note that this only handles the case of a single key
var rid = "";
var keyValue = (string) record.SOURCE_REFERENCE_NUM;
var signingKey = $"{_tenantId}:{job.SchemaId}:{keyValue}";
var signingKeyBytes = System.Text.Encoding.UTF8.GetBytes(signingKey);

using (var hmac = new HMACSHA256(signingKeyBytes))
{
    var ridBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"|{keyValue}"));
    rid = Base64UrlTextEncoder.Encode(ridBytes);
}

```

#### How/Where are id's generated?
We use [Xid](https://github.com/rs/xid)'s as our globally unique identifiers.  Identifiers are expected to be genreated on the client, and not the server.  