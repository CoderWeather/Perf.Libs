namespace Perf.SourceGeneration.Internal;

internal sealed class NestedScope : IDisposable {
    private static readonly Stack<NestedScope> Stack = new();

    private readonly IndentedTextWriter writer;
    private bool shouldCloseOnDispose = true;

    private NestedScope(IndentedTextWriter writer) {
        this.writer = writer;
    }

    public void Dispose() {
        if (shouldCloseOnDispose) {
            Close();
        }

        if (Stack.Any()) {
            Stack.Pop();
        }
    }

    public void Close() {
        writer.Indent--;
        writer.WriteLine('}');
        shouldCloseOnDispose = false;
    }

    public static void CloseLast() {
        var scope = Stack.Peek();
        scope.Close();
    }

    public static NestedScope Start(IndentedTextWriter writer) {
        var scope = new NestedScope(writer);
        Stack.Push(scope);
        writer.WriteLine('{');
        writer.Indent++;
        return scope;
    }
}

/// <summary>Helper class for generating partial parts of nested types</summary>
internal sealed class NestedClassScope : IDisposable {
    private readonly List<string> containingClasses = new(0);
    private readonly IndentedTextWriter writer;

    private NestedClassScope(IndentedTextWriter writer, ISymbol classSymbol) {
        this.writer = writer;
        var containingSymbol = classSymbol.ContainingSymbol;
        while (containingSymbol.StrictEquals(classSymbol.ContainingNamespace) is false) {
            var containingNamedType = (INamedTypeSymbol)containingSymbol;
            containingClasses.Add(GetClsString(containingNamedType));
            containingSymbol = containingSymbol.ContainingSymbol;
        }

        containingClasses.Reverse();
    }

    public void Dispose() {
        writer.Indent--;
        writer.WriteLine('}');
        foreach (var _ in containingClasses) {
            writer.Indent--;
            writer.WriteLine("}"); // end container
        }
    }

    public static NestedClassScope Start(IndentedTextWriter writer, INamedTypeSymbol cls) {
        var scope = new NestedClassScope(writer, cls);
        foreach (var containingClass in scope.containingClasses) {
            writer.WriteLine($"{containingClass}");
            writer.WriteLine("{");
            writer.Indent++;
        }

        writer.WriteLine($"{GetClsString(cls)}");
        writer.WriteLine('{');
        writer.Indent++;

        return scope;
    }

    private static string TypeKindToStr(INamedTypeSymbol type) {
        return type switch {
            { TypeKind: TypeKind.Class } => type.IsRecord
                ? "record"
                : "class",
            { IsValueType: true } => type.IsRecord
                ? "record struct"
                : "struct",
            _ => throw new($"Unhandled kind {type} in {nameof(TypeKindToStr)}")
        };
    }

    private static string GetClsString(INamedTypeSymbol type) {
        // {public/private...} {ref} partial {class/struct} {name}
        var visibilityModifier = type.Accessibility();
        var refModifier = type.IsRefLikeType ? " ref" : null;
        var fullName = type.ToString()!;
        const string format = "{0}{1} partial {2} {3}";
        var str = string.Format(format,
            visibilityModifier,
            refModifier,
            TypeKindToStr(type),
            fullName.Substring(fullName.LastIndexOf('.') + 1)
        );
        return str;
    }
}