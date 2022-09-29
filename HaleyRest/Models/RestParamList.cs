﻿using Haley.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Collections.Specialized;
using System.Xml;
using Haley.Utils;

namespace Haley.Models {
    public class RestParamList : List<RestParam> {

        #region Getters
        public RestParam this[string key] {
            get {
                var allparams = this.Where(p => p.Key.Equals(key));
                if (allparams.Count() == 0) {
                    return null;
                }

                if (allparams.Count() == 1) {
                    return allparams.Single();
                }

                // We have more than one value for same key, we need to concatenate and provide the values over.
                var result = string.Join(",", allparams.Select(p => p.Value).ToArray());
                return new RestParam(key, result); //Instead of one value, we give all values merged together.
            }
        }

        public IEnumerable<string> Keys {
            get {
                return this.Select(p => p.Key)?.Distinct();
            }
        }

        public IEnumerable<string> Values {
            get {
                return this.Select(p => p.Value); //Not distinct.
            }
        }

        #endregion

        #region Constructors
        public RestParamList() { }

        public RestParamList(IEnumerable<RestParam> @params) {
            AddRange(@params);
        }
        public RestParamList(NameValueCollection @params) {
            AddCollection(@params);
        }

        public RestParamList(Dictionary<string, string> @params) {
            this.AddDictionary(@params);
        }
        #endregion

        #region Methods

        public void AddRange(IEnumerable<RestParam> @params) {
            if (@params == null) return;
            base.AddRange(@params);
        }
        public void AddCollection(NameValueCollection @params) {
            if (@params == null) return;
            foreach (var key in @params.AllKeys) {
                var param = new RestParam(key, @params[key]);
                base.Add(param);
            }
        }

        public void AddDictionary(IDictionary<string, string> @params) {
            if (@params == null) return;
            foreach (var item in @params) {
                var param = new RestParam(item.Key, item.Value);
                base.Add(param);
            }
        }

        public void SortByKey() {
            this.Sort((item1, item2) => item1.Key.CompareTo(item2.Key));
        }

        public string GetConcatenatedString(string splitter = "&", string kvp_merger = "=", bool should_sort = true,bool strictencodekvp = true, bool encodevalues = true) {
            var baseArr = new RestParam[this.Count()];
            this.CopyTo(baseArr);
            var copiedList = baseArr.ToList();
            var workinglist = copiedList;
            if (strictencodekvp) {
                workinglist = copiedList.Select(p => new RestParam(NetUtils.UrlEncodeStrict(p.Key), NetUtils.UrlEncodeStrict(p.Value))).ToList();
            }

            if (should_sort) {
                workinglist.Sort((item1, item2) => item1.Key.CompareTo(item2.Key)); //Compare the keys to sort. //Still this keep hello100 as small than hello2. Need to optimize later to use better way to compre alphanumeric values (check haley utils alphanumeric comparer).
            }

            StringBuilder sb = new StringBuilder();
            var total = copiedList.Count();
            int i = 0;
            foreach (var item in copiedList) {
                sb.Append(item.Key);
                sb.Append(kvp_merger);
                if (!encodevalues) {
                    sb.Append(NetUtils.UrlEncodeRelaxed(item.Value));
                }
                sb.Append(item.Value);

                i++;
                if (i < total) {
                    sb.Append(splitter);
                }
            }

            return sb.ToString();
        }
        #endregion

    }
}
