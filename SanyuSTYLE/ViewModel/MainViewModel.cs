using EasyModbus;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using SANYU2021.Commands;
using SanyuSTYLE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SANYU2021.ViewModel
{

    public class MainViewModel : INotifyPropertyChanged
    {
        private ModbusClient ModClient = new ModbusClient("COM3");
        private DispatcherTimer connectionTimer = new DispatcherTimer();
        private DispatcherTimer odczytTimer = new DispatcherTimer();
        WaitWindow w2 = new WaitWindow();
        private Rejestr aktualnyRejestr;
        private bool _odczytStance = false;
        private int _engineStance = -1;

        public MainViewModel()
        {
            //parametry
            connectionTimer.Interval = new TimeSpan(0, 0, 1);
            connectionTimer.Tick += ConnectionTimer_Tick;
            odczytTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            odczytTimer.Tick += OdczytTimer_Tick;

            //tymczasowe parametry polacznia slave
            ModClient.Baudrate = 9600;
            ModClient.Parity = System.IO.Ports.Parity.None;

            //komendy
            ConnectCommand = new RelayCommand(ConnectFunc);
            OdczytCommand = new RelayCommand(OdczytFunc);
            EngineCommand = new RelayCommand(EngineFunc);
            SaveRegisterCommand = new RelayCommand(SaveRegisterFunc);
            ExportCommand = new RelayCommand(saveFile);
            ImportCommand = new RelayCommand(openFile);
            FullExportCommand = new RelayCommand(fullexport);

            //zmienne
            ConnVal = "Brak połączenia";
            IsConnected = false;
        }

        private void SaveRegisterFunc(object obj)
        {
            if (aktualnyRejestr is not null) {
                switch (aktualnyRejestr.Typ) // 0- read only, 1- mozliwosc edycji zawsze 2 - mozliwosc edycji tylko przy STOP
                {
                    case 0:
                        MessageBox.Show("Wartosc tylko do odczytu, nie mozna modyfikowac!");
                        break;
                    case 1:
                        if (BiezacaWartosc > aktualnyRejestr.Max || BiezacaWartosc < aktualnyRejestr.Min)
                        {
                            MessageBox.Show("Wprowadzono niepoprawną wartość");
                            try
                            {
                                BiezacaWartosc = ModClient.ReadHoldingRegisters(aktualnyRejestr.Id, 1)[0];
                            }
                            catch
                            {
                                MessageBox.Show("Błąd podczas zczytywania parametru");
                                BiezacaWartosc = 0;
                            }
                        }
                        else
                        {
                            try
                            {
                                ModClient.WriteSingleRegister(aktualnyRejestr.Id, BiezacaWartosc);
                                BiezacaWartosc = ModClient.ReadHoldingRegisters(aktualnyRejestr.Id, 1)[0];

                            }
                            catch
                            {
                                MessageBox.Show("NIEZNANY BŁĄD PARAMETRU/ BRAK ZGODNOSCI Z INTRUKCJA");
                                BiezacaWartosc = 0;
                            }
                        }
                        break;
                    case 2:
                        if (BiezacaWartosc > aktualnyRejestr.Max || BiezacaWartosc < aktualnyRejestr.Min || _engineStance != 0)
                        {
                            MessageBox.Show("Wprowadzono niepoprawną wartość lub silnik nie jest w stanie STOP");
                            try
                            {
                                BiezacaWartosc = ModClient.ReadHoldingRegisters(aktualnyRejestr.Id, 1)[0];
                            }
                            catch
                            {
                                MessageBox.Show("Błąd podczas zczytywania parametru");
                                BiezacaWartosc = 0;
                            }
                        }
                        else {
                            try
                            {
                                ModClient.WriteSingleRegister(aktualnyRejestr.Id, BiezacaWartosc);
                                BiezacaWartosc = ModClient.ReadHoldingRegisters(aktualnyRejestr.Id, 1)[0];
                            }
                            catch
                            {
                                MessageBox.Show("NIEZNANY BŁĄD PARAMETRU/ BRAK ZGODNOSCI Z INTRUKCJA");
                                BiezacaWartosc = 0;
                            }
                        }
                        break;
                }
            }
        }

        private void EngineFunc(object obj)
        {
            try
            {
                var param = Convert.ToInt32(obj);
                ModClient.WriteSingleRegister(8192, param);
            }
            catch
            {
                MessageBox.Show("Bład podczas wywolywania funkcji rozruchu lub stopu");
            }
        }

        private void OdczytTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                int[] temptable = ModClient.ReadHoldingRegisters(1, 9);                
                double[] temptabled = new double[9];

                for(int i=0; i<temptable.Length;i++)
                {
                    temptabled[i] = (double)temptable[i];
                    if (i == 0 || i == 1 || i == 2 || i == 4) temptabled[i] = temptabled[i] / 10;
                    else if (i == 6) temptabled[i] = temptabled[i] / 100;
                }
                Odczyt = temptabled;

            }
            catch
            {
                zatrzymajCzytanie();
            }
        }

        void zatrzymajCzytanie()
        {
            odczytTimer.Stop();
            _odczytStance = false;
            OdczytStanceLabel = "Rozpocznij zczytywanie";

        }

        private void OdczytFunc(object obj)
        {
            if (!_odczytStance)
            {
                odczytTimer.Start();
                _odczytStance = true;
                OdczytStanceLabel = "Zakończ zczytywanie";
            }
            else
            {
                zatrzymajCzytanie();
            }
        }

        private void ConnectionTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                _engineStance = ModClient.ReadHoldingRegisters(28, 1)[0];
                ustawDiody(_engineStance);
            }
            catch
            {
                wyloguj();
                MessageBox.Show("Urządzenie zostało odłączone lub nastąpił problem połączenia", "Błąd połączenia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //glowny przycisk connect
        private void ConnectFunc(object obj)
        {
            if (!IsConnected)
            {
                //zaloguj();
                ConnVal = "Wymuszony start!!!";
                ConnectButtonStance = "Rozłącz";
                IsConnected = true;
            }
            else
            {
                wyloguj();
            }
        }

        void ustawDiody(int stc)
        {
            switch (stc)
            {
                case 0:
                    Diody = new SolidColorBrush[]
                    {
                        new SolidColorBrush(Colors.Gray), new SolidColorBrush(Colors.Gray) , new SolidColorBrush(Colors.Red)
                    };
                    break;
                case 1:
                    Diody = new SolidColorBrush[]
                    {
                        new SolidColorBrush(Colors.Lime), new SolidColorBrush(Colors.Gray) , new SolidColorBrush(Colors.Gray)
                    };
                    break;
                case 2:
                    Diody = new SolidColorBrush[]
                    {
                        new SolidColorBrush(Colors.Gray), new SolidColorBrush(Colors.Lime) , new SolidColorBrush(Colors.Gray)
                    };
                    break;
                default:
                    Diody = new SolidColorBrush[]
                    {
                        new SolidColorBrush(Colors.Gray), new SolidColorBrush(Colors.Gray) , new SolidColorBrush(Colors.Gray)
                    };
                    break;
            }
        }

        void zaloguj()
        {
            try
            {
                ModClient.Connect();
                ModClient.ReadHoldingRegisters(28, 1);
                pobierzAlarm();
                ConnVal = "Połączono";
                ConnectButtonStance = "Rozłącz";
                IsConnected = true;
                connectionTimer.Start();
            }
            catch
            {
                ConnVal = "Brak połączenia";
                ConnectButtonStance = "Połącz";
                MessageBox.Show("Nie udało się połączyć, sprawdź ustawienia falownika:\n-Prędkość transmisji -> 9600 bps\n-Format danych -> Tryb RTU, brak bitu parzystości", "Błąd połączenia", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        void pobierzAlarm()
        {
            int[] val = ModClient.ReadHoldingRegisters(10, 1);
            AlarmErrorLabel = StaticArrays.bledyAlarmow[val[0]];
            val = ModClient.ReadHoldingRegisters(14, 5);
            double[] temp = new double[5] { val[0]*1.0 / 10, val[1]*1.0 / 10, val[2], val[3]*1.0/10, val[4]*1.0/10};
            AlarmParameters = temp;         
        }

        void wyloguj()
        {
            ModClient.Disconnect();
            ConnVal = "Brak połączenia";
            ConnectButtonStance = "Połącz";
            _engineStance = -1;
            IsConnected = false;
            connectionTimer.Stop();
            ustawDiody(-1);
            zatrzymajCzytanie();
        }



        //komendy
        public ICommand ConnectCommand { get; set; }
        public ICommand OdczytCommand { get; set; }
        public ICommand EngineCommand { get; set; }
        public ICommand SaveRegisterCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand FullExportCommand { get; set; }
        public ICommand ImportCommand { get; set; }




        //zmienne dynamiczne

        private bool _isReady = true;
        public bool IsReady
        {
            get { return _isReady; }
            set { _isReady = value;
                OnPropetryChanged();
            }
        }

        private string _connectButtonStance = "Połącz";
        public string ConnectButtonStance
        {
            get { return _connectButtonStance; }
            set { _connectButtonStance = value;
                OnPropetryChanged();
            }
        }


        private string _connVal;
        public string ConnVal
        {
            get { return _connVal; }
            set {
                _connVal = value;
                OnPropetryChanged();
            }
        }

        //tablica z wartosciami odczytu natychmiastowego
        private double[] _odczyt = new double[9];
        public double[] Odczyt
        {
            get { return _odczyt; }
            set { _odczyt = value;
                OnPropetryChanged();
            }
        }

        //parametry ostatniego alarmu
        private double[] _alarmParameters;
        public double[] AlarmParameters
        {
            get { return _alarmParameters; }
            set { _alarmParameters = value;
                OnPropetryChanged();
            }
        }


        //stan polaczenia => true - połączono
        private bool _isconnected;
        public bool IsConnected
        {
            get { return _isconnected; }
            set { _isconnected = value;
                OnPropetryChanged();
            }
        }

        //parametr odpowiedzialny za bool do enabled pola Biezaca wartosc
        private bool _biezacaWartoscEnabled = false;
        public bool BiezacaWartoscEnabled
        {
            get { return _biezacaWartoscEnabled; }
            set {
                _biezacaWartoscEnabled = value;
                OnPropetryChanged();
            }
        }

        //napis rozpocznij zczytywanie/ zakoncz zczytywanie
        private string _odczytStanceLabel = "Rozpocznij zczytywanie";
        public string OdczytStanceLabel
        {
            get { return _odczytStanceLabel; }
            set { _odczytStanceLabel = value;
                OnPropetryChanged();
            }
        }

        //slowny zapis alarmu -> informacja dla uzytkownika
        private string _alarmErrorLabel = "Brak alarmu";
        public string AlarmErrorLabel
        {
            get { return _alarmErrorLabel; }
            set { _alarmErrorLabel = value;
                OnPropetryChanged();
            }
        }

        //tablica kolorow diód
        private SolidColorBrush[] _diody = new SolidColorBrush[]
        {
            new SolidColorBrush(Colors.Gray), new SolidColorBrush(Colors.Gray) , new SolidColorBrush(Colors.Gray)
        };
        public SolidColorBrush[] Diody
        {
            get { return _diody; }
            set { _diody = value;
                OnPropetryChanged();
            }
        }

        //lista rozwijana
        private Dictionary<int, string> _listaKategorii = StaticArrays.listaKategorii;
        public Dictionary<int,string> ListaTestowa
        {
            get { return _listaKategorii; }
            set
            {
                _listaKategorii = value;
                OnPropetryChanged();
            }
        }

        //lista rejestrow do wyboru -> rolka
        private Dictionary<string,string> _listaRejestrow;
        public Dictionary<string,string> ListaRejestrow
        {
            get { return _listaRejestrow; }
            set { _listaRejestrow = value;
                OnPropetryChanged();
            }
        }

        //zmienna przechowująca wartosc domyslna dla danego rejestru -> dB
        private int _wartoscDomyslna;
        public int WartoscDomyslna
        {
            get { return _wartoscDomyslna; }
            set { _wartoscDomyslna = value;
                OnPropetryChanged();
            }
        }

        //zmienna przechowująca zakres wartosci dla danego rejestru -> dB
        private string _zakresWartosci;
        public string ZakresWartosci
        {
            get { return _zakresWartosci; }
            set { _zakresWartosci = value;
                OnPropetryChanged();
            }
        }

        //zmienna przechowująca typ dla danego rejestru -> dB
        private string _typ = "-";
        public string Typ
        {
            get { return _typ; }
            set { _typ = value;
                OnPropetryChanged();
            }
        }

        private string _etykieta = "";
        public string Etykieta
        {
            get { return _etykieta; }
            set { _etykieta = value;
                OnPropetryChanged();
            }
        }

        //zmienna przechowująca biezaca wartosc dla danego rejestru -> falownik
        private int _biezacaWartosc;
        public int BiezacaWartosc
        {
            get { return _biezacaWartosc; }
            set { _biezacaWartosc = value;
                OnPropetryChanged();
            }
        }


        //pobiera liste rejestrow z danej kategorii
        private void pobierzRejestry(string _wybranaKategoria)
        {
            Typ = "-";
            BiezacaWartoscEnabled = false;
            BiezacaWartosc = 0;
            WartoscDomyslna = 0;
            ZakresWartosci = "-";
            Etykieta = "";
            WybranyParametr = null;
            ListaRejestrow = null;
            aktualnyRejestr = null;
            Dictionary<string, string> templist = new Dictionary<string, string>();
            foreach (var rej in DbLocator.Database.TabelaRejestrow.Where(x => x.Kategoria == Int32.Parse(_wybranaKategoria)).ToList())
            { 
                templist.Add(rej.NazwaRejestru,rej.Opis + " ("+ rej.NazwaRejestru+")");
            }
            if (templist.Count != 0)
            {
                ListaRejestrow = templist;
            }
        }

        //zmienna przechowujaca aktualnie wybrana kategorie -> query do dB
        private string _wybranaKategoria;
        public string WybranaKategoria
        {
            get { return _wybranaKategoria; }
            set { _wybranaKategoria = value;
                OnPropetryChanged();
                pobierzRejestry(_wybranaKategoria);
            }
        }

        //zmienna przechowujaca aktualnie wybrany parametr -> query do dB
        private string _wybranyParametr;
        public string WybranyParametr
        {
            get { return _wybranyParametr; }
            set { _wybranyParametr = value;
                OnPropetryChanged();
                pobierzDaneRejestru(value);
            }
        }

        //funkcja pobierajaca dane dla konkretnego rejestru z bazy danych -> rejestr (Wybrany Parametr)
        private void pobierzDaneRejestru(string rejestr)
        {
            if (rejestr is not null)
            {
                aktualnyRejestr = DbLocator.Database.TabelaRejestrow.FirstOrDefault(x => x.NazwaRejestru == rejestr);
                WartoscDomyslna = aktualnyRejestr.WartoscDomyslna;
                ZakresWartosci = aktualnyRejestr.ZakresWartosci;
                Etykieta = aktualnyRejestr.Etykieta;
                Typ = StaticArrays.typyRejestrow[aktualnyRejestr.Typ];
                if (aktualnyRejestr.Typ > 0) BiezacaWartoscEnabled = true;
                try
                {
                    BiezacaWartosc = ModClient.ReadHoldingRegisters(aktualnyRejestr.Id, 1)[0];                   
                }
                catch
                {
                    BiezacaWartosc = 0;
                }
            }
            else aktualnyRejestr = null;
        }


        //IMPORT EKSPORT:

        //EKSPORT PODGLĄDOWY

       private async void saveFile(object obj)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Pliki Sanyu | *.sanyu";
            if (sfd.ShowDialog() == true)
            {
                w2.Show();              
                int result = await saveFileAsync(sfd);
                w2.Hide();
                if(result == 1) MessageBox.Show("Pomyślnie zapisano plik!");
                else
                {
                    MessageBox.Show("Nie udało się zapisać pliku");
                }
            }                
        }

        private async Task<int> saveFileAsync(SaveFileDialog sfd)
        {
            return await Task.Run(() =>
            {
                    string savedFile = Path.GetFullPath(sfd.FileName.ToString());
                    StringBuilder csv = new StringBuilder();
                    IsReady = false;
                    csv.Append("rejestr;wartosc\n");                   
                    foreach (var rej in DbLocator.Database.TabelaRejestrow.Where(x => x.Typ != 0).ToList())
                    {
                        try
                        {
                            var valueToExport = ModClient.ReadHoldingRegisters(rej.Id, 1)[0];         
                            var newLine = string.Format("{0};{1}\n", rej.Id, valueToExport);
                            csv.Append(newLine);
                        }
                        catch
                        {
                            IsReady = true;                           
                            return 0;
                        }
                    }
                    File.AppendAllText(savedFile, csv.ToString());
                    IsReady = true;
                    return 1;                
            });
        }

        private async void fullexport(object obj)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Pliki *.csv | *.csv";
            if (sfd.ShowDialog() == true)
            {
                w2.Show();
                int result = await fullexportAsync(sfd);
                w2.Hide();
                if (result == 1) MessageBox.Show("Pomyślnie zapisano plik!");
                else
                {
                    MessageBox.Show("Nie udało się zapisać pliku");
                }
            }
        }

        private async Task<int> fullexportAsync(SaveFileDialog sfd)
        {
            return await Task.Run(() =>
            {
                IsReady = false;
                string savedFile = Path.GetFullPath(sfd.FileName.ToString());
                StringBuilder csv = new StringBuilder();
                csv.Append("rejestr;wartosc_domyslna;wartosc_biezaca\n");
                foreach (var rej in DbLocator.Database.TabelaRejestrow.ToList())
                {
                    try
                    {
                        var valueToExport = ModClient.ReadHoldingRegisters(rej.Id, 1)[0];
                        var newLine = string.Format("{0};{1};{2}\n", rej.Id, rej.WartoscDomyslna.ToString(), valueToExport);
                        csv.Append(newLine);
                    }
                    catch
                    {
                        IsReady = true;
                        return 0;
                    }
                }
                File.AppendAllText(savedFile, csv.ToString());
                IsReady = true;               
                return 1;
            });
        }

        //IMPORT
        private async void openFile(object obj)
        {
            if (_engineStance != 0)
            {
                MessageBox.Show("Wymagany tryb STOP");
                return;
            }
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Sanyu Files | *.sanyu";
            dialog.Multiselect = false;

            if (dialog.ShowDialog() == true)
            {
                w2.Show();
                int result = await importFileAsync(dialog.FileName);
                w2.Hide();
                if (result == 1) MessageBox.Show("Poprawnie zaimportowano plik");
                else
                {
                    MessageBox.Show("Plik ma nieprawidłowy format lub próbowano nadpisać niemodyfikowalny rejestr");
                }               
            }
        }

        private async Task<int> importFileAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                IsReady = false;
                using (TextFieldParser parser = new TextFieldParser(fileName))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(";");
                    int rejestr;
                    int wartosc;
                    if (!parser.EndOfData)
                    {
                        parser.ReadLine();
                    }
                    while (!parser.EndOfData)
                    {
                        try
                        {
                            //Process row
                            string[] fields = parser.ReadFields();
                            rejestr = Int32.Parse(fields[0]);
                            wartosc = Int32.Parse(fields[1]);
                            ModClient.WriteSingleRegister(rejestr, wartosc);
                        }
                        catch
                        {
                            IsReady = true;
                            return 0;
                        }
                    }
                    IsReady = true;
                    return 1;
                }
            });
        }

        //event, prosze nie modyfikowac
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropetryChanged([CallerMemberName]string propetryName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propetryName));
        }
    }
}
