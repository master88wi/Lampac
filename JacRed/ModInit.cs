﻿using JacRed.Engine;
using JacRed.Models.AppConf;
using Lampac.Models.AppConf;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;

namespace Jackett
{
    public class ModInit
    {
        #region ModInit
        static (ModInit, DateTime) cacheconf = default;

        public static ModInit conf
        {
            get
            {
                if (cacheconf.Item1 == null)
                {
                    if (!File.Exists("module/JacRed.conf"))
                        return new ModInit();
                }

                var lastWriteTime = File.GetLastWriteTime("module/JacRed.conf");

                if (cacheconf.Item2 != lastWriteTime)
                {
                    var jss = new JsonSerializerSettings { Error = (se, ev) => 
                    { 
                        ev.ErrorContext.Handled = true; 
                        Console.WriteLine("module/JacRed.conf - " + ev.ErrorContext.Error + "\n\n"); 
                    }};

                    string json = File.ReadAllText("module/JacRed.conf");

                    if (json.Contains("\"version\""))
                    {
                        cacheconf.Item1 = JsonConvert.DeserializeObject<ModInit>(json, jss);
                    }
                    else
                    {
                        cacheconf.Item1 = new ModInit() { Red = JsonConvert.DeserializeObject<RedConf>(json, jss) };
                    }

                    if (cacheconf.Item1 == null)
                        cacheconf.Item1 = new ModInit();

                    cacheconf.Item2 = lastWriteTime;
                }

                return cacheconf.Item1;
            }
        }
        #endregion

        public static void loaded()
        {
            Directory.CreateDirectory("cache/jacred");
            Directory.CreateDirectory("cache/jackett");
            Directory.CreateDirectory("cache/torrent");

            ThreadPool.QueueUserWorkItem(async _ => await SyncCron.Run());
            ThreadPool.QueueUserWorkItem(async _ => await FileDB.Cron());
        }


        /// <summary>
        /// red
        /// jackett
        /// dynamic
        /// </summary>
        public string typesearch = "red";

        public RedConf Red = new RedConf();

        public JacConf Jackett = new JacConf();
    }
}
