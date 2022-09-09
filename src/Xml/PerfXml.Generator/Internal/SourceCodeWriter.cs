namespace PerfXml.Generator.Internal;

internal sealed class SourceCodeWriter : IDisposable {
    private readonly IndentedTextWriter baseWriter;

    private readonly string indentationString;
    private readonly Stack<IndentedTextWriter> savePoints;

    public SourceCodeWriter(string indentationString = "    ") {
        this.indentationString = indentationString;
        baseWriter = new(new StringWriter(), this.indentationString);
        savePoints = new();
    }

    public IndentedTextWriter Writer => savePoints.Count > 0 ? savePoints.Peek() : baseWriter;

    public void Dispose() {
        if (savePoints.Count > 0) {
            FlushAll();
        }

        baseWriter.Dispose();
    }

    public SavePointInstance SavePoint() {
        var newWriter = new IndentedTextWriter(new StringWriter(), indentationString) {
            Indent = Writer.Indent
        };
        savePoints.Push(newWriter);
        return new(this);
    }

    public void CancelSavePoint() => savePoints.Pop();

    public void Flush() {
        if (savePoints.Count > 0) {
            var writer = savePoints.Pop();
            Writer.InnerWriter.Write(writer.InnerWriter.ToString());
        }
    }

    private void FlushAll() {
        while (savePoints.Count > 0) {
            var writer = savePoints.Pop();
            Writer.InnerWriter.Write(writer.InnerWriter.ToString());
        }
    }

    public override string ToString() => baseWriter.InnerWriter.ToString();

    internal sealed class SavePointInstance : IDisposable {
        private bool canceled;

        public SavePointInstance(SourceCodeWriter writer) {
            Writer = writer;
        }

        public SourceCodeWriter Writer { get; }

        public void Dispose() {
            if (canceled is false) {
                Writer.Flush();
            }
        }

        public void Cancel() {
            Writer.CancelSavePoint();
            canceled = true;
        }
    }
}