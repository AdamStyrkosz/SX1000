using SANYU2021.Commands;
using SANYU2021.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SanyuSTYLE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var database = new SanyuDbContext();
            //sprawdz czy jest utworzona
            database.Database.EnsureCreated();

            DbLocator.Database = database;
        }
    }
}
