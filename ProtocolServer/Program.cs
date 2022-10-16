using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using ProtocolServer.Transport;

var listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, 8989));
listenSocket.Listen(128);
var transportScheduler = new IOQueue();
var applicationScheduler = PipeScheduler.ThreadPool;
var senderPool = new SenderPool();
var memoryPool = new PinnedBlockMemoryPool();

await AcceptConnections();
async Task AcceptConnections()
{
    while (true)
    {
        var socket = await listenSocket.AcceptAsync();
        var connection = new Connection(socket, senderPool,
            transportScheduler, applicationScheduler, memoryPool);
        _ = ProcessConnection(connection);
    }
}

static async Task ProcessConnection(Connection connection)
{
    connection.Start();
    while (true)
    {
        var result = await connection.Input.ReadAsync();
        var buff = result.Buffer;
        if (buff.IsSingleSegment)
        {
            await connection.Output.WriteAsync(buff.First);
        }
        else
        {
            foreach (var mem in buff)
            {
                await connection.Output.WriteAsync(mem);
            }
        }
        connection.Input.AdvanceTo(buff.End);
        if (result.IsCompleted || result.IsCanceled)
        {
            break;
        }
    }
    connection.Shutdown();
    await connection.DisposeAsync();
}