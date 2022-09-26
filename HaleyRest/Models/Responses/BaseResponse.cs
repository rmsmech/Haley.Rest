﻿using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;


namespace Haley.Models
{
    public class BaseResponse : IResponse
    {
        public HttpResponseMessage OriginalResponse { get; private set; }
        public HttpContent OriginalContent => OriginalResponse == null ? null : OriginalResponse.Content;

        public bool IsSuccessStatusCode => OriginalResponse == null ? false : OriginalResponse.IsSuccessStatusCode;
        public IRequest Request {get; internal set;}
        public string Message { get; private set; }

        public IResponse UpdateResponse(HttpResponseMessage response) {
            this.OriginalResponse = response;
            return this;
        }
        public IResponse UpdateResponse(IResponse source_response)
        {
            this.OriginalResponse = source_response.OriginalResponse;
            return this;
        }
        public IResponse SetMessage(string message) {
            Message = message;
            return this;
        }
        public BaseResponse(HttpResponseMessage httpresponse) { UpdateResponse(httpresponse); }
        public BaseResponse() : this(httpresponse: null) { }
    }
}
