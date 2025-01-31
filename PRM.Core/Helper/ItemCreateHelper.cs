﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PRM.Core.I;
using PRM.Core.Protocol;

namespace PRM.Core.Helper
{
    public class ItemCreateHelper
    {
        private static readonly object Locker = new object();
        private static List<ProtocolServerBase> _baseList = new List<ProtocolServerBase>();

        public static ProtocolServerBase CreateFromDbOrm(IDbServerModel iServerModel)
        {
            // reflect all the child class
            lock (Locker)
            {
                if (_baseList.Count == 0)
                {
                    var assembly = typeof(ProtocolServerBase).Assembly;
                    var types = assembly.GetTypes();
                    _baseList = types.Where(x => x.IsSubclassOf(typeof(ProtocolServerBase)) && !x.IsAbstract)
                        .Select(type => (ProtocolServerBase)Activator.CreateInstance(type)).ToList();
                }
            }

            // get instance form json string
            foreach (var serverBase in _baseList)
            {
                if (iServerModel.GetProtocol() == serverBase.Protocol
                    && iServerModel.GetClassVersion() == serverBase.ClassVersion)
                {
                    var jsonString = iServerModel.GetJson();
                    var ret = serverBase.CreateFromJsonString(jsonString);
                    if (ret == null) continue;
                    // set id.
                    ret.Id = iServerModel.GetId();
                    return ret;
                }
            }

            return null;
        }

        public static ProtocolServerBase CreateFromJsonString(string jsonString)
        {
            var jObj = JsonConvert.DeserializeObject<dynamic>(jsonString);
            if (jObj == null ||
                jObj?.Protocol == null ||
                jObj?.ClassVersion == null)
                return null;

            // reflect all the child class
            lock (Locker)
            {
                if (_baseList.Count == 0)
                {
                    var assembly = typeof(ProtocolServerBase).Assembly;
                    var types = assembly.GetTypes();
                    _baseList = types.Where(item => item.IsSubclassOf(typeof(ProtocolServerBase)) && !item.IsAbstract)
                        .Select(type => (ProtocolServerBase)Activator.CreateInstance(type)).ToList();
                }
            }

            // get instance form json string
            lock (Locker)
            {
                foreach (var @base in _baseList)
                {
                    if (jObj.Protocol.ToString() == @base.Protocol &&
                        jObj.ClassVersion.ToString() == @base.ClassVersion)
                    {
                        var ret = @base.CreateFromJsonString(jsonString);
                        if (ret != null)
                        {
                            return ret;
                        }
                    }
                }
            }
            return null;
        }
    }
}