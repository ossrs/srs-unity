using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SrsUtil
{
    public class SdpData
    {
        public string api;
        public string tid;
        public string streamurl;
        public string clientip;
        public string sdp;
    }

    public static ConfObject PrepareUrl(string webrtcUrl, string defaultPath, string defaultSchema)
    {
        var urlObject = Parse(webrtcUrl);

        var schema = urlObject.userQuery.ContainsKey("schema") ? urlObject.userQuery["schema"] : defaultSchema;

        var port = urlObject.port != -1 ? urlObject.port : 1985;
        if (schema == "https:")
        {
            port = urlObject.port != -1 ? urlObject.port : 443;
        }


        var api = urlObject.userQuery.ContainsKey("play") ? urlObject.userQuery["play"] : defaultPath;
        if (api.LastIndexOf("/") != api.Length - 1)
        {
            api += '/';
        }

        var apiUrl = schema + "//" + urlObject.server + ':' + port + api;
        foreach (var kvp in urlObject.userQuery)
        {
            if (kvp.Key != "api" && kvp.Key != "play")
            {
                apiUrl += '&' + kvp.Key + '=' + kvp.Value;
            }
        }

        apiUrl = apiUrl.Replace(api + '&', api + '?');

        var streamUrl = urlObject.url;

        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
        double millisecondsSinceEpoch = t.TotalMilliseconds;
        double result = new Random().NextDouble() * millisecondsSinceEpoch * 100;

        return new ConfObject
        {
            apiUrl = apiUrl,
            streamUrl = streamUrl,
            schema = schema,
            urlObject = urlObject,
            port = port,
            tid = ((long) result).ToString("X").Substring(0, 7)
                .ToLower()
        };
    }

    static UrlObject Parse(string url)
    {
        if (!url.Contains("://"))
        {
            url = $"rtmp://{url}";
        }

        var uri = new Uri(url);
        var vhost = uri.Host;
        var app = uri.AbsolutePath.Substring(1, uri.AbsolutePath.LastIndexOf("/") - 1);
        var stream = uri.AbsolutePath.Substring(uri.AbsolutePath.LastIndexOf("/") + 1);

        app.Replace("...vhost...", "?vhost=");
        if (app.IndexOf("?") >= 0)
        {
            var paramsString = app.Substring(app.IndexOf("?"));
            app = app.Substring(0, app.IndexOf("?"));

            if (paramsString.IndexOf("vhost=") > 0)
            {
                vhost = paramsString.Substring(paramsString.IndexOf("vhost=") + "vhost=".Length);
                if (vhost.IndexOf("&") > 0)
                {
                    vhost = vhost.Substring(0, vhost.IndexOf("&"));
                }
            }
        }

        if (uri.Host == vhost)
        {
            var regex = new Regex("^(\\d+)\\.(\\d+)\\.(\\d+)\\.(\\d+)$");
            if (regex.IsMatch(uri.Host))
            {
                vhost = "__defaultVhost__";
            }
        }

        var schema = uri.Scheme;

        var port = uri.Port;
        if (port == -1)
        {
            if (schema == "webrtc" && url.IndexOf($"webrtc: //{uri.Host}:") == 0)
            {
                port = (url.IndexOf($"webrtc: //{uri.Host}:80") == 0) ? 80 : 443;
            }

            if (schema == "http")
            {
                port = 80;
            }
            else if (schema == "https")
            {
                port = 443;
            }
            else if (schema == "rtmp")
            {
                port = 1935;
            }
        }

        var ret = new UrlObject
        {
            url = url,
            schema = schema,
            server = uri.Host,
            port = port,
            vhost = vhost,
            app = app,
            stream = stream
        };
        FillQuery(uri.Query, ret);

        if (ret.port == -1)
        {
            if (schema == "webrtc" || schema == "rtc")
            {
                if (ret.userQuery.ContainsKey("schema") && ret.userQuery["schema"] == "https")
                {
                    ret.port = 443;
                }
                else
                {
                    ret.port = 1985;
                }
            }
        }

        return ret;
    }

    static void FillQuery(string queryString, UrlObject obj)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return;
        }

        if (queryString.IndexOf("?") >= 0)
        {
            queryString = queryString.Split('?')[1];
        }

        var queries = queryString.Split('&');
        foreach (var elem in queries)
        {
            var query = elem.Split('=');
            switch (query[0])
            {
                case "url":
                    obj.url = query[1];
                    break;
                case "schema":
                    obj.schema = query[1];
                    break;
                case "server":
                    obj.server = query[1];
                    break;
                case "port":
                    obj.port = Convert.ToInt32(query[1]);
                    break;
                case "vhost":
                    obj.vhost = query[1];
                    break;
                case "app":
                    obj.app = query[1];
                    break;
                case "stream":
                    obj.stream = query[1];
                    break;
                case "domain":
                    obj.domain = query[1];
                    break;
            }

            obj.userQuery[query[0]] = query[1];
        }

        if (obj.domain != null)
        {
            obj.vhost = obj.domain;
        }
    }

    public class UrlObject
    {
        public string url;
        public string schema;
        public string server;
        public int port;
        public string vhost;
        public string app;
        public string stream;
        public Dictionary<string, string> userQuery = new Dictionary<string, string>();
        public string domain;
    }

    public class ConfObject
    {
        public string apiUrl;
        public string streamUrl;
        public string schema;
        public UrlObject urlObject;
        public int port;
        public string tid;
    }
}