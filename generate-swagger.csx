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


File.WriteAllText("events/swagger-generated.json", schema.ToJson());

