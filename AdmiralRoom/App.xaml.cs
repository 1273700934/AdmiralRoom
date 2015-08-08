﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Huoyaoyuan.AdmiralRoom
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Helper.SetIEEmulation(11001);
            Helper.SetGPURendering(true);
            Helper.SetMMCSSTask();

            this.MainWindow = new MainWindow();
            ThemeService.EnableAero(true);
            MainWindow.Show();
        }
    }
}
