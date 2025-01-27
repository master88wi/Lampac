﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lampac.Engine;
using Lampac.Engine.CORE;
using Lampac.Models.JAC.AniLibria;
using System.Collections.Concurrent;
using Lampac.Models.JAC;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Lampac.Engine.Parse;
using Shared;
using Shared.Engine.CORE;

namespace Lampac.Controllers.JAC
{
    [Route("anilibria/[action]")]
    public class AniLibriaController : BaseController
    {
        #region parseMagnet
        async public Task<ActionResult> parseMagnet(string url, string code)
        {
            if (!AppInit.conf.Anilibria.enable)
                return Content("disable");

            string key = $"anilibria:parseMagnet:{url}";
            if (Startup.memoryCache.TryGetValue(key, out byte[] _m))
                return File(_m, "application/x-bittorrent");

            var proxyManager = new ProxyManager("anilibria", AppInit.conf.Anilibria);

            byte[] _t = await HttpClient.Download($"{AppInit.conf.Anilibria.host}/{url}", referer: $"{AppInit.conf.Anilibria.host}/release/{code}.html", timeoutSeconds: 10, proxy: proxyManager.Get());
            if (_t != null && BencodeTo.Magnet(_t) != null)
            {
                if (AppInit.conf.jac.cache)
                {
                    await TorrentCache.Write(key, _t);
                    Startup.memoryCache.Set(key, _t, DateTime.Now.AddMinutes(Math.Max(1, AppInit.conf.jac.torrentCacheToMinutes)));
                }

                return File(_t, "application/x-bittorrent");
            }
            else if (await TorrentCache.Read(key) is var tcache && tcache.cache)
            {
                return File(tcache.torrent, "application/x-bittorrent");
            }

            proxyManager.Refresh();
            return Content("error");
        }
        #endregion

        #region parsePage
        async public static Task<bool> parsePage(string host, ConcurrentBag<TorrentDetails> torrents, string query)
        {
            string memkey = $"anilibria:{query}";

            if (!AppInit.conf.Anilibria.enable || Startup.memoryCache.TryGetValue($"{memkey}:error", out _))
                return false;

            #region Кеш поиска
            if (!Startup.memoryCache.TryGetValue(memkey, out List<RootObject> roots))
            {
                var proxyManager = new ProxyManager("anilibria", AppInit.conf.Anilibria);

                roots = await HttpClient.Get<List<RootObject>>("https://api.anilibria.tv/v2/searchTitles?search=" + HttpUtility.UrlEncode(query), timeoutSeconds: AppInit.conf.jac.timeoutSeconds, proxy: proxyManager.Get(), IgnoreDeserializeObject: true);
                if (roots == null || roots.Count == 0)
                {
                    if (AppInit.conf.jac.emptycache && AppInit.conf.jac.cache)
                        Startup.memoryCache.Set($"{memkey}:error", 0, DateTime.Now.AddMinutes(Math.Max(1, AppInit.conf.jac.htmlCacheToMinutes)));

                    proxyManager.Refresh();
                    return false;
                }

                if (AppInit.conf.jac.cache)
                    Startup.memoryCache.Set(memkey, roots, DateTime.Now.AddMinutes(Math.Max(1, AppInit.conf.jac.htmlCacheToMinutes)));
            }
            #endregion

            foreach (var root in roots)
            {
                DateTime createTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(root.last_change > root.updated ? root.last_change : root.updated);

                foreach (var torrent in root.torrents.list)
                {
                    if (string.IsNullOrWhiteSpace(root.code) || 480 >= torrent.quality.resolution && string.IsNullOrWhiteSpace(torrent.quality.encoder) && string.IsNullOrWhiteSpace(torrent.url))
                        continue;

                    torrents.Add(new TorrentDetails()
                    {
                        trackerName = "anilibria",
                        types = new string[] { "anime" },
                        url = $"{AppInit.conf.Anilibria.host}/release/{root.code}.html",
                        title = $"{root.names.ru} / {root.names.en} {root.season.year} (s{root.season.code}, e{torrent.series.@string}) [{torrent.quality.@string}]",
                        sid = torrent.seeders,
                        pir = torrent.leechers,
                        createTime = createTime,
                        parselink = $"{host}/anilibria/parsemagnet?url={HttpUtility.UrlEncode(torrent.url)}&code={root.code}",
                        sizeName = tParse.BytesToString(torrent.total_size),
                        name = root.names.ru,
                        originalname = root.names.en,
                        relased = root.season.year
                    });
                }
            }


            return true;
        }
        #endregion
    }
}
