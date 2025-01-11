using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class FakeHidStream : Stream
{
    private readonly Queue<byte[]> _responseQueue = new Queue<byte[]>();
    private readonly MemoryStream _writeBuffer = new MemoryStream();

    // Add a mapping of expected writes to pre-programmed responses
    public void AddResponse(byte[] expectedWrite, string hexResponse)
    {
        /*public void MockStreamResponse(Func<byte[], bool> requestMatcher, string hexResponse)
    {
        // TODO: hook into when stream is written to
    }*/
        _responseQueue.Enqueue(Convert.FromHexString(hexResponse));
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        // Capture the written data for comparison or debugging
        byte[] writtenData = new byte[count];
        Array.Copy(buffer, offset, writtenData, 0, count);

        // Optionally, compare `writtenData` to an expected value here.
        // For simplicity, this mock directly serves queued responses.
        _writeBuffer.Write(writtenData, 0, writtenData.Length);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_responseQueue.Count == 0)
        {
            throw new InvalidOperationException("No more responses in the queue.");
        }

        var response = _responseQueue.Dequeue();
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
