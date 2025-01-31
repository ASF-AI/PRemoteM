﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JsonKnownTypes;
using Microsoft.Win32;
using Newtonsoft.Json;
using PRM.Core.I;
using PRM.Core.Protocol.Runner.Default;
using PRM.Core.Service;
using Shawn.Utils;

namespace PRM.Core.Protocol.Runner
{
    public class ExternalRunner : Runner
    {
        public class ObservableKvp<K, V> : NotifyPropertyChangedBase
        {
            private K _key;
            public K Key
            {
                get => _key;
                set => SetAndNotifyIfChanged(ref _key, value);
            }

            private V _value;

            public V Value
            {
                get => _value;
                set => SetAndNotifyIfChanged(ref _value, value);
            }

            public ObservableKvp(K key, V value)
            {
                Key = key;
                Value = value;
            }
        }

        public ExternalRunner(string runnerName) : base(runnerName)
        {
        }

        protected string _exePath;
        public string ExePath
        {
            get => _exePath;
            set => SetAndNotifyIfChanged(ref _exePath, value);
        }


        protected string _arguments = "";
        public string Arguments
        {
            get => _arguments;
            set => SetAndNotifyIfChanged(ref _arguments, value);
        }


        protected bool _runWithHosting = false;
        public bool RunWithHosting
        {
            get => _runWithHosting;
            set => SetAndNotifyIfChanged(ref _runWithHosting, value);
        }

        private ObservableCollection<ObservableKvp<string, string>> _environmentVariables = new ObservableCollection<ObservableKvp<string, string>>();
        public ObservableCollection<ObservableKvp<string, string>> EnvironmentVariables
        {
            get => _environmentVariables ??= new ObservableCollection<ObservableKvp<string, string>>();
            set => _environmentVariables = value;
        }

        /// <summary>
        /// Marco names for auto complete use
        /// </summary>
        [JsonIgnore]
        public List<string> MarcoNames { get; set; }
        [JsonIgnore]
        public Type ProtocolType { get; set; }
    }
}
