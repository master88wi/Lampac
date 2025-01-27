﻿using Shared.Model.Online.VideoDB;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Shared.Engine.Online
{
    public class VideoDBInvoke
    {
        #region VideoDBInvoke
        string? host;
        string apihost;
        Func<string, string> onstreamfile;
        Func<string, string>? onlog;
        Func<string, List<(string name, string val)>?, ValueTask<string?>> onget;

        public VideoDBInvoke(string? host, string? apihost, Func<string, List<(string name, string val)>?, ValueTask<string?>> onget, Func<string, string> onstreamfile, Func<string, string>? onlog = null)
        {
            this.host = host != null ? $"{host}/" : null;
            this.apihost = apihost!;
            this.onstreamfile = onstreamfile;
            this.onlog = onlog;
            this.onget = onget;
        }
        #endregion

        #region Embed
        public async ValueTask<EmbedModel?> Embed(long kinopoisk_id, int serial)
        {
            string? html = await onget.Invoke($"{apihost}/iplayer/videodb.php?kp={kinopoisk_id}" + (serial > 0 ? "&series=true" : ""), new List<(string name, string val)>()
            {
                ("referer", "https://www.google.com/")
                //("cookie", "invite=a246a3f46c82fe439a45c3dbbbb24ad5"),
                //("referer", $"{apihost}/")
            });

            onlog?.Invoke(html ?? "html null");

            string? file = new Regex("file:([^\n\r]+,\\])").Match(html ?? "").Groups[1].Value;
            if (string.IsNullOrWhiteSpace(file))
                return null;

            file = Regex.Replace(file.Trim(), "(\\{|, )([a-z]+): ?", "$1\"$2\":")
                        .Replace("},]", "}]");

            onlog?.Invoke("file: " + file);
            var pl = JsonSerializer.Deserialize<List<RootObject>>(file);
            if (pl == null || pl.Count == 0) 
                return null;

            onlog?.Invoke("pl " + pl.Count);
            return new EmbedModel() { pl = pl, movie = !file.Contains("\"comment\":") };
        }
        #endregion

        #region Html
        public string Html(EmbedModel root, long kinopoisk_id, string? title, string? original_title, string? t, int s, int sid)
        {
            bool firstjson = true;
            var html = new StringBuilder();
            html.Append("<div class=\"videos__line\">");

            if (root.movie)
            {
                #region Фильм
                foreach (var pl in root.pl)
                {
                    string? name = pl.title;
                    string? file = pl.file;

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(file))
                        continue;

                    #region streansquality
                    var streams = new List<(string link, string quality)>() { Capacity = 4 };

                    foreach (Match m in Regex.Matches(file, $"\\[(1080|720|480|360)p?\\]([^\\[\\|,\n\r\t ]+\\.(mp4|m3u8))"))
                    {
                        string link = m.Groups[2].Value;
                        if (string.IsNullOrEmpty(link))
                            continue;

                        streams.Insert(0, (onstreamfile.Invoke(link), $"{m.Groups[1].Value}p"));
                    }

                    if (streams.Count == 0)
                        continue;

                    string streansquality = "\"quality\": {" + string.Join(",", streams.Select(s => $"\"{s.quality}\":\"{s.link}\"")) + "}";
                    #endregion

                    #region subtitle
                    string subtitles = string.Empty;

                    try
                    {
                        int subx = 1;
                        var subs = pl.subtitle;
                        if (subs != null)
                        {
                            var subbuilder = new StringBuilder();

                            foreach (string cc in subs.Split(","))
                            {
                                if (string.IsNullOrWhiteSpace(cc) || !cc.EndsWith(".srt"))
                                    continue;

                                string suburl = onstreamfile.Invoke(cc);
                                subbuilder.Append("{\"label\": \"" + $"sub #{subx}" + "\",\"url\": \"" + suburl + "\"},");
                                subx++;
                            }

                            if (subbuilder.Length > 0)
                                subtitles = Regex.Replace(subbuilder.ToString(), ",$", "");
                        }
                    }
                    catch { }
                    #endregion

                    html.Append("<div class=\"videos__item videos__movie selector " + (firstjson ? "focused" : "") + "\" media=\"\" data-json='{\"method\":\"play\",\"url\":\"" + streams[0].link + "\",\"title\":\"" + (title ?? original_title) + "\", " + streansquality + ", \"subtitles\": [" + subtitles + "]}'><div class=\"videos__item-imgbox videos__movie-imgbox\"></div><div class=\"videos__item-title\">" + name + "</div></div>");
                    firstjson = false;
                }
                #endregion
            }
            else
            {
                #region Сериал
                string? enc_title = HttpUtility.UrlEncode(title);
                string? enc_original_title = HttpUtility.UrlEncode(original_title);

                if (s == -1)
                {
                    for (int i = 0; i < root.pl.Count; i++)
                    {
                        string? name = root.pl?[i].title;
                        if (name == null)
                            continue;

                        string season = Regex.Match(name, "^([0-9]+)").Groups[1].Value;
                        if (string.IsNullOrEmpty(season))
                            continue;

                        string link = host + $"lite/videodb?kinopoisk_id={kinopoisk_id}&title={enc_title}&original_title={enc_original_title}&s={season}&sid={i}";

                        html.Append("<div class=\"videos__item videos__season selector " + (firstjson ? "focused" : "") + "\" data-json='{\"method\":\"link\",\"url\":\"" + link + "\"}'><div class=\"videos__season-layers\"></div><div class=\"videos__item-imgbox videos__season-imgbox\"><div class=\"videos__item-title videos__season-title\">" + name + "</div></div></div>");
                        firstjson = false;
                    }
                }
                else
                {
                    var season = root.pl?[sid]?.folder;
                    if (season == null)
                        return string.Empty;

                    var hashvoices = new HashSet<string>();
                    var htmlvoices = new StringBuilder();
                    var htmlepisodes = new StringBuilder();

                    foreach (var episode in season)
                    {
                        var episodes = episode?.folder;
                        if (episodes == null || episodes.Count == 0)
                            continue;

                        foreach (var pl in episodes)
                        {
                            string? perevod = pl?.comment;
                            if (perevod != null && string.IsNullOrEmpty(t))
                                t = perevod;

                            #region Переводы
                            if (!hashvoices.Contains(perevod))
                            {
                                hashvoices.Add(perevod);
                                string link = host + $"lite/videodb?kinopoisk_id={kinopoisk_id}&title={enc_title}&original_title={enc_original_title}&s={s}&sid={sid}&t={HttpUtility.UrlEncode(perevod)}";
                                string active = t == perevod ? "active" : "";

                                htmlvoices.Append("<div class=\"videos__button selector " + active + "\" data-json='{\"method\":\"link\",\"url\":\"" + link + "\"}'>" + perevod + "</div>");
                            }
                            #endregion

                            if (perevod != t)
                                continue;

                            string? name = episode?.title;
                            string? file = pl?.file;

                            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(file))
                                continue;

                            var streams = new List<(string link, string quality)>() { Capacity = 4 };

                            foreach (Match m in Regex.Matches(file, $"\\[(1080|720|480|360)p?\\]([^\\[\\|,\n\r\t ]+\\.(mp4|m3u8))"))
                            {
                                string link = m.Groups[2].Value;
                                if (string.IsNullOrEmpty(link))
                                    continue;

                                streams.Insert(0, (onstreamfile.Invoke(link), $"{m.Groups[1].Value}p"));
                            }

                            if (streams.Count == 0)
                                continue;

                            string streansquality = "\"quality\": {" + string.Join(",", streams.Select(s => $"\"{s.quality}\":\"{s.link}\"")) + "}";

                            htmlepisodes.Append("<div class=\"videos__item videos__movie selector " + (firstjson ? "focused" : "") + "\" media=\"\" s=\"" + s + "\" e=\"" + Regex.Match(name, "^([0-9]+)").Groups[1].Value + "\" data-json='{\"method\":\"play\",\"url\":\"" + streams[0].link + "\",\"title\":\"" + $"{title ?? original_title} ({name})" + "\", " + streansquality + "}'><div class=\"videos__item-imgbox videos__movie-imgbox\"></div><div class=\"videos__item-title\">" + name + "</div></div>");
                            firstjson = false;
                        }
                    }

                    html.Append(htmlvoices.ToString() + "</div><div class=\"videos__line\">" + htmlepisodes.ToString());
                }
                #endregion
            }

            return html.ToString() + "</div>";
        }
        #endregion
    }
}
