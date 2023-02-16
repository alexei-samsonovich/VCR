using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NamedPipeClient {
    class Program {
        static void Main(string[] args) {
            PipeClient pipeClient = new PipeClient();
            pipeClient.OnPipeCommandReceived += (pipeClient_, pipeCommand) => { Console.WriteLine($"Получено сообщение: " + pipeCommand.Command + "\n"); };
            pipeClient.Start();

            while (true) {
                var message = Console.ReadLine();
                if (message != null) {
                    pipeClient.SendMessage(message);
                }
            }
        }
    }

    [Serializable]
    public struct PipeCommand {
        [JsonPropertyName("command")]
        public string Command { get; set; }
        [JsonPropertyName("extraData")]
        public string ExtraData { get; set; }
    }

    public class PipeClient {
        public static PipeClient Instance { get; private set; }

        public const string ReadPipeName = "VCR_W";
        public const string WritePipeName = "VCR_R";

        public event EventHandler<PipeCommand> OnPipeCommandReceived;

        private Thread readThread;
        private Thread writeThread;
        private Thread readFromQueueThread;
        private StreamString streamReadString;
        private StreamString streamWriteString;
        private Queue<string> readQueue;
        private Queue<string> writeQueue;
        private object readLock;
        private object writeLock;



        public PipeClient() {
            Instance = this;
            readQueue = new Queue<string>();
            writeQueue = new Queue<string>();
            readLock = new object();
            writeLock = new object();
        }

        public void Start() {
            Console.WriteLine("Ожидаем подключения клиента...");
            readThread = new Thread(ClientReadThread);
            readThread.Start();
            writeThread = new Thread(ClientWriteThread);
            writeThread.Start();

            readFromQueueThread = new Thread(ReadFromPipeQueue);
            readFromQueueThread.Start();
        }

        public void ReadFromPipeQueue() {
            while (true) {
                lock (readLock) {
                    if (readQueue.Count > 0) {
                        string message = readQueue.Dequeue();
                        PipeCommand pipeCommand = JsonSerializer.Deserialize<PipeCommand>(message);
                        OnPipeCommandReceived?.Invoke(this, pipeCommand);
                    }
                }
            }
        }

        private void ClientReadThread() {
            using (NamedPipeClientStream pipeReadClient = new NamedPipeClientStream(".", ReadPipeName, PipeDirection.In)) {

                // Пытаемся сконнектиться
                while (!pipeReadClient.IsConnected) {
                    Console.WriteLine("Коннектимся к серверу...");
                    try {
                        pipeReadClient.Connect();
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Ошибка подключения к серверу: {ex}");
                    }
                    Thread.Sleep(100);
                }

                Console.WriteLine("Клиент(чтение) подключен!!!");

                try {
                    streamReadString = new StreamString(pipeReadClient);

                    while (true) {
                        string message = streamReadString.ReadString();

                        lock (readLock) {
                            readQueue.Enqueue(message);
                        }

                        Thread.Sleep(10);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine("Ошибка: " + ex);
                }

                Console.WriteLine("Канал на чтение закрыт!");
            }
        }

        private void ClientWriteThread() {
            using (NamedPipeClientStream pipeWriteClient = new NamedPipeClientStream(".", WritePipeName, PipeDirection.Out)) {

                // Пытаемся сконнектиться
                while (!pipeWriteClient.IsConnected) {
                    Console.WriteLine("Коннектимся к серверу...");
                    try {
                        pipeWriteClient.Connect();
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Ошибка подключения к серверу: {ex}");
                    }
                    Thread.Sleep(100);
                }

                Console.WriteLine("Клиент(запись) подключен!!!");

                try {
                    streamWriteString = new StreamString(pipeWriteClient);

                    //SendMessage("Hello from the Client!");

                    while (true) {
                        string messageFromQueue = null;

                        // На всякий случай добавляем лок при считывании сообщения из очереди
                        lock (writeLock) {
                            if (writeQueue.Count > 0) {
                                messageFromQueue = writeQueue.Dequeue();
                            }
                        }

                        if (messageFromQueue != null) {
                            Console.WriteLine("Отправляем сообщение: " + messageFromQueue);
                            streamWriteString.WriteString(messageFromQueue);
                        }

                        Thread.Sleep(10);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine("Ошибка: " + ex);
                }

                Console.WriteLine("Канал на запись закрыт!");
            }
        }

        public void SendMessage(string message) {
            SendMessage(new PipeCommand { Command = message });
        }

        public void SendMessage(PipeCommand pipeCommand) {
            lock (writeLock) {
                writeQueue.Enqueue(JsonSerializer.Serialize(pipeCommand));
            }
        }
    }
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
