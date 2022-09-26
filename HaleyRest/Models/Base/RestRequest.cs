﻿using Haley.Enums;
using Haley.Utils;
using System;
using Haley.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Concurrent;
using Haley.Models;
using Trs =System.Timers;
using System.Web;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Haley.Models
{
    //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body

    /// <summary>
    /// A simple straightforward HTTPClient Wrapper.
    /// </summary>
    public sealed class RestRequest : RestBase, IRequest
    {
        HttpRequestMessage _request = null;  //Prio-1
        HttpContent _content = null; //Prio-2
        IEnumerable<RequestObject> _requestObjects = new List<RequestObject>();//Prio-3

        #region Attributes
        string _boundary = "----CustomBoundary" + DateTime.Now.Ticks.ToString("x");
        CancellationToken? _cancellation_token = null;
        bool _inherit_headers = false;
        bool _inherit_authentication = false;
       
        public IClient Client { get; private set; }
        #endregion

        #region Constructors
        public RestRequest(string end_point_url, IClient client) : base(end_point_url) {
            Client = client;
        }
        public RestRequest(string end_point_url) : this(end_point_url,null) { }
        public RestRequest() : this(string.Empty, null) { }
        #endregion
        #region Base Fluent Methods
        public IRequest SetClient(IClient client) {
            this.Client = client;
            return this;
        }

        public IRequest InheritHeaders() {
            _inherit_headers = true;
            return this;
        }

        public IRequest InheritAuthentication() {
            _inherit_authentication = true;
            return this;
        }

        public override IRestBase WithEndPoint(string resource_url_endpoint) {
            URL = ParseURI(resource_url_endpoint).pathQuery; //What if we needed to use full URL?
            return this;
        }

        public override IRestBase AddCancellationToken(CancellationToken cancellation_token) {
            this._cancellation_token = cancellation_token;
            return this;
        }
        #endregion

        #region Get Methods
        public override async Task<RestResponse> GetAsync() {
            return await GetAsync<string>();
        }
        public override async Task<RestResponse<T>> GetAsync<T>() {
            var _response = await SendAsync(Method.GET);
            RestResponse<T> result = new RestResponse<T>();
            _response.MapProperties(result);
            if (_response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(result.Content)) {
                try {
                    if (typeof(T) == typeof(string)) {
                        result.Content = result.Content as T;
                    }
                    else {
                        result.Content = JsonSerializer.Deserialize<T>(result.Content);
                    }
                }
                catch (Exception) {
                    result.Content = null; //Since it is a class, it should be nullable.
                }
            }
            return result;
        }
       
        public override async Task<IResponse> PostAsync() {
            return await SendAsync(Method.POST);
        }
        public override Task<IResponse> PutAsync() {
            return await SendAsync(Method.PUT);
        }
        public override Task<IResponse> DeleteAsync() {
            return await SendAsync(Method.DELETE);
        }
        public override Task<IResponse> SendAsync(Method method) {
            throw new NotImplementedException();
        }

        #endregion

        #region GetSerialized Methods
        public async Task<RestResponse<T>> GetAsync<T>(string resource_url) where T : class {
            return await GetAsync<T>(resource_url, null);
        }
        public async Task<RestResponse<T>> GetAsync<T>(string resource_url, QueryParam parameter) where T : class {
            List<QueryParam> queries = new List<QueryParam>();
            queries.Add(parameter);
            return await GetByParamsAsync<T>(resource_url, queries);
        }
        public async Task<RestResponse<T>> GetByParamsAsync<T>(string resource_url, IEnumerable<QueryParam> parameters) where T : class {
            var _response = await SendObjectsAsync(resource_url, parameters, Method.GET);
            RestResponse<T> result = new RestResponse<T>();
            //_response.CopyTo(result);
            _response.MapProperties(result);
            if (_response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(result.Content)) {
                try {
                    if (typeof(T) == typeof(string)) {
                        result.Content = result.Content as T;
                    } else {
                        result.Content = JsonSerializer.Deserialize<T>(result.Content);
                    }
                } catch (Exception) {
                    result.Content = null; //Since it is a class, it should be nullable.
                }
            }
            return result;
        }
        #endregion

        #region Post Methods
        public async Task<IResponse> PostObjectAsync(string resource_url, RequestObject parameter)
        {
            return await PostObjectsAsync(resource_url, new List<RequestObject>() { parameter });
        }
        public async Task<IResponse> PostObjectsAsync(string resource_url, IEnumerable<RequestObject> parameters)
        {
            return await SendObjectsAsync(resource_url, parameters, Method.POST);
        }
        #endregion

        #region Delete Methods
        public async Task<IResponse> DeleteObjectAsync(string resource_url, QueryParam param) {
            return await DeleteObjectsAsync(resource_url, new List<QueryParam>() { param });
        }
        public async Task<IResponse> DeleteObjectsAsync(string resource_url, IEnumerable<QueryParam> parameters) {
            return await SendObjectsAsync(resource_url, parameters, Method.DELETE);
        }
        #endregion
        #region region Request Creation
        public override IRestBase WithQuery(QueryParam param) {
            return WithParameter(param);
        }
        public override IRestBase WithQueries(IEnumerable<QueryParam> parameters) {
            return WithParameters(parameters);
        }
        public override IRestBase WithBody(object content, bool is_serialized, BodyContentType content_type) {
            return WithParameter(new RawBodyRequest(content,is_serialized,content_type));
        }
        public override IRestBase WithParameter(RequestObject param) {
            return WithParameters(new List<RequestObject>() { param });
        }
        public override IRestBase WithParameters(IEnumerable<RequestObject> parameters) {
            _requestObjects = parameters;
            return this;
        }
        public override IRestBase WithContent(HttpContent content) {
            _content = content;
            return this;
        }
        #endregion

        #region Send Methods
        public async Task<IResponse> SendAsync(string url, object content, Method method, bool is_serialized, BodyContentType content_type = BodyContentType.StringContent)
        {
            return await SendObjectAsync(url, new RawBodyRequest(content, is_serialized, content_type),method);
        }
        public async Task<IResponse> SendObjectAsync(string url, RequestObject param, Method method)
        {
            //Just add this single param as a list to the send method.
            return await SendObjectsAsync(url, new List<RequestObject>() { param }, method);
        }
        public async Task<IResponse> SendObjectsAsync(string url, IEnumerable<RequestObject> paramList, Method method) {
            string inputURL = url;
            var processedInputs = ConverToHttpContent(inputURL, paramList, method); //Put required url queries, bodies etc.
            return await SendAsync(processedInputs.url, processedInputs.content, method);
        }
        public async Task<IResponse> SendAsync(string url, HttpContent content, Method method) {

            WriteLog(LogLevel.Information, $@"Initiating a {method} request to {url} with base url {BaseURI}");
            //1. Here, we do not add anything to the URL or Content.
            //2. We just validate the URl and get the path and query part.
            //3. Add request headers and Authentication (if available).
            HttpMethod request_method = HttpMethod.Get;
            switch (method) {
                case Method.GET:
                    request_method = HttpMethod.Get;
                    break;
                case Method.POST:
                    request_method = HttpMethod.Post;
                    break;
                case Method.DELETE:
                    request_method = HttpMethod.Delete;
                    break;
                case Method.PUT:
                    request_method = HttpMethod.Put;
                    break;
            }
            //At this point, do not parse the URL. It might already contain the URL params added to it. So just call the URL. // parseURI(url).resource_part
            var request = new HttpRequestMessage(request_method, ParseURI(url).pathQuery); //URL should not have base part.

            if (content != null) request.Content = content; //Set content if not null

            //If the request has some kind of request headers, then add them.
            if (!string.IsNullOrWhiteSpace(request_token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue(request_token); //if the input is not correct, for instance, token has space, then it will throw exception. Add without validation.
                //request.Headers.TryAddWithoutValidation("Authorization", request_token);
            }

            //Add other request headers if available.
            if (_requestHeaders != null && _requestHeaders?.Count > 0) {
                foreach (var kvp in _requestHeaders) {
                    try {
                        request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value); //Do not validate.
                    } catch (Exception ex) {
                        WriteLog(LogLevel.Debug, new EventId(2001, "Header Error"), "Error while trying to add a header", ex);
                    }
                }
            }

            RestResponse result = new StringResponse();
            var _response = await SendAsync(request);
            _response.CopyTo(result); //Copy base value.
            //Response we receive will be base response.
            if (_response.IsSuccessStatusCode) {
                var _cntnt = _response.Content;
                var _strCntnt = await _cntnt.ReadAsStringAsync();
                result.Content = _strCntnt;

            }
            return result; //All calls from here will receive stringResponse content.
        }
        
        internal async Task<IResponse> ExecuteAsync(HttpRequestMessage request) {
            this._request = request;
            ValidateClient();
            var _validationCB = Client.GetRequestValidation();
            //if some sort of validation callback is assigned, then call that first.
            if (_validationCB != null) {
                var validation_check = await _validationCB.Invoke(request);
                if (!validation_check) {
                    WriteLog(LogLevel.Information, "Local request validation failed. Please verify the validation methods to return true on successful validation");
                    return new StringResponse() { Content = "Internal Request Validation call back failed." };
                }
            }

            //Here we donot modify anything. We just send and receive the response.
            HttpResponseMessage message;
            if (_cancellation_token != null) {
                message = await Client.BaseClient.SendAsync(request, _cancellation_token.Value);
            }
            else {
                message = await Client.BaseClient.SendAsync(request);
            }

            var _response = new ResponseBase() { OriginalResponse = message };
            return _response;
        }
        internal async Task<IResponse> ExecuteAsync(HttpRequestMessage request,CancellationToken cancellation_token) {
            this._cancellation_token = cancellation_token;
            return await ExecuteAsync(request);
        }
        #endregion

        #region Helpers

        private void ValidateClient() {
            if (Client == null) throw new ArgumentNullException(nameof(Client));
        }
        protected (HttpContent content, string url) ConverToHttpContent(string url, IEnumerable<RequestObject> paramList, Method method) {
            try {
                //HTTPCONENT itself is a abstract class. We can have StringContent, StreamContent,FormURLEncodedContent,MultiPartFormdataContent.
                //Based on the params, we might add the data to content or to the url (in case of get).
                if (paramList == null || paramList?.Count() == 0) return (null, url ?? string.Empty);
                HttpContent processed_content = null;
                string processed_url = url;

                //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body
                //A get request can have a content body.

                //The paramlist might containt multiple request param(which will be trasformed in to query). however, only one (the first) request body will be considered
                processed_content = PrepareBody(paramList, method);
                processed_url = PrepareQuery(url, paramList);
                return (processed_content, processed_url);
            }
            catch (Exception ex) {
                throw ex;
            }
        }
        protected HttpContent PrepareBody(IEnumerable<RequestObject> paramList, Method method) {
            //We can add only one type of body to an object. If we have more than one type, we log the error and take only the first item.
            try {
                HttpContent result = null;
                //paramList.Where(p=> typeof(IRequestBody).IsAssignableFrom(p))?.f
                var _requestBody = paramList.Where(p => p is IRequestBody)?.FirstOrDefault();
                if (_requestBody == null || _requestBody.Value == null) return result; //Not need of further processing for null values.
                WriteLog(LogLevel.Debug, $@"Request body of type {_requestBody?.GetType()} is getting added to request body.");
                if (_requestBody is RawBodyRequest rawReq) {
                    //Just add a raw content and send.
                    result = prepareRawBody(rawReq);

                }
                else if (_requestBody is FormBodyRequest formreq) {
                    //Decide if this is multipart form or urlencoded form data
                    result = prepareFormBody(formreq);
                }
                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(6000), "Error while trying to prepare body", ex);
                return null;
            }
        }
        protected string PrepareQuery(string url, IEnumerable<RequestObject> paramList) {
            string result = url;
            var _query = HttpUtility.ParseQueryString(string.Empty);

            var _paramQueries = paramList.Where(p => p is IRequestQuery)?.Cast<IRequestQuery>().ToList();
            if (_paramQueries == null || _paramQueries.Count == 0) return result; //return the input url

            foreach (var param in _paramQueries) {
                var _key = param.Key;
                var _value = param.Value;

                if (param.ShouldEncode) {
                    //Encode before adding
                    if (!param.IsEncoded) {
                        _key = Uri.EscapeDataString(_key);
                        _value = Uri.EscapeDataString(_value);
                        param.SetEncoded();
                    }
                }
                _query[_key] = _value;
            }

            var _formed_query = _query.ToString();
            if (!string.IsNullOrWhiteSpace(_formed_query)) {
                result = result + "?" + _formed_query;
            }
            return result;
        }
        protected HttpContent prepareRawBody(RawBodyRequest rawbody) {
            try {
                HttpContent result = null;
                switch (rawbody.BodyType) {
                    case BodyContentType.StringContent:
                        string mediatype = null;
                        string _serialized_content = rawbody.Value as string; //Assuming it is already serialized.

                        switch (rawbody.StringBodyFormat) {
                            case StringContentFormat.Json:
                                if (!rawbody.IsSerialized) {
                                    _serialized_content = rawbody.ToJson(_jsonConverters?.Values?.ToList());
                                }
                                mediatype = "application/json";
                                break;

                            case StringContentFormat.XML:
                                if (!rawbody.IsSerialized) {
                                    _serialized_content = rawbody.ToXml().ToString();
                                }
                                mediatype = "application/xml";
                                break;
                            case StringContentFormat.PlainText:
                                if (!rawbody.IsSerialized) {
                                    _serialized_content = rawbody.ToJson(_jsonConverters?.Values?.ToList());
                                }
                                mediatype = "text/plain";
                                break;
                        }
                        result = new StringContent(_serialized_content, Encoding.UTF8, mediatype);
                        break;

                    case BodyContentType.ByteArrayContent:
                    case BodyContentType.StreamContent:
                        if (rawbody.Value is byte[] byteContent) {
                            //If byte content.
                            result = new ByteArrayContent(byteContent, 0, byteContent.Length);
                        }
                        else if (rawbody.Value is Stream streamContent) {
                            //If stream content.
                            result = new StreamContent(streamContent);
                            //Dont' remove all headers. Only the content type. Header might have authentications properly set.
                            result.Headers.Remove("Content-Type");
                            result.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            result.Headers.ContentDisposition = new ContentDispositionHeaderValue("stream-data") { FileName = rawbody.FileName ?? "attachment" };
                        }
                        break;
                }
                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(6001), "Error while trying to prepare Raw body", ex);
                return null;
            }
        }
        protected HttpContent prepareFormBody(FormBodyRequest formbody) {
            try {
                HttpContent result = null;
                //Form can be url encoded form and multi form.. //TODO : REFINE
                //For more than one add as form data.
                MultipartFormDataContent form_content = new MultipartFormDataContent();
                form_content.Headers.Remove("Content-Type");
                form_content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);

                foreach (var item in formbody.Value) {
                    if (item.Value == null) continue;
                    var rawContent = prepareRawBody(item.Value);
                    if (string.IsNullOrWhiteSpace(item.Value.FileName)) {
                        form_content.Add(rawContent, item.Key); //Also add the key.
                    }
                    else {
                        form_content.Add(rawContent, item.Key, item.Value.FileName); //File name cannot be empty. Sending empty variable throws exception/
                    }
                }

                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(1003), "Error while trying to prepare Form body", ex);
                return null;
            }
        }
        #endregion
        public override string ToString()
        {
            return this.URL;
        }

        
    }
}
