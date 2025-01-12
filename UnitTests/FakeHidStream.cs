using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class FakeHidStream : Stream
{
    private readonly List<Func<byte[], string?>> _responseMatchers = new();
    private readonly Queue<string> _responseQueue = new();
    
    // Add a mapping of expected writes to pre-programmed responses
    public void MockStreamResponse(Func<byte[], string?> requestResponder)
    {
        _responseMatchers.Add(requestResponder);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        foreach (var requestMatcher in _responseMatchers) // TODO: reverse order. Also provide a way for a matcher to remove itself?
        {
            var response = requestMatcher.Invoke(buffer); // TODO: switch to ReadOnlySpan, honor offset and count
            if (response != null)
            {
                _responseQueue.Enqueue(response);
                break;
            }
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_responseQueue.Count == 0)
        {
            throw new InvalidOperationException("No more responses in the queue.");
        }

        // TODO: keep returning this response until all consumed by the caller
        var response = Convert.FromHexString(_responseQueue.Dequeue());
        int bytesToCopy = Math.Min(response.Length, count);
        Array.Copy(response, 0, buffer, offset, bytesToCopy);

        return bytesToCopy;
    }

    // Implement abstract members of Stream
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { /* No-op for this mock */ }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
