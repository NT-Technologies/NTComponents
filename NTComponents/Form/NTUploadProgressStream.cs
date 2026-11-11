namespace NTComponents;

internal sealed class NTUploadProgressStream : Stream {
    private readonly Stream _inner;
    private readonly Func<int, Task> _onBytesRead;
    private readonly Func<Task>? _onCompleted;
    private int _completed;

    public NTUploadProgressStream(Stream inner, Func<int, Task> onBytesRead, Func<Task>? onCompleted = null) {
        _inner = inner;
        _onBytesRead = onBytesRead;
        _onCompleted = onCompleted;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;

    public override long Position {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) {
        var bytesRead = _inner.Read(buffer, offset, count);
        ReportRead(bytesRead);
        return bytesRead;
    }

    public override int Read(Span<byte> buffer) {
        var bytesRead = _inner.Read(buffer);
        ReportRead(bytesRead);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        var bytesRead = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        await ReportReadAsync(bytesRead).ConfigureAwait(false);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        var bytesRead = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        await ReportReadAsync(bytesRead).ConfigureAwait(false);
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.WriteAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync() {
        await _inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void ReportRead(int bytesRead) {
        if (bytesRead > 0) {
            _ = _onBytesRead(bytesRead);
            return;
        }

        _ = CompleteAsync();
    }

    private Task ReportReadAsync(int bytesRead) {
        if (bytesRead > 0) {
            return _onBytesRead(bytesRead);
        }

        return CompleteAsync();
    }

    private Task CompleteAsync() {
        if (Interlocked.Exchange(ref _completed, 1) == 1) {
            return Task.CompletedTask;
        }

        return _onCompleted?.Invoke() ?? Task.CompletedTask;
    }
}
