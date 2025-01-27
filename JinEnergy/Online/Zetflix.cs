﻿using JinEnergy.Engine;
using Microsoft.JSInterop;
using Shared.Engine.Online;

namespace JinEnergy.Online
{
    public class ZetflixController : BaseController
    {
        [JSInvokable("lite/zetflix")]
        async public static ValueTask<string> Index(string args)
        {
            var arg = defaultArgs(args);
            int s = int.Parse(parse_arg("s", args) ?? "-1");
            string? t = parse_arg("t", args);

            if (arg.kinopoisk_id == 0)
                return EmptyError("arg");

            var oninvk = new ZetflixInvoke
            (
               null,
               AppInit.Zetflix.corsHost(),
               (url, head) => JsHttpClient.Get(url.Contains(".cub.watch") ? url : AppInit.Zetflix.corsHost(url), addHeaders: head),
               onstreamtofile => onstreamtofile
               //AppInit.log
            );

            var content = await InvokeCache(arg.id, $"zetfix:view:{arg.kinopoisk_id}:{s}", () => oninvk.Embed(arg.kinopoisk_id, s));
            if (content?.pl == null)
                return EmptyError("content");

            int number_of_seasons = 1;
            if (!content.movie && s == -1 && arg.id > 0)
                number_of_seasons = await InvStructCache(arg.id, $"zetfix:number_of_seasons:{arg.kinopoisk_id}", () => oninvk.number_of_seasons(arg.id));

            return oninvk.Html(content, number_of_seasons, arg.kinopoisk_id, arg.title, arg.original_title, t, s);
        }
    }
}
