using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace WebServerJ4
{
    class Server
    {
        // объект, который принимает tcp-клиентов
        TcpListener listener;

        public Server(int port)
        {
            // создаем слушателя
            listener = new TcpListener(IPAddress.Any, port);

            Console.WriteLine("ЗАПУСКАЕМ ПРОСЛУШКУ");

            // запустили прослушку
            listener.Start();

            Console.WriteLine("ЗАПУСТИЛИ ПРОСЛУШКУ");

            while (true)
            {
                // отлавливаем клиента, который отправил на сервер запрос

                TcpClient tcpClient = listener.AcceptTcpClient();

                Thread thread = new Thread(ClientThread);

                thread.Start(tcpClient);

                Console.WriteLine("КЛИЕНТА ПОЙМАЛИ");
            }
        }

        static void ClientThread(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            Client newClient = new Client(tcpClient);
        }


        static void Main(string[] args)
        {
            // web - порт 80
            Server server = new Server(80);
        }
    }

    class Client
    {
        public Client(TcpClient client)
        {
            // полный запрос от клиента
            string request = "";

            byte[] buffer = new byte[1024];

            int count;   // кол-во байтов запроса клиента

            // вариант простой. Запрос короткий
            count = client.GetStream().Read(buffer, 0, buffer.Length);

            request = Encoding.UTF8.GetString(buffer, 0, count);

            Console.WriteLine(request);

            // нужно получить url ресурса (адрес ресурса)
            // ДОМАШНЕЕ ЗАДАНИЕ

            // 127.0.0.1/12.jpg -> /12.jpg
            // 127.0.0.1/index.html -> /index.html

            string url = GetRequest(request);
            // /
            // /index.html
            // /index.html?num=12

            Console.WriteLine("URL: " + url);


            if (url == "/")  // 127.0.0.1 -> /
            {
                url += "index.html";   // /index.html
            }

            string filePath = @"www" + url;  // www/index.html

            


            // проверка сущетвования файла на сервере
            if (!File.Exists(filePath))
            {
                // вывести пользователю ошибку
                SendError(client, 404);

                return;
            }

            // определение content-type
            string contentType = "text/html";
            if (url.Contains(".css"))
            {
                contentType = "text/css";
            }


            // работа с файлом - создание потока чтения
            FileStream fileStream = new FileStream(filePath, FileMode.Open);

            string initLine = "HTTP/1.1 200 OK";

            string headersLine = "Content-Type: " + contentType + "\n" +
                "Content-Length: " + fileStream.Length;

            string response_first = initLine + "\n" + headersLine + "\n\n";

            byte[] firstBytes = Encoding.UTF8.GetBytes(response_first);

            // [отправить первую часть клиенту]

            // получили поток для общения с клиентом
            NetworkStream networkStream = client.GetStream();

            networkStream.Write(firstBytes, 0, firstBytes.Length);



            // работа с файловым потоком
            // count - кол-во байтов из файлового потока
            count = fileStream.Read(buffer, 0, buffer.Length);

            // [отправить вторую часть клиенту - body            networkStream.Write(buffer, 0, count);

            fileStream.Close();
            client.Close();
        }

        // GET /index.html HTTP/1.1\nHost: 127.0.0.1\nAccept: ...\r\n\r\n
        public string GetResourse(string request)
        {
            string[] parts = request.Split();
            return parts[1];
        }

        public string GetRequest(string request)
        {
            Regex regex = new Regex(@"/\S*");

            MatchCollection matchCollection = regex.Matches(request);

            return matchCollection[0].Value;   // первое совпадение
        }

        public void SendError(TcpClient client, int code)
        {
            string initLine = "HTTP/1.1 " + code.ToString() +
                 " " + (HttpStatusCode)code;

            string bodyLine = "<html><body><h1>" + code.ToString() +
                "</h1></body></html>";

            string headersLine = "Content-Type: text/html\n" +
                "Content-Length: " + bodyLine.Length.ToString();

            string response = initLine + "\n" +
                headersLine + "\n\n" + bodyLine;

            byte[] bytes = Encoding.UTF8.GetBytes(response);

            // получили поток для общения с клиентом
            NetworkStream networkStream = client.GetStream();

            networkStream.Write(bytes, 0, bytes.Length);

            client.Close();
        }

        public void Test(TcpClient client)
        {
            string html = "<html> <b><i><font color = red> " +
                "Hello from server! " +
                "</font></i></b> </html>";

            string response = "HTTP/1.1 200 OK\n" +
                "Content-Type: text/html\n" +
                "Content-Length: " + html.Length.ToString() +
                "\n\n" + html;

            byte[] bytes = Encoding.UTF8.GetBytes(response);

            // получили поток для общения с клиентом
            NetworkStream networkStream = client.GetStream();

            networkStream.Write(bytes, 0, bytes.Length);

            client.Close();
        }
    }
}

