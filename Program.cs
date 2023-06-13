using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NXTest.DB;
using RKNet_Model;
using RKNet_Model.VMS.NX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NXTest
{
    internal class Program
    {
        private static List<NxCamera> Cameras { get; set; } // список камер
        private static DbContextOptionsBuilder<SQLiteDBContext> SQLitedbContextOptionsBuilder { get; set; }
        private static DbContextOptionsBuilder<MSSQLDBContext> MSSQLdbContextOptionsBuilder { get; set; }
        private static Settings Settings { get; set; } // настройки
        private static string LogPath { get; set; } = "C:\\GetNXCameraPhotos\\logs.txt"; //путь к логам
        private static string JsonPath { get; set; } = "C:\\GetNXCameraPhotos\\appconfig.json"; // путь к json
        private static string Log { get; set; }
        static void Main(string[] args)
        {
            while (true) // бесконечный цикл выполнения
            {
                string json = File.ReadAllText(JsonPath); // достаём настройки из appconfig.json
                Settings = JsonSerializer.Deserialize<Settings>(json); // дессириализуем
                Cameras = new List<NxCamera>(); // обнуляем камеры
                SQLitedbContextOptionsBuilder = new(); // создаём настройки подключения к бд
                SQLitedbContextOptionsBuilder.UseSqlite(Settings.SQLLiteConnectionString);
                MSSQLdbContextOptionsBuilder = new();
                MSSQLdbContextOptionsBuilder.UseSqlServer(Settings.MSSQLConnectionString);


                if (DateTime.Now > Settings.NextDayTime) // проверяем настал ли период запроса
                {
                    try
                    {
                        using (SQLiteDBContext sqlite = new(SQLitedbContextOptionsBuilder.Options)) // запрос списка камер
                        {

                            Cameras = sqlite.NxCameras
                            .Include(c => c.TT)
                            .Include(c => c.NxSystem)
                            .Include(c => c.CamGroup)
                            .Where(c => c.TT != null)
                            .Where(c => c.CamGroup.Id == 1)
                            .ToList();
                        }
                    }
                    catch (Exception)
                    {
                        SaveLog("ошибка запроса списка камер из SQLlite"); // логгируем ошибку
                    }
                   
                    ParallelLoopResult result = Parallel.ForEach(Cameras,SavePhoto); // параллельно запрашиваем все фото и сохраняем в БД
                    SaveLog(Log);
                    Log = "";
                    SaveLog($"Данные за {Settings.NextDayTime} успешно сохранены");  // логгируем успешное сохранение в БД
                    if (Settings.NextDayTime.Hour != Settings.End) //если следующий интервал сегодня - добовляем интервал в минутах
                    {
                        Settings.NextDayTime = Settings.NextDayTime.AddMinutes(Settings.Interval);
                    }
                    else //если следующий интервал завтра - устанавливаем следующий период на начало следующего дня
                    {
                        Settings.NextDayTime = Settings.NextDayTime.AddDays(1);
                        Settings.NextDayTime = new DateTime(Settings.NextDayTime.Year, Settings.NextDayTime.Month, Settings.NextDayTime.Day, Settings.Begin, 0, 0);
                    }                    

                }
                File.WriteAllText(JsonPath, JsonSerializer.Serialize(Settings)); //обновляем json
                TimeSpan timeOut = (Settings.NextDayTime - DateTime.Now); // считаем остаток до начала следующего периода в миллисекундах
                int timeOutMilliseconds = (int)timeOut.TotalMilliseconds; 
                if (timeOutMilliseconds < 0) { timeOutMilliseconds = 0; } // если остаток отрицательный, то приравниваем его к 0 т. к. период уже наступил
                Thread.Sleep(timeOutMilliseconds); // спим до начала следующего периода
            }
        }


        private static void SavePhoto(NxCamera cam)
        {
            module_NX.NX nx = new module_NX.NX();
            int resolution = 700; // начальное разрешение фото
            RKNet_Model.Result<System.Drawing.Bitmap> campicture = new Result<System.Drawing.Bitmap>();
            campicture.Ok = false;
            while (!campicture.Ok && resolution >= 300) // если фото нет ищем с разрешением поменьше
            {
              campicture = nx.GetCameraPicture(Settings.NextDayTime, cam, resolution);
              resolution = resolution - 100;
            }            
            if (campicture.Ok)  // если получили фото - сохраняем его в БД
            {
                try
                {
                    byte[] byteImage;
                    using (MemoryStream ms = new())
                    {
                        campicture.Data.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        byteImage = ms.ToArray();
                    }
                    PhotoCam data = new();
                    using (MSSQLDBContext msSQL = new(MSSQLdbContextOptionsBuilder.Options))
                    {
                        data.TTCode = cam.TT.Code;
                        data.TTName = cam.TT.Name;
                        data.dateTime = Settings.NextDayTime;
                        data.camId = cam.Id;
                        data.camName = cam.Name;
                        data.Image = byteImage;
                        if (cam.CamGroup != null)
                        {
                            data.groupId = cam.CamGroup.Id;
                            data.groupName = cam.CamGroup.Name;
                        }
                        msSQL.PhotoCams.Add(data);
                        msSQL.SaveChanges();
                    }
                }
                catch (Exception)
                {
                    Logging($"ошибка записи в  БД MSSQL c камеры {cam.Id} {cam.Name}");
                }              
            }
            else
            {
                Logging($"не удалось получить фото c камеры {cam.Id} {cam.Name} за {Settings.NextDayTime}");
            }
        }

        private static void Logging(string message)
        {
            Log = Log + $"{DateTime.Now} {message}\r";
        }

        private static void SaveLog(string message)
        {
            File.AppendAllText(LogPath, $"\r{DateTime.Now} {message}");
        }

    }
}
