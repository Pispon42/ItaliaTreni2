using System.ComponentModel.DataAnnotations;

namespace ItaliaTreni.Models
{
    public class OutOfScaleMeasurement
    {
        public OutOfScaleMeasurement(int mmId, int mm_fine, double e1, double s1, double e2, double s2, double e3, double s3, double e4, double s4)
        {
            this.mmId = mmId;
            this .mm_fine = mm_fine;
            this .e1 = e1;
            this .e2 = e2;
            this .e3 = e3;
            this .e4 = e4;
            this .s1 = s1;
            this .s2 = s2;
            this .s3 = s3;
            this .s4 = s4;

        }

        [Key]
        public int mmId { get; set; }

        public int mm_fine { get; set; }

        public double e1 { get; set; }

        public double s1 { get; set; }

        public double e2 { get; set; }

        public double s2 { get; set; }

        public double e3 { get; set; }

        public double s3 { get; set; }

        public double e4 { get; set; }

        public double s4 { get; set; }

        //LETTURA SU SCHERMO 
        public void outOnScreen()
        {
            Console.WriteLine
               (
                    $"Estensione --> mm (inizio): {this.mmId}    mm (fine): {this.mm_fine}" +
                    $"\nErrori (valore misurato - soglia) e realtive soglie: " +
                    $"\n    - Errore su p1: {this.e1}   (soglia: {this.s1})" +
                    $"\n    - Errore su p2: {this.e2}   (soglia: {this.s2})" +
                    $"\n    - Errore su p3: {this.e3}   (soglia: {this.s3})" +
                    $"\n    - Errore su p4: {this.e4}   (soglia: {this.s4})\n"
               );
        }

    }
}
