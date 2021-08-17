using System.Collections.Generic;

public class StaticArrays
    {
        public static Dictionary<int, string> bledyAlarmow = new Dictionary<int, string>()
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
        public static Dictionary<int, string> typyRejestrow = new Dictionary<int, string>()
        {
            {0,"Tylko do odczytu"},
            {1,"Edycja w stanie zatrzymania lub pracy"},
            {2,"Edycja w stanie zatrzymania"}
        };

        public static Dictionary<int, string> listaKategorii = new Dictionary<int, string>() {
            {0, "P0XX (Funkcje monitorujące)"},
            {1, "P1XX (Funkcje podstawowe)"},
            {2, "P2XX (Funkcje wejścia/wyjścia)"},
            {3, "P3XX (Funkcje wejscia/wyjścia)"},
            {4, "P4XX (Opcje aplikacyjne)"},
            {5, "P5XX (PLC)"},
            {6, "P6XX (PLC)"},
            {7, "P7XX (Funkcje zaawansowane)"},
            {8, "P8XX (Funkcje zaawansowane)"},
        };
    }
