namespace PerfMonads.Tests;

using System.Text.Json;
using Perf.Monads.Result;
using Xunit.Abstractions;

public class Json_Testing(ITestOutputHelper print) {
    static readonly JsonSerializerOptions JsonOptions = new() {
        Converters = { MonadResultJsonConverter<int, string>.Instance }
    };

    [Fact]
    public void Ok_Serialization() {
        var json = """{"ok":10}""";
        Result<int, string> r = 10;
        var json2 = JsonSerializer.Serialize(r, JsonOptions);

        Assert.Equal(json, json2);

        var r2 = JsonSerializer.Deserialize<Result<int, string>>(json, JsonOptions);

        Assert.Equal(r, r2);
    }

    [Fact]
    public void Error_Serialization() {
        var json = """{"error":"10"}""";
        Result<int, string> r = "10";
        var json2 = JsonSerializer.Serialize(r, JsonOptions);

        Assert.Equal(json, json2);

        var r2 = JsonSerializer.Deserialize<Result<int, string>>(json, JsonOptions);

        Assert.Equal(r, r2);
    }

    [Fact]
    public void Ok_Serialization_Error() {
        var json = """{"ok":"10"}""";

        var e = Assert.Throws<JsonException>(
            () => {
                var r = JsonSerializer.Deserialize<Result<int, string>>(json, JsonOptions);
            }
        );

        print.WriteLine(e.Message);

        var json2 = """{"ok2":10}""";
        var e2 = Assert.Throws<JsonException>(
            () => {
                var r = JsonSerializer.Deserialize<Result<int, string>>(json2, JsonOptions);
            }
        );
        
        print.WriteLine(e2.Message);
    }
}
