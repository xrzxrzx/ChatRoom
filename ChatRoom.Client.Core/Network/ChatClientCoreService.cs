using ChatRoom.Client.Core.Common;
using ChatRoom.Client.Core.Network.MessageBag;
using ChatRoom.Client.Core.Network.MessageBag.APIMessageBag;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Serilog;

namespace ChatRoom.Client.Core.Network;

public class ChatClientCoreService : IChatClientCoreService
{
    private TcpClient tcpClient;
    private NetworkStream? networkStream;
    private CancellationTokenSource? receiveCancellationTokenSource;
    private Task? receiveTask;
    private bool disposed;

    private ILogger logger;

    public event IChatClientCoreService.OnEventReceivedHandler? OnEventReceived;
    public event IChatClientCoreService.OnResponseReceivedHandler? OnResponseReceived;

    public ChatClientCoreService(ILogger logger)
    {
        tcpClient = new TcpClient();
        this.logger = logger;
    }

    public async Task ConnectAsync(ChatClientConfig chatClientConfig)
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(ChatClientCoreService));

        if (!tcpClient.Connected)
        {
            try
            {
                await tcpClient.ConnectAsync(chatClientConfig.ServerIp, chatClientConfig.ServerPort);
            }
            catch
            {
                throw;
            }
        }

        if (networkStream is null)
        {
            networkStream = tcpClient.GetStream();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (disposed)
            return;

        if (networkStream is null)
            return;

        byte[] buffer = Encoding.UTF8.GetBytes(message + "\n");
        await networkStream.WriteAsync(buffer, 0, buffer.Length);
    }

    public void StartReceive()
    {
        if (disposed || networkStream is null || receiveTask is not null)
            return;

        receiveCancellationTokenSource = new CancellationTokenSource();
        receiveTask = ReceiveAsync(receiveCancellationTokenSource.Token);
    }

    private async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        if (networkStream is null)
            return;

        byte[] buffer = new byte[1024];
        StringBuilder messageBuffer = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && !disposed)
            {
                int bytesRead = await networkStream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                    break;

                messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                while (true)
                {
                    string pendingMessage = messageBuffer.ToString();
                    int separatorIndex = pendingMessage.IndexOf('\n');
                    if (separatorIndex < 0)
                        break;

                    string message = pendingMessage[..separatorIndex].TrimEnd('\r');
                    messageBuffer.Remove(0, separatorIndex + 1);

                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    MessageBagAnalysis bagAnalysis = new MessageBagAnalysis(message);
                    if (bagAnalysis.IsEvent)//事件消息
                    {
                        EventMessageBag messageBag = bagAnalysis.GetEventMessageBag();
                        OnEventReceived?.Invoke(messageBag);
                    }
                    else//API响应消息
                    {
                        ResponseMessageBag responseBag = bagAnalysis.GetResponseMessageBag();
                        OnResponseReceived?.Invoke(responseBag);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException ex)
        {
            logger.Warning(ex, "接收循环已关闭。");
        }
        catch (InvalidOperationException ex)
        {
            logger.Warning(ex, "接收数据时套接字状态无效。");
        }
        catch (IOException ex)
        {
            logger.Warning(ex, "接收数据时发生 I/O 错误。");
        }
        finally
        {
            ResetConnection();//重置连接状态
            receiveTask = null;
            receiveCancellationTokenSource?.Dispose();
            receiveCancellationTokenSource = null;
        }
    }

    private void ResetConnection()
    {
        networkStream?.Dispose();
        networkStream = null;

        tcpClient.Dispose();
        tcpClient = new TcpClient();
    }

    public void Dispose()
    {
        disposed = true;

        receiveCancellationTokenSource?.Cancel();
        receiveCancellationTokenSource?.Dispose();
        receiveCancellationTokenSource = null;

        networkStream?.Dispose();
        networkStream = null;

        tcpClient.Dispose();
    }
}
