﻿using JinEnergy.Engine;
using Lampac.Models.AppConf;
using Lampac.Models.LITE;
using Lampac.Models.SISI;
using Microsoft.JSInterop;
using System.Text.Json;

namespace JinEnergy
{
    public static class AppInit
    {
        #region OnInit
        [JSInvokable("initial")]
        public static bool IsInitial() { return true; }


        /// <param name="type">
        /// apk  - android
        /// web  - msx, браузер, etc
        /// cors - виджет или браузер с отключеным cors
        /// </param>
        /// <param name="conf">
        /// url   - ссылка на json с настройками
        /// </param>
        [JSInvokable("oninit")]
        async public static Task OnInit(string type, string urlconf)
        {
            if (type == "apk")
                IsAndrod = true;

            await LoadOrUpdateConfig(urlconf);

            var timer = new System.Timers.Timer(TimeSpan.FromMinutes(10));

            timer.Elapsed += async (s, e) => await LoadOrUpdateConfig(urlconf);

            timer.AutoReset = true;
            timer.Enabled = true;
        }
        #endregion

        #region LoadOrUpdateConfig
        async static Task LoadOrUpdateConfig(string urlconf)
        {
            try
            {
                if (!string.IsNullOrEmpty(urlconf))
                {
                    string? json = urlconf;
                    Shared.Model.AppInit? setings = null;

                    if (urlconf.StartsWith("http"))
                        json = await JsHttpClient.Get(urlconf + (urlconf.Contains("?") ? "&" : "?") + $"v={DateTime.Now.ToBinary()}");

                    if (json != null)
                    {
                        setings = JsonSerializer.Deserialize<Shared.Model.AppInit>(json);
                        if (setings != null)
                        {
                            conf = setings;

                            if (setings.corsehost != null)
                                Shared.Model.AppInit.corseuhost = setings.corsehost;
                        }

                        if (urlconf.Contains("bwa.to/settings/"))
                        {
                            if (!conf.KinoPub.enable)
                            {
                                string? kinopubtk = await JsHttpClient.Get($"https://bwa.to/temp/kinopubtk.txt?v={DateTime.Now.ToBinary()}", timeoutSeconds: 4);
                                if (kinopubtk != null)
                                {
                                    conf.KinoPub.enable = !string.IsNullOrEmpty(kinopubtk);
                                    conf.KinoPub.token = kinopubtk;
                                }
                            }

                            if (!conf.Filmix.pro && string.IsNullOrEmpty(conf.Filmix.token))
                            {
                                string? FXFS = await JsHttpClient.Get($"https://bwa.to/temp/hashfimix.txt?v={DateTime.Now.ToBinary()}", timeoutSeconds: 4);
                                if (FXFS != null)
                                    conf.Filmix.freehash = !string.IsNullOrEmpty(FXFS);
                            }
                        }
                    }
                }
            }
            catch { }
        }
        #endregion


        static Shared.Model.AppInit conf = new Shared.Model.AppInit();

        public static IJSRuntime? JSRuntime;

        public static string log(string msg)
        {
            JSRuntime?.InvokeVoidAsync("console.log", "BWA", msg);
            return string.Empty;
        }

        public static Random random = new Random();


        public static bool IsAndrod { get; private set; }

        public static SisiConf sisi => conf.sisi;

        public static SisiSettings BongaCams => conf.BongaCams;

        public static SisiSettings Chaturbate => conf.Chaturbate;

        public static SisiSettings Eporner => conf.Eporner;

        public static SisiSettings HQporner => conf.HQporner;

        public static SisiSettings Porntrex => conf.Porntrex;

        public static SisiSettings Xhamster => conf.Xhamster;

        public static SisiSettings Xnxx => conf.Xnxx;

        public static SisiSettings Xvideos => conf.Xvideos;

        public static SisiSettings PornHub => conf.PornHub;

        public static SisiSettings Ebalovo => conf.Ebalovo;

        public static SisiSettings Spankbang => conf.Spankbang;


        public static OnlinesSettings Kinobase => conf.Kinobase;

        public static RezkaSettings Rezka => conf.Rezka;

        public static RezkaSettings Voidboost => conf.Voidboost;

        public static OnlinesSettings Collaps => conf.Collaps;

        public static OnlinesSettings Ashdi => conf.Ashdi;

        public static OnlinesSettings Eneyida => conf.Eneyida;

        public static OnlinesSettings Kinokrad => conf.Kinokrad;

        public static OnlinesSettings Kinotochka => conf.Kinotochka;

        public static OnlinesSettings Redheadsound => conf.Redheadsound;

        public static OnlinesSettings Kinoprofi => conf.Kinoprofi;

        public static OnlinesSettings Lostfilmhd => conf.Lostfilmhd;

        public static FilmixSettings Filmix => conf.Filmix;

        public static FilmixSettings FilmixPartner => conf.FilmixPartner;

        public static OnlinesSettings Zetflix => conf.Zetflix;

        public static OnlinesSettings VideoDB => conf.VideoDB;

        public static OnlinesSettings CDNmovies => conf.CDNmovies;

        public static OnlinesSettings VDBmovies => conf.VDBmovies;


        public static OnlinesSettings VCDN => conf.VCDN;

        public static OnlinesSettings VoKino => conf.VoKino;

        public static OnlinesSettings VideoAPI => conf.VideoAPI;

        public static IframeVideoSettings IframeVideo => conf.IframeVideo;

        public static OnlinesSettings HDVB => conf.HDVB;

        public static OnlinesSettings Seasonvar => conf.Seasonvar;

        public static KinoPubSettings KinoPub => conf.KinoPub;

        public static BazonSettings Bazon => conf.Bazon;

        public static AllohaSettings Alloha => conf.Alloha;

        public static KodikSettings Kodik => conf.Kodik;


        public static OnlinesSettings AnilibriaOnline => conf.AnilibriaOnline;

        public static OnlinesSettings AniMedia => conf.AniMedia;

        public static OnlinesSettings AnimeGo => conf.AnimeGo;

        public static OnlinesSettings Animevost => conf.Animevost;

        public static OnlinesSettings Animebesst => conf.Animebesst;
    }
}
