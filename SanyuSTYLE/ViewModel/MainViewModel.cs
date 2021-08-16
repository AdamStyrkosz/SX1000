using EasyModbus;
using SANYU2021.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        static Dictionary<int, string> bledyAlarmow = new Dictionary<int, string>()
        {
            {0,"Brak błędu"},
            {2,"Przeciążenie prądowe podczas przysp."},
            {3,"Przeciążenie prądowe podczas ham."},
            {4,"Przeciązenie prądowe dla stałej prędkości"},
            {5,"Przepięcie podczas przysp."},
            {6,"Przepięcie podczas ham."},
            {7,"Przepięcie dla stałej prędkości"},
            {8,"Przeciążony rezystor"},
            {9,"Zbyt małe napięcie zasilania"},
            {10,"Przeciążenie napędu"},
            {11,"Przeciązenie silnika"},
            {14,"Moduł przegrzany"},
            {15,"Błąd zewnętrzny"},
            {16,"Błąd komunikacji"},
            {24,"Za niskie ciśnienie (dla PID)"},
            {27,"Za wysokie ciśnienie (dla PID)"},
            {28,"Suchobieg"},
            {29,"Czas zasilania osiągnięty"},
            {31,"Błąd sterowania PID"}
        };
        static Dictionary<int, string> typyRejestrow = new Dictionary<int, string>()
        {
            {0,"Tylko do odczytu"},
            {1,"Edycja w stanie zatrzymania lub pracy"},
            {2,"Edycja w stanie zatrzymania"}
        };

        private ModbusClient ModClient = new ModbusClient("COM3");

        private DispatcherTimer connectionTimer = new DispatcherTimer();
        private DispatcherTimer odczytTimer = new DispatcherTimer();

        private Rejestr aktualnyRejestr;
        private bool _odczytStance = false;
        private int _engineStance = -1;

       
        public MainViewModel()
        {
            //parametry
            connectionTimer.Interval = new TimeSpan(0, 0, 1);
            connectionTimer.Tick += ConnectionTimer_Tick;

            odczytTimer.Interval = new TimeSpan(0, 0, 0,0,250);
            odczytTimer.Tick += OdczytTimer_Tick;

            //tymczasowe parametry polacznia slave
            ModClient.Baudrate = 9600;
            ModClient.Parity = System.IO.Ports.Parity.None;

            //komendy
            ConnectCommand = new RelayCommand(ConnectFunc);
            OdczytCommand = new RelayCommand(OdczytFunc);
            EngineCommand = new RelayCommand(EngineFunc);
            SaveRegisterCommand = new RelayCommand(SaveRegisterFunc);

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
                            MessageBox.Show("Wprowadzono niepoprawną wartość lub silnik działa");
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
            catch(Exception ex)
            {
                MessageBox.Show("Bład podczas wywolywania funkcji rozruchu lub stopu");
            }
        }

        private void OdczytTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Odczyt = ModClient.ReadHoldingRegisters(1, 9);
            }
            catch(Exception ex)
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
            catch(Exception ex)
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
                //WYMUSZONY START!!!!!
                zaloguj();
                //ConnVal = "Wymuszony start!!!";
                //IsConnected = true;

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
                        new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.Gray) , new SolidColorBrush(Colors.Gray)
                    };
                    break;
                case 2:
                    Diody = new SolidColorBrush[]
                    {
                        new SolidColorBrush(Colors.Gray), new SolidColorBrush(Colors.Green) , new SolidColorBrush(Colors.Gray)
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
                IsConnected = true;
                connectionTimer.Start();


            }
            catch (Exception ex)
            {
                ConnVal = "Błąd połączenia";
                MessageBox.Show("Nie udało się połączyć", "Błąd połączenia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void pobierzAlarm()
        {
            int[] val = ModClient.ReadHoldingRegisters(10, 1);
            AlarmErrorLabel = bledyAlarmow[val[0]];
            AlarmParameters = ModClient.ReadHoldingRegisters(14, 5);
        }

        void wyloguj()
        {
            ModClient.Disconnect();
            ConnVal = "Rozłączono";
            IsConnected = false;
            connectionTimer.Stop();
            ustawDiody(-1);
            zatrzymajCzytanie();
        }



        //komendy
        public ICommand ConnectCommand{ get; set; }
        public ICommand OdczytCommand { get; set; }
        public ICommand EngineCommand { get; set; }
        public ICommand SaveRegisterCommand { get; set; }


        //zmienne dynamiczne
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
        private int[] _odczyt;
        public int[] Odczyt
        {
            get { return _odczyt; }
            set { _odczyt = value;
                OnPropetryChanged();
            }
        }

        //parametry ostatniego alarmu
        private int[] _alarmParameters;
        public int[] AlarmParameters
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

        //lista rozwijana -> do przerobienia na DICT
        private KeyValuePair<int, string>[] tripLengthList = {
        new KeyValuePair<int, string>(0, "P0XX"),
        new KeyValuePair<int, string>(1, "P1XX"),
        new KeyValuePair<int, string>(2, "P2XX"),
        new KeyValuePair<int, string>(3, "P3XX"),
        new KeyValuePair<int, string>(4, "P4XX"),
        new KeyValuePair<int, string>(5, "P5XX"),
        new KeyValuePair<int, string>(6, "P6XX"),
        new KeyValuePair<int, string>(7, "P7XX"),
        new KeyValuePair<int, string>(8, "P8XX"),
        };
        public KeyValuePair<int, string>[] ListaTestowa
        {
            get { return tripLengthList; }
            set
            {
                tripLengthList = value;
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
        private string _typ;
        public string Typ
        {
            get { return _typ; }
            set { _typ = value;
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
                Typ = typyRejestrow[aktualnyRejestr.Typ];
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



        //event, prosze nie modyfikowac
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropetryChanged([CallerMemberName]string propetryName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propetryName));
        }
    }
}
