using System.ComponentModel.DataAnnotations;

namespace ItaliaTreni.Models
{
    public class Measurement
    {
        public Measurement(int mmId, float p1, float p2, float p3, float p4)
        {
            this.mmId = mmId;
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.p4 = p4;
        }

        [Key]
        public int mmId { get; set; }

        public float p1 { get; set; }

        public float p2 { get; set; }

        public float p3 { get; set; }

        public float p4 { get; set; }

        //LETTURA SU SCHERMO 
        public void outOnScreen()
        {
            Console.WriteLine
               (
                   $"mm: {this.mmId}    " +
                   $"p1: {this.p1}  " +
                   $"p2: {this.p2}  " +
                   $"p3: {this.p3}  " +
                   $"p4: {this.p4}"
               );
        }


    }
}
