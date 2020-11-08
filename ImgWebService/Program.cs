using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace ImgWebService
{
    class Program
    {
        //private static string Url;
        static void Main(string[] args)
        {
            // КОНФИГУРИРУЕМ ЛОГЕР            
            // NLog.Config.DOMConfigurator.Configure();

            Console.Title = "ImgWebService";
            Console.Write($"\t=== === === === === === ===\n" +
                          $"\tВеб-сервис ImgWebService\n" +
                          $"\tversion: {Assembly.GetExecutingAssembly().GetName().Version}\n" +
                          $"\t=== === === === === === ===\n\n");
            Console.ForegroundColor = ConsoleColor.Green;
            while (true)
            {
                Service service = new Service();

                Console.Write("Введите Url-адрес страницы сайта: ");
                service.Url = Console.ReadLine();

                service.GetHTML();

                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadLine();
                Console.Clear();
            }
        }
    }
}
