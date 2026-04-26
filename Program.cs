using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TaskManagerServer
{
    class Program
    {
        private const int PORT = 9000;

        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
            Console.WriteLine("Сервер запущено. Порт: " + PORT);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Підключився клієнт: " +
                    ((IPEndPoint)client.Client.RemoteEndPoint).Address);

                
                Thread t = new Thread(() => HandleClient(client))
                { IsBackground = true };
                t.Start();
            }
        }

        static void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8)
                { AutoFlush = true };

                string command;
                while ((command = reader.ReadLine()) != null)
                {
                    Console.WriteLine("Команда: " + command);

                    if (command == "LIST")
                    {
                        // Повернути список процесів
                        Process[] procs = Process.GetProcesses();
                        Array.Sort(procs, (a, b) =>
                            string.Compare(a.ProcessName,
                                           b.ProcessName,
                                           StringComparison.OrdinalIgnoreCase));

                        foreach (var p in procs)
                            writer.WriteLine(p.ProcessName + ".exe");

                        writer.WriteLine("END"); // маркер кінця списку
                    }
                    else if (command.StartsWith("KILL "))
                    {
                        // Завершити процес
                        string procName = command.Substring(5).Trim();
                        if (procName.EndsWith(".exe"))
                            procName = procName.Substring(0,
                                procName.Length - 4);
                        try
                        {
                            Process[] procs =
                                Process.GetProcessesByName(procName);
                            if (procs.Length == 0)
                            {
                                writer.WriteLine("ERROR:Процес не знайдено");
                            }
                            else
                            {
                                foreach (var p in procs)
                                {
                                    p.Kill();
                                    p.WaitForExit(2000);
                                }
                                writer.WriteLine("OK");
                            }
                        }
                        catch (Exception ex)
                        {
                            writer.WriteLine("ERROR:" + ex.Message);
                        }
                    }
                    else if (command.StartsWith("START "))
                    {
                        // Запустити новий процес
                        string path = command.Substring(6).Trim();
                        try
                        {
                            Process.Start(path);
                            writer.WriteLine("OK");
                        }
                        catch (Exception ex)
                        {
                            writer.WriteLine("ERROR:" + ex.Message);
                        }
                    }
                    else
                    {
                        writer.WriteLine("ERROR:Невідома команда");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Помилка клієнта: " + ex.Message);
            }
            finally
            {
                client.Close();
                Console.WriteLine("Клієнт відключився.");
            }
        }
    }
}
