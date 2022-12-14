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
