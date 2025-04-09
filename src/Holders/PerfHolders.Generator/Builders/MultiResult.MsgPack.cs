namespace Perf.Holders.Generator.Builders;

using Internal;
using Types;

sealed class MultiResultMessagePackSourceBuilder(MultiResultHolderContextInfo context) {
    const string Exceptions = "global::Perf.Holders.Exceptions.MultiResultHolderExceptions";
    readonly InterpolatedStringBuilder sb = new(stringBuilder: new(8000));

    readonly CompInfo compInfo = context.CompInfo;
    int bracesToCloseOnEnd;

    void Preparation() {
        if (compInfo.SupportNullableAnnotation() is false) {
            var els = context.Elements;
            for (var i = 0; i < els.Count; i++) {
                var el = els[i];
                if (el.IsStruct is false) {
                    els[i] = el with {
                        TypeNullable = el.Type
                    };
                }
            }
        }
    }

    public string WriteAllAndBuild() {
        Preparation();
        DeclareTopLevelStatements();
        WriteMessagePackFormatter();
        // WriteEndOfType();
        WriteEndOfFile();
        return sb.ToString();
    }

    void DeclareTopLevelStatements() {
        sb.AppendLine("// <auto-generated />");
        if (compInfo.SupportNullableAnnotation()) {
            sb.AppendLine("#nullable enable");
        }

        if (compInfo.SupportFileScopedNamespace()) {
            sb.AppendLine("namespace Perf.Holders.Serialization.MessagePack;");
        } else {
            sb.AppendLine("namespace Perf.Holders.Serialization.MessagePack\n{");
            bracesToCloseOnEnd++;
        }
    }

    void WriteMessagePackFormatter() {
        if (context.MultiResult.TypeParameterCount > 0) {
            return;
        }

        var accessibility = context.MultiResult.Accessibility is TypeAccessibility.Public ? "public " : "";
        const string msgPack = "global::MessagePack";
        sb.AppendInterpolatedLine(
            $$"""
            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
            {{accessibility}}sealed class MessagePackFormatter_{{context.MultiResult.OnlyName}} : {{msgPack}}.Formatters.IMessagePackFormatter<{{context.MultiResult.GlobalName}}>
            {
                public static readonly MessagePackFormatter_{{context.MultiResult.OnlyName}} Instance = new();
            """
        );
        sb.Indent++;
        sb.AppendInterpolatedLine(
            $"public void Serialize(ref {msgPack}.MessagePackWriter writer, {context.MultiResult.GlobalName} value, {msgPack}.MessagePackSerializerOptions options)"
        );
        sb.AppendLine("{");
        sb.Indent++;
        sb.AppendLine("writer.WriteMapHeader(1);");

        if (context.Configuration.OpenState is true) {
            sb.AppendLine("var state = (byte)value.State;");
        } else if (context.Configuration.AddIsProperties is true) {
            sb.Append("byte state = ");
            foreach (var el in context.Elements) {
                sb.AppendInterpolated($"value.{el.StateCheck.Property} ? (byte){el.Index + 1} : ");
            }

            sb.AppendLine("(byte)0;");
        }

        sb.AppendInterpolatedLine($"writer.WriteUInt8(state);");
        sb.AppendLine("switch(state) {");
        sb.Indent++;
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"case {el.Index + 1}:");
            sb.Indent++;
            sb.AppendInterpolatedLine($"{msgPack}.MessagePackSerializer.Serialize(ref writer, value.{el.Property}, options);");
            sb.Indent--;
            sb.AppendLine("break;");
        }

        sb.AppendInterpolatedLine($"default: throw {Exceptions}.Default<{context.MultiResult.GlobalName}>();");
        sb.Indent--;
        sb.AppendLine("}");
        sb.AppendLine();
        sb.Indent--;
        sb.AppendLine("}");

        sb.AppendInterpolatedLine(
            $"public {context.MultiResult.GlobalName} Deserialize(ref {msgPack}.MessagePackReader reader, {msgPack}.MessagePackSerializerOptions options)"
        );
        sb.AppendLine("{");
        sb.Indent++;
        sb.AppendInterpolatedLine(
            $$"""
            if (reader.IsNil || reader.TryReadMapHeader(out var mapHeader) == false) {
                throw new {{msgPack}}.MessagePackSerializationException($"Expected '{({{msgPack}}.MessagePackType.Map)}' but got '{reader.NextMessagePackType}'");
            }

            if (mapHeader is not 1) {
                throw new {{msgPack}}.MessagePackSerializationException($"Expected map header 1 but got '{mapHeader}'");
            }

            var key = reader.ReadByte();
            switch (key) {
            """
        );
        sb.Indent++;
        foreach (var el in context.Elements) {
            sb.AppendInterpolatedLine($"case {el.Index + 1}: return {msgPack}.MessagePackSerializer.Deserialize<{el.Type}>(ref reader, options);");
        }

        sb.AppendLine($"default: throw new {msgPack}.MessagePackSerializationException($\"Expected key 1 or 2 but got '{{key}}'\");");

        sb.Indent--;
        sb.AppendLine("}");
        sb.Indent--;
        sb.AppendLine("}");
        sb.Indent--;
        sb.AppendLine("}");
    }

    void WriteEndOfFile() {
        for (var i = 0; i < bracesToCloseOnEnd; i++) {
            sb.Indent--;
            sb.Append('}');
        }

        sb.AppendLine();
    }
}
