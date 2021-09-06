﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PRM.Core.Model;

namespace PRM.View.ProtocolConfig
{
    /// <summary>
    /// PuttyRunnerConfiger.xaml 的交互逻辑
    /// </summary>
    public partial class PuttyRunnerConfiger : UserControl
    {
        public PuttyRunnerConfiger()
        {
            InitializeComponent();
        }

        public void Init(PrmContext prmContext)
        {
            DataContext = prmContext.ProtocolConfigurationService.SshDefaultRunner;
        }
    }
}
