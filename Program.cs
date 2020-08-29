using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IG_MDs {
    class Program {
        const string USER = "";
        const string PASS = "";
        Dictionary<string, string> futureHeaders = new Dictionary<string, string>();
        static void Main(string[] args) {
            HttpClient client = new HttpClient();
            getCookies(client);
        }
        public static void getCookies(HttpClient client) {
            try {

                client.DefaultRequestHeaders.Add("scheme", "https");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.Add("accept-encoding", "deflate, br");
                client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
                client.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
                client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.120 Safari/537.36");
                client.DefaultRequestHeaders.Add("X-Instagram-AJAX", "1");
                client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("x-csrftoken", "");
                client.DefaultRequestHeaders.Add("X-Instagram-AJAX", "");
                var response = client.GetAsync("https://www.instagram.com/accounts/login/?source=auth_switcher").Result;
                if (response.IsSuccessStatusCode) {
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    var indexx = responseString.IndexOf("\"csrf_token\":\"") + 14;
                    var csrf = responseString.Substring(indexx, responseString.Length - indexx);
                    csrf = csrf.Substring(0, csrf.IndexOf('"'));
                    client.DefaultRequestHeaders.Add("x-csrftoken", csrf);
                    var form = new Dictionary<string, string>{
            { "username", USER },
            { "enc_password", PASS },
            //FAILS BC THE PASS IS NOT ENCRYPTED
            { "queryParams", "{\"source\":\"auth_switcher\"}" },
            { "optIntoOneTap", "false" },
            };
                    var content = new FormUrlEncodedContent(form);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                    response = client.PostAsync("https://www.instagram.com/accounts/login/ajax/", content).Result;
                    if (response.IsSuccessStatusCode) {
                        responseString = response.Content.ReadAsStringAsync().Result;
                        var setCookies = response.Headers.GetValues("set-cookie");
                        var substr = "";
                        foreach (string element in setCookies) {
                            substr += element.Substring(0, element.IndexOf(";") + 1);
                            if (element.IndexOf("csrftoken") != -1) {
                                client.DefaultRequestHeaders.Add("x-csrftoken", element.Substring(element.IndexOf("=") + 1, element.IndexOf(";") - element.IndexOf("=")));
                            }
                        }
                        client.DefaultRequestHeaders.Add("cookie", substr);
                        getMds(client);
                    }

                }
            } catch (Exception e) {
                Console.WriteLine(e);

            }

        }

        public static void getMds(HttpClient client) {
            try {
                var response = client.GetAsync("https://www.instagram.com/direct_v2/web/inbox/?persistentBadging=true&folder=0&limit=100&thread_message_limit=18446744073709551615").Result;
                if (response.IsSuccessStatusCode) {
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    dynamic body = JsonConvert.DeserializeObject<dynamic>(responseString);
                    string texto;
                    dynamic items;
                    Object Falsetrue;
                    for (int i = 0; i < body["inbox"]["threads"].Length; i++) {
                        Console.WriteLine(body["inbox"]["threads"][i]["users"][0]["username"]);
                        items = body["inbox"]["threads"][i]["items"];
                        for (int j = 0; j < items.Length; j++) {
                            switch (items[j]["item_type"]) {
                                case "media_share":
                                    if (items[j]["media_share"]["media_type"] == 1) {
                                        Console.WriteLine('\t' + items[j]["media_share"]["image_versions2"]["candidates"][0]["url"]);
                                    } else if (items[j]["media_share"]["media_type"] == 2) {
                                        Console.WriteLine('\t' + items[j]["media_share"]["video_versions"][0]["url"]);
                                    } else if (items[j]["media_share"]["media_type"] == 8) {
                                        for (int k = 0; k < items[j]["media_share"]["carousel_media"].Length; k++) {
                                            Console.WriteLine("\t\t" + items[j]["media_share"]["carousel_media"][k]["image_versions2"]["candidates"][0]["url"]);
                                        }
                                        texto = items[j]["media_share"].TryGetValue("text", out Falsetrue) ? items[j]["media_share"]["text"] : "";
                                        Console.WriteLine("\t\t ---- " + texto);
                                    }
                                    break;
                                case "reel_share":
                                    texto = items[j]["reel_share"].TryGetValue("text", out Falsetrue) ? items[j]["reel_share"]["text"] : "";
                                    if (items[j]["reel_share"]["type"] == "reaction") {
                                        Console.WriteLine("\t Ha reaccionado a tu historia");
                                    } else {
                                        if (items[j]["reel_share"]["media"]["expiring_at"] == 0) {
                                            Console.WriteLine("\t Ha expirado la historia");
                                        } else {
                                            if (items[j]["reel_share"]["media"]["media_type"] == 1) {
                                                Console.WriteLine('\t' + items[j]["reel_share"]["media"]["image_versions2"]["candidates"][0]["url"] + " ---- " + texto);
                                            } else if (items[j]["reel_share"]["media"]["media_type"] == 2) {
                                                Console.WriteLine('\t' + items[j]["reel_share"]["media"]["video_versions"][0]["url"] + " ---- " + texto);
                                            } else if (items[j]["reel_share"]["media"]["expiring_at"] == 0) {
                                                Console.WriteLine("\t Has respondido a su historia: " + texto);
                                            }
                                        }
                                    }
                                    break;
                                case "story_share":
                                    texto = items[j]["story_share"]["text"];
                                    if (!items[j]["story_share"].TryGetValue("reason", out Falsetrue)) {
                                        if (items[j]["story_share"]["media"]["media_type"] == 1) {
                                            Console.WriteLine('\t' + items[j]["story_share"]["media"]["image_versions2"]["candidates"][0]["url"] + " ---- " + texto);
                                        } else if (items[j]["story_share"]["media"]["media_type"] == 2) {
                                            Console.WriteLine('\t' + items[j]["story_share"]["media"]["video_versions"][0]["url"] + " ---- " + texto);
                                        }
                                    } else {
                                        Console.WriteLine("Historia no disponible. " + texto);
                                    }
                                    Console.WriteLine(texto);
                                    break;
                                case "voice_media":
                                    Console.WriteLine(":\t" + items[j]["voice_media"]["media"]["audio"]["audio_src"]);
                                    break;
                                case "raven_media":
                                    Console.WriteLine("\t VIDEO VISTO.");
                                    break;
                                case "media":
                                    if (items[j]["media"]["media_type"] == 1) {
                                        Console.WriteLine('\t' + items[j]["media"]["image_versions2"]["candidates"][0]["url"]);
                                    } else if (items[j]["media"]["media_type"] == 2) {
                                        Console.WriteLine('\t' + items[j]["media"]["video_versions"][0]["url"]);
                                    } else if (items[j]["media"]["media_type"] == 8) {
                                        for (int k = 0; k < items[j]["media"]["carousel_media"].Length; k++) {
                                            Console.WriteLine("\t\t" + items[j]["media"]["carousel_media"][k]["image_versions2"]["candidates"][0]["url"]);
                                        }
                                        Console.WriteLine("\t\t ---- " + items[j]["reel_share"]["text"]);
                                    }
                                    break;
                                case "action_log":
                                    Console.WriteLine("\t" + items[j]["action_log"]["description"]);
                                    break;
                                case "video_call_event":
                                    Console.WriteLine("\t VIDEOLLAMADA REALIZADA.");
                                    break;
                                case "text":
                                    Console.WriteLine("\t" + items[j]["text"]);
                                    break;
                                case "placeholder":
                                    Console.WriteLine("\t TE HA ENVIADO UN IGTV.");
                                    break;
                                default:
                                    Console.WriteLine("\t TIPO NO DEFINIDO: " + items[j]["item_type"]);
                                    break;
                            }
                        }
                    }
                }
            } catch(Exception e) {
                Console.WriteLine("Error: " + e);
            }
        }
    }
}
