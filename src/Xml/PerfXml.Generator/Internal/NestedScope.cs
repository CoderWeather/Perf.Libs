namespace PerfXml.Generator.Internal;

sealed class NestedScope : IDisposable {
    NestedScope(IndentedTextWriter writer) {
        this.writer = writer;
    }

    readonly IndentedTextWriter writer;
    static readonly Stack<NestedScope> Stack = new();
    bool shouldCloseOnDispose = true;

    public void Dispose() {
        if (shouldCloseOnDispose) {
            Close();
        }

        if (Stack.Count != 0) {
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
sealed class NestedClassScope : IDisposable {
    readonly List<string> containingClasses = new(0);
    readonly IndentedTextWriter writer;

    NestedClassScope(IndentedTextWriter writer, ISymbol classSymbol) {
        this.writer = writer;
        var containingSymbol = classSymbol.ContainingSymbol;
        while (containingSymbol.StrictEquals(classSymbol.ContainingNamespace) is false) {
            var containingNamedType = (INamedTypeSymbol)containingSymbol;
            containingClasses.Add(GetClsString(containingNamedType));
            containingSymbol = containingSymbol.ContainingSymbol;
        }

        containingClasses.Reverse();
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

    static string TypeKindToStr(INamedTypeSymbol type) {
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

    static string GetClsString(INamedTypeSymbol type) {
        // {public/private...} {ref} partial {class/struct} {name}
        var visibilityModifier = type.Accessibility();
        var refModifier = type.IsRefLikeType ? " ref" : null;
        var fullName = type.ToString()!;
        const string format = "{0}{1} partial {2} {3}";
        var str = string.Format(
            format,
            visibilityModifier,
            refModifier,
            TypeKindToStr(type),
            fullName.Substring(fullName.LastIndexOf('.') + 1)
        );
        return str;
    }

    public void Dispose() {
        writer.Indent--;
        writer.WriteLine('}');
        foreach (var _ in containingClasses) {
            writer.Indent--;
            writer.WriteLine("}"); // end container
        }
    }
}
