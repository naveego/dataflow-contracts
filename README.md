# dataflow-contracts
JSONSchemas, protobuf definitions, OpenAPI files, etc for establishing shared contracts related to data flow.


# Data Point
Fill in docs


### RecordID (aka RID or rid)
The RecordID is a unique identifier associated with a datapoint when it is first obtained
from a source system. The RecordID is based on the tenant, schemaID, and the values of the
key(s) of the source record from the source schema (note: not the keys of the *shape* record,
the RecordID is computed before the record is mapped into a shape).

The algorithm for generating RecordIDs uses HMAC-SHA256:

To generate the signing key:

1. Sort the IDs of the schema's keys in alphabetical order.
2. Join the sorted IDs into a string, delimited by `:`.
3. Construct a string of `tenantID:schemaID:sortedJoinedKeys...`.

To generate the input to the hash:

1. Get the values of the keys, in alphabetical order *by key*.
2. Convert the values to strings using the default Go formatting rules (https://golang.org/pkg/fmt/). 
3. Create a string with the prefix `|` followed by the key values delimited by `|`.

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