#!  "netcoreapp2.1"

#r "nuget: NJsonSchema, 9.12.0"
#r "nuget: MongoDb.Driver.signed, 2.7.0"
#r "nuget: NJsonSchema.CodeGeneration.CSharp, 9.12.0"
#r "nuget: Newtonsoft.Json, 11.0.2"

using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


var root = "events/swagger.json";

var schema = await JsonSchema4.FromFileAsync(root);

var json = schema.ToJson();

var jobject = JObject.Parse(json);

jobject["description"] = "This is the raw generated swagger based pulling together all the schemas. It is not a valid swagger.";

File.WriteAllText("events/swagger-generated-raw.json", jobject.ToString());

jobject.Property("description").Remove();

foreach (var t in jobject.SelectTokens("$..$id").ToList()) {
    t.Parent?.Remove();
}

foreach (var t in jobject.SelectTokens("$..$schema").ToList()) {
    t.Parent?.Remove();
}

var refTokens = jobject.SelectTokens("$..$ref").ToList();
var refs = refTokens
    .Select(x => x.ToString())
    .Distinct()
    .Where(x => x.Count(c => c == '/') > 2)
    .ToDictionary(t => t, t => {
        var jsonpath = t.ToString().Replace("#", "$").Replace("/", ".");
        var token = jobject.SelectToken(jsonpath);
        return token;
    })
;



var definitions = (JObject)jobject.Property("definitions").Value;

foreach (var item in refTokens)
{    
    var shortName = "#/definitions/" + item.ToString().Split("/definitions/").Last();
    Console.WriteLine(shortName);

    item.Replace(JToken.FromObject(shortName));
}

foreach (var kv in jobject.SelectTokens("$.definitions..definitions").SelectMany(x => x.Children())
.OfType<JProperty>()
.GroupBy(x => x.Name).Select(x => x.First())
    .ToDictionary(x => x.Name, x => x.Value)) {
    var shortName = kv.Key.ToString().Split("/definitions/").Last();
    definitions[shortName] = kv.Value;
}

var nestedDefinitions = jobject.SelectTokens("$.definitions..definitions").ToList();
foreach (var n in nestedDefinitions) {
    n.Parent.Remove();
}

foreach (var x in jobject.SelectTokens("$..discriminator").ToList()){
    Console.WriteLine("replacing discriminator", x);
    
    x.Replace(JToken.FromObject("typ"));
}

foreach (JValue x in jobject.SelectTokens("$..additionalProperties").OfType<JValue>().ToList()){
    if (x.Value<bool>() == false) {
        x.Parent.Remove();
    }    
}

foreach (JArray x in jobject.SelectTokens("$..enum").OfType<JArray>().ToList()){
    var property = x.Ancestors().OfType<JProperty>().First();

    var titledAncestors = property.Ancestors().OfType<JObject>().Where(p => p.Property("title") != null).Select(p => p.Property("title")).ToArray();
    string name = "";

    if(titledAncestors.Length == 0){
        continue;
    }

    if (titledAncestors.Length > 0) {
        name = titledAncestors[0].Value.ToString();
    }

    if (titledAncestors.Length > 1) {
        name = titledAncestors[1].Value.ToString() + name;
    }

    property.Parent["type"] = "string";
    property.AddAfterSelf(        
        new JProperty(
            "x-ms-enum",
            new JObject(
                new JProperty("name", name),
                new JProperty("modelAsString", true)
            )
        ));
}

foreach (var x in jobject.SelectTokens("$..oneOf").ToList()){
    Console.WriteLine("collapsing oneOf", x);
    var oneOfRef = x.SelectToken("$..$ref");
    x.Parent.Parent.Replace(new JObject(new JProperty("$ref", oneOfRef)));
}

foreach (var x in jobject.SelectTokens("$.definitions..title").ToList()){
   // Console.WriteLine("adding client side name for autorest, for title " + x.ToString());
    x.Parent.AddAfterSelf(new JProperty("x-ms-client-name", x.ToString()));
}

//Console.WriteLine(definitions.ToString());


File.WriteAllText("events/swagger-generated.json", jobject.ToString());


public string GetLabel(JToken token) {
    var o = token as JObject;
    if (o != null) {
        if (o["title"] != null) {
            return o["title"].Value<string>();
        }
        token = o.Parent;
    }

    var p = token as JProperty;
    if(p != null) {
        return p.Name;
    }

    throw new Exception("could not get label for " + token.ToString());    
}