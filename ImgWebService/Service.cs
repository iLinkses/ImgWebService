using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace ImgWebService
{
    internal class Service
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        internal string Url { get; set; }

        int ThreadCount, ImageCount;

        internal void GetHTML()
        {
            ///Удаление косой черты для удобства изменения относительных ссылок на абсолютные
            Url = Url.EndsWith("/") ? Url.Remove(Url.Length - 1) : Url;

            try
            {
                using (WebClient client = new WebClient())
                {
                    //string reply = client.DownloadString(Url);
                    string reply = Encoding.UTF8.GetString(client.DownloadData(Url));
                    //Console.WriteLine($"{Directory.GetCurrentDirectory()}\\{Url.Replace("http://", "").Replace("/","")}.html");
                    GetAltSrc(reply);
                    //await DownloadHTML(Url, reply);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
            }
            ///https://ru.stackoverflow.com/questions/622823/Нужно-получить-на-c-исходный-текст-html-страницы про куки файлы
        }

        /// <summary>
        /// Скачивает страницу сайта в папку с программой (ПЕРЕСТАЛ ИСПОЛЬЗОВАТЬ ИЗ-ЗА НЕНАДОБНОСТИ)
        /// </summary>
        /// <param name="code">Исходный текст страницы сайта</param>
        private async Task DownloadHTML(string code)
        {
            string writePath = $"{Directory.GetCurrentDirectory()}\\{Url.Replace("http://", "").Replace("/", "")}.html";

            try
            {
                using (StreamWriter sw = new StreamWriter(writePath, false, System.Text.Encoding.Default))
                {
                    await sw.WriteLineAsync(code);
                }

                Console.WriteLine("Запись выполнена");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Структура для хранения атрибутов изображения
        /// </summary>
        internal struct Image
        {
            internal string AltImg;
            internal string SrcImg;
            internal string NameImg;
            internal string HostUrl;
        }
        /// <summary>
        /// Парсит HTML и достает нужные атрибуты
        /// </summary>
        /// <param name="HTML_Code">Исходный текст страницы сайта</param>
        internal void GetAltSrc(string HTML_Code)
        {
            ArrayList ImgList = new ArrayList();
            Regex ImgRegex = new Regex("<img.*?>");
            ///Заполняем коллекцию тегами IMG
            foreach (Match ImgMatch in ImgRegex.Matches(HTML_Code))
            {
                ImgList.Add(ImgMatch.Value);
            }
            //Console.WriteLine(ImgList.Count);

            List<Image> Images = new List<Image>();
            Regex SrcRegex = new Regex("<img.+?src=[\"'](.+?)[\"'].*?>");
            Regex AltRegex = new Regex("<img.+?alt=[\"'](.*?)[\"'].*?>");

            try
            {
                ///Достаем из тега атрибуты src и alt
                foreach (var img in ImgList)
                {
                    Images.Add(new Image
                    {
                        AltImg = AltRegex.Match(img.ToString()).Groups[1].Value,
                        SrcImg = SrcRegex.Match(img.ToString()).Groups[1].Value,
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
            }

            ///Убираем дубликаты
            List<Image> FilteredImages = Images.GroupBy(img => img.SrcImg).Select(src => src.First()).ToList();

            for (int i = 0; i < FilteredImages.Count; ++i)
            {
                //Uri uri = new Uri(FilteredImages[i].SrcImg, UriKind.RelativeOrAbsolute);
                var Img = FilteredImages[i];
                ///если адрес не абсолютный
                if (!new Uri(FilteredImages[i].SrcImg, UriKind.RelativeOrAbsolute).IsAbsoluteUri)
                {
                    Img.SrcImg = Img.SrcImg.Insert(0, $"{Url.Split(':').First()}://{new Uri(Url).Host}");
                }
                ///Добавляем в list название изображения
                Img.NameImg = Img.SrcImg.Split('/').Last();
                ///Добавляем в list host
                Img.HostUrl = new Uri(Img.SrcImg).Host;
                FilteredImages[i] = Img;
            }

            GetImages(FilteredImages);
        }

        /// <summary>
        /// Создает каталоги, если их нет и скачивает в них изображения
        /// </summary>
        /// <param name="Images">list с параметрами изображения</param>
        internal void GetImages(List<Image> Images)
        {
            if (Images.Count == 0)
            {
                Console.WriteLine($"На сайте {Url} не нашлось тегов <img>");
                logger.Info($"На сайте {Url} не нашлось тегов <img>");
                return;
            }
            Console.WriteLine($"Количество доступных для скачивания изображений: {Images.Count}");

            ///Добавить различные проверки
            Console.WriteLine("Введите количество изображений, которые необходимо скачать: ");
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out ImageCount))
                {
                    Console.WriteLine("Ошибка ввода! Введите целое число...");
                    continue;
                }
                if (ImageCount > 0 && ImageCount < Images.Count + 1)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Ошибка ввода! Введите число больше нуля, а также меньше или равное {Images.Count}...");
                    continue;
                }
            }

            Console.WriteLine($"Ваш компьютер поддерживает {Environment.ProcessorCount + (Environment.ProcessorCount < 5 ? " потока" : " потоков")}");

            ///Добавить различные проверки
            Console.WriteLine("Введите количество потоков: ");
            while (true)
            {
                if (!int.TryParse(Console.ReadLine(), out ThreadCount))
                {
                    Console.WriteLine("Ошибка ввода! Введите целое число...");
                    continue;
                }
                if (ThreadCount > 0 && ThreadCount < Environment.ProcessorCount + 1)
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Ошибка ввода! Введите число больше нуля, а также меньше или равное {Environment.ProcessorCount}...");
                    continue;
                }
            }

            ///Задание реального количества потоков
            if (ImageCount < ThreadCount)
            {
                ThreadCount = ImageCount;
            }

            ///Создание каталога images
            DirectoryInfo DirImages = new DirectoryInfo(Directory.GetCurrentDirectory() + @"\images");
            if (!DirImages.Exists)
            {
                DirImages.Create();
            }

            try
            {
                ///Распараллеливаем скачивание
                Parallel.ForEach(Images.Take(ImageCount), new ParallelOptions { MaxDegreeOfParallelism = ThreadCount }, Img =>
                {
                    ///Создание подкаталогов по имени хоста
                    if (!new DirectoryInfo($@"{DirImages}\{Img.HostUrl}").Exists)
                    {
                        DirImages.CreateSubdirectory(Img.HostUrl);
                    }
                    ///Скачиваем изображения
                    using (WebClient client = new WebClient())
                    {
                        if (!new FileInfo($@"{DirImages}\{Img.HostUrl}\{Img.NameImg}").Exists)
                        {
                            ///Добавить проверку на "валидность" ссылки
                            client.DownloadFile(Img.SrcImg, $@"{DirImages}\{Img.HostUrl}\{Img.NameImg}");
                        }
                        else
                        {
                            logger.Info("Попытка перезаписи файла");
                        }
                    }
                });

                SerializeJson(Images.Take(ImageCount).ToList());
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Формирует ответ в виде Json
        /// </summary>
        /// <param name="Images">list с параметрами изображения</param>
        private void SerializeJson(List<Image> Images)
        {
            List<JsonAnswer.RootObject> JsonAnswerList = new List<JsonAnswer.RootObject>();

            try
            {
                foreach (var Host in Images.Select(h => h.HostUrl).Distinct().ToList())
                {
                    var JsonObject = new JsonAnswer.RootObject
                    {
                        host = Host,
                        images = SetImageList(Host, Images)

                    };
                    JsonAnswerList.Add(JsonObject);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
            }
            
            Console.WriteLine(JsonConvert.SerializeObject(JsonAnswerList, Newtonsoft.Json.Formatting.Indented));
        }

        /// <summary>
        /// Заполняет "промежуточный" лист с параметрами изображения для объекта HOST
        /// </summary>
        /// <param name="Host"></param>
        /// <param name="Images"></param>
        private List<JsonAnswer.images> SetImageList(string Host, List<Image> Images)
        {
            List<JsonAnswer.images> imagesList = new List<JsonAnswer.images>();
            try
            {
                foreach (var img in Images.Where(h => h.HostUrl == Host).ToList())
                {
                    FileInfo fileInfo = new FileInfo($@"{Directory.GetCurrentDirectory()}\images\{img.HostUrl}\{img.NameImg}");
                    imagesList.Add(new JsonAnswer.images { alt = img.AltImg, src = img.SrcImg, size = fileInfo.Length.ToString() + " байт" });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                Console.WriteLine(ex.Message);
            }
            return imagesList;
        }
    }
}
