using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PipeServer {
    public static PipeServer Instance { get; private set; }

    public event EventHandler<PipeCommand> onPipeCommandReceived;

    public const string ReadPipeName = "VCR_R";
    public const string WritePipeName = "VCR_W";

    private Thread readThread;
    private Thread writeThread;
    private Queue<string> readQueue;
    private Queue<string> writeQueue;
    private object readLock;
    private object writeLock;

    public static NamedPipeServerStream pipeReadServer;
    public static NamedPipeServerStream pipeWriteServer;

    public PipeServer() {
        Instance = this;
        readQueue = new Queue<string>();
        writeQueue = new Queue<string>();
        readLock = new object();
        writeLock = new object();
    }

    public void Start() {
        //Debug.LogError("Ожидаем подключения клиента...");
        readThread = new Thread(ServerThreadRead);
        readThread.Start();
        writeThread = new Thread(ServerThreadWrite);
        writeThread.Start();

        // Создаем на сцене unity объект и прокидываем ему функцию для callback'a 
        // объект вызывает ее каждый фрейм в функции Update
        FunctionUpdater.Create(ReadMessages);
    }

    public void CreateNewGameObjectPipeListener() {
        FunctionUpdater.Create(ReadMessages);
    }

    private void ServerThreadRead() {
        using (pipeReadServer = new NamedPipeServerStream(ReadPipeName, PipeDirection.In)) {
            
            Debug.LogError("Start pipe read server...");

            // Ожидаем подключения клиента
            pipeReadServer.WaitForConnection();
            Debug.LogError("Client Read connected!");

            try {
                StreamString readStreamString = new StreamString(pipeReadServer);

                while (true) {
                    string jsonMessage = readStreamString.ReadString();
                    Debug.LogError("json message " + jsonMessage);

                    //На всякий случай добавляем лок при добавлении сообщения в очередь
                    // прочитанных, но еще не полученных сообщений
                    lock (readLock) {
                        readQueue.Enqueue(jsonMessage);
                    }

                    if (Thread.CurrentThread.ThreadState != ThreadState.Aborted) {
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
            }
        }
        Debug.LogError("Read server finished");
    }

    private void ServerThreadWrite() {
        using (pipeWriteServer = new NamedPipeServerStream(WritePipeName, PipeDirection.Out)) {
            Debug.LogError("Start pipe write server...");
            // Ожидаем подключения клиента
            pipeWriteServer.WaitForConnection();
            Debug.LogError("Client Write connected!");

            try {
                StreamString writeStreamString = new StreamString(pipeWriteServer);

                while (true) {
                    string messageFromQueue = null;


                    // На всякий случай добавляем лок при считывании сообщения из очереди
                    lock (writeLock) {
                        if (writeQueue.Count > 0) {
                            messageFromQueue = writeQueue.Dequeue();
                        }
                    }

                    if (messageFromQueue != null) {
                        Debug.LogError("Send message: " + messageFromQueue);
                        writeStreamString.WriteString(messageFromQueue);
                    }
                    
                    if (Thread.CurrentThread.ThreadState != ThreadState.Aborted) {
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError(ex);
            }
            Debug.LogError("Write server finished");
        }
    }


    public new void SendMessage(string message) {
        SendMessage(new PipeCommand { command = message });
    }

    public void SendMessage(PipeCommand pipeCommand) {
        lock (writeQueue) {
            writeQueue.Enqueue(JsonUtility.ToJson(pipeCommand));
        }
    }

    private void ReadMessages() {
        lock (readLock) {
            if (readQueue.Count > 0) {
                string message = readQueue.Dequeue();
                PipeCommand pipeCommand = JsonUtility.FromJson<PipeCommand>(message);
                onPipeCommandReceived?.Invoke(this, pipeCommand);
            }
        }
    }

    public void DestroySelf() {
        // Запускается в OnDestroy, вырубает потоки-серверы
        Debug.LogError("Pipe server destroy self!");
        Debug.LogError("is alive write therad - " + readThread.IsAlive);
        Debug.LogError("is alive write therad - " + writeThread.IsAlive);
        try {
            if (readThread != null && readThread.ThreadState != ThreadState.Aborted) {
                using (NamedPipeClientStream pipeReadClient = new NamedPipeClientStream(".", WritePipeName, PipeDirection.In)) {
                    pipeReadClient.Connect();
                }
            }
            if (writeThread != null && writeThread.ThreadState != ThreadState.Aborted) {
                using (NamedPipeClientStream pipeWriteClient = new NamedPipeClientStream(".", ReadPipeName, PipeDirection.Out)) {
                    pipeWriteClient.Connect();
                }
            }
        } finally {
            Thread.Sleep(100);
            readThread?.Abort();
            writeThread?.Abort();
        }
    }
}

[Serializable]
public struct PipeCommand {
    public string command;
    public string extraData;
}


public class StreamString {
    private Stream ioStream;
    private UTF8Encoding streamEncoding;

    public StreamString(Stream ioStream) {
        this.ioStream = ioStream;
        streamEncoding = new UTF8Encoding();
    }

    public string ReadString() {
        int len = 0;

        len = ioStream.ReadByte() * 256;
        len += ioStream.ReadByte();
        byte[] inBuffer = new byte[len];
        ioStream.Read(inBuffer, 0, len);

        return streamEncoding.GetString(inBuffer);
    }

    public int WriteString(string outString) {
        byte[] outBuffer = streamEncoding.GetBytes(outString);
        int len = outBuffer.Length;
        if (len > UInt16.MaxValue) {
            len = (int)UInt16.MaxValue;
        }
        ioStream.WriteByte((byte)(len / 256));
        ioStream.WriteByte((byte)(len & 255));
        ioStream.Write(outBuffer, 0, len);
        ioStream.Flush();

        return outBuffer.Length + 2;
    }
}