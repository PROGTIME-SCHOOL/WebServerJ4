using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections.Generic;   // for dictionary
using System.Linq;   // for collections

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


            string url = GetRequest(request);
            // /
            // /index.html
            // /index.html?num=12
            // http://127.0.0.1/math?n1=3&n2=2&n3=89

            Console.WriteLine("URL: " + url);

            string page = "";      // index.html
            string param = "";     // num=12


            if (url == "/")  // 127.0.0.1 -> /
            {
                url += "index.html";   // /index.html
            }

            // для файла убираем параметры
            if (url.Contains('?'))
            {
                string[] parts = url.Split('?');
                page = parts[0];      // /math
                param = parts[1];     // n1=3&n2=2&n3=89
            }
            else
            {
                page = url;
            }

            string filePath = @"www" + page;  // www/index.html

            // работа с параметром
            int value = 0;

            Dictionary<string, int> pairs = new Dictionary<string, int>();

            if (param != "")  // a=12   // n1=3&n2=2&n3=89
            {
                Regex regex = new Regex(@"\w+=\w+");

                var collection = regex.Matches(param);

                foreach (Match item in collection)
                {
                    string text = item.Value;   // n1=3

                    string nameParam = text.Split("=")[0];
                    string valueParam = text.Split("=")[1];

                    pairs.Add(nameParam, int.Parse(valueParam)); // заполнить словарь
                }

                //value = int.Parse(regex.Match(param).Value);

                // Square(client, value);

                // return;
            }

            // Сложение всех чисел в запросе
            if (page.Contains("math"))
            {
                AddNumbers(client, pairs);

                return;
            }

            if (page.Contains("sub"))   // вычитание
            {
                SubNumbers(client, pairs); // 4-3-2

                return;
            }



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

            FileStream fileStream = new FileStream(filePath, FileMode.Open);

            #region Формируем начальную строку + заголовки

            string initLine = "HTTP/1.1 200 OK";

            string headersLine = "Content-Type: " + contentType + "\n" +
                "Content-Length: " + fileStream.Length;

            string response_first = initLine + "\n" + headersLine + "\n\n";

            byte[] firstBytes = Encoding.UTF8.GetBytes(response_first);

            #endregion

            // [отправить первую часть клиенту]
            NetworkStream networkStream = client.GetStream(); // получили поток для общения с клиентом
            networkStream.Write(firstBytes, 0, firstBytes.Length);

            // [отправить вторую часть клиенту - body    
            while (fileStream.Position < fileStream.Length) // Пока не достигнут конец файла
            {
                count = fileStream.Read(buffer, 0, buffer.Length); // Читаем данные из файла          
                client.GetStream().Write(buffer, 0, count); // И передаем их клиенту
            }


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

        public void SubNumbers(TcpClient client, Dictionary<string, int> pairs)
        {
            // logic
            // 5 2 = 

            int res = pairs.First().Value;  // 5

            foreach (var item in pairs)
            {
                res -= item.Value;
            }

            res += pairs.First().Value;


            string html = "<html> <b><i><font color = red><h1> " +
                res.ToString() +
                "</h1></font></i></b> </html>";

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

        public void AddNumbers(TcpClient client, Dictionary<string, int> pairs)
        {
            // logic
            int res = 0;
            foreach (var item in pairs)
            {
                res += item.Value;
            }

            string html = "<html> <b><i><font color = red><h1> " +
                res.ToString() +
                "</h1></font></i></b> </html>";

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

        public void Square(TcpClient client, int value)
        {
            int res = value * value; // считаем квадрат

            string html = "<html> <b><i><font color = red><h1> " +
                res.ToString() +
                "</h1></font></i></b> </html>";

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

