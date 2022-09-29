﻿using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using Haley.Rest;
using Haley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using RestSharp;
using RestSharp.Authenticators;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;
using System.Security.Cryptography;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args) {
            Console.WriteLine("Hello, World!");
            //string test = $@"23452352@#$%^&*@!%!&";
            //Console.WriteLine($@"Actual - {test}");
            //var URIEscapted = Uri.EscapeUriString(test);
            //var DataEscaped = Uri.EscapeDataString(test);
            //Console.WriteLine($@"URI Escaped - {URIEscapted}");
            //Console.WriteLine($@"DATA Escaped - {DataEscaped}");
            //Console.WriteLine($@"URI DOUBLE Escaped - {Uri.EscapeUriString(URIEscapted)}");
            //Console.WriteLine($@"DATA DOUBLE Escaped - {Uri.EscapeDataString(DataEscaped)}");
            var res1 = CallMethod().Result;
            //var res3 = DirectHTTPCall().Result;
            //var res2 = RestSharpCall().Result;
        }
        static async Task<bool> DirectHTTPCall() {

            var clientHandler = new HttpClientHandler {
                //UseCookies = false,
            };

            var client = new HttpClient(clientHandler);
            var request = new HttpRequestMessage {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://daep.withbc.com/oauth/request_token"),
                Headers =
                {
                    //{ "cookie", $@"awesome_cookie=1663577011.57" },
                    { "Authorization", $@"OAuth oauth_consumer_key=""4579bfc5-0671-4087-bed3-00a41b5cff8c"", oauth_nonce=""6bfeyvo1QBahRF2kDs7YtNgJYiE1sSyM"", oauth_signature=""YCDXRzyhAsA%2BVnyu7j%2FLGkoTm0Q%3D"", oauth_signature_method=""HMAC-SHA1"", oauth_timestamp=""1664431675"", oauth_version=""1.0""" },
                }
            };
            using (var response = await client.SendAsync(request)) {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
            }
            return true;
        }
        static async Task<bool> CallMethod() {
            try {
                var client = new FluentClient($@"https://daep.withbc.com");
                //client.AddRequestHeaders("Accept", "*/*");
                //client.AddRequestHeaders("User-Agent", "HaleyRest_V1.9");
                var _res3 = await client.SendAsync(Haley.Enums.Method.POST);
                var _res2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/"));

                client.SetAuthenticator(new Haley.Utils.OAuth1Authenticator("4579bfc5-0671-4087-bed3-00a41b5cff8c", "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5")); //Set for all 
                var _res4 = await client.WithEndPoint($@"oauth/request_token").InheritAuthentication().SetAuthParam(new OAuth1TokenParam() {RequestType = OAuthRequestType.RequestToken}).SendAsync(Haley.Enums.Method.POST);

                //var authheader = new Haley.Utils.OAuth1Authenticator().GetAuthorizationHeader(new OAuthToken("4579bfc5-0671-4087-bed3-00a41b5cff8c", "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5"), OAuthRequestType.RequestToken,new OAuthRequest() { Method = Haley.Enums.Method.POST,RequestURL = $@"https://daep.withbc.com/oauth/request_token" });
                ////client.AddRequestAuthentication("oauth_consumer_key=\"4579bfc5-0671-4087-bed3-00a41b5cff8c\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"1663965208\",oauth_nonce=\"sxxdWnS82RP\",oauth_version=\"1.0\",oauth_signature=\"zEJeuzHEeeLp9HK7pZ%2FERcWItvI%3D\"", "OAuth");
                ////var request = client.AddRequestHeaders("Authorization", "OAuth oauth_consumer_key=\"4579bfc5-0671-4087-bed3-00a41b5cff8c\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"1664108209\",oauth_nonce=\"SccRLuUoZmd\",oauth_version=\"1.0\",oauth_signature=\"fpxOyBvbOf/tt87AIO29WuRoJf8=\"");
                ////client.AddRequestHeaders("Cookie", "awesome_cookie=1663964753.45");
                ////client.AddRequestHeaders("Accept", "text/plain,application/json,application/xml,text/javascript");

                //client.AddRequestHeaders("Cookie", $@"awesome_cookie=1664110011.57; Secure ; path=/");
                ////client.AddRequestHeaders("Host", "daep.withbc.com");
                //client.AddRequestHeaders("Authorization", $@"OAuth {authheader}");
                ////client.AddRequestHeaders("Connection", "keep-alive");
                //var _res = await client.PostObjectAsync("oauth/request_token", null);
                ////Console.WriteLine(_res.OriginalResponse.ToString());
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return false;
            }

        }

        static async Task<bool> RestSharpCall() {
            var client = new RestClient($@"https://daep.withbc.com/")
            {
                Authenticator = new RestSharp.Authenticators.OAuth1Authenticator()
            };
            client.Authenticator = RestSharp.Authenticators.OAuth1Authenticator.ForRequestToken("4579bfc5-0671-4087-bed3-00a41b5cff8c", "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5");
            var request = new RestSharp.RestRequest("oauth/request_token", RestSharp.Method.Post);
            var param = request.Parameters;
            var response = await client.PostAsync(request);
            return true;
       
        }
    }
}