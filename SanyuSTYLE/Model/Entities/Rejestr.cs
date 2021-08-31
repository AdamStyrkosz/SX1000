using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class Rejestr
    {
    public int Id { get; set; }
    public string NazwaRejestru { get; set; }
    public int Kategoria { get; set; }
    public int Typ { get; set; }
    public  string Opis { get; set; }
    public string ZakresWartosci { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
    public int WartoscDomyslna { get; set; }
    public string Etykieta { get; set; }

}

