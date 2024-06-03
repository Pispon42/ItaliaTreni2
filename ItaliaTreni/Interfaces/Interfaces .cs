using System;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using CsvHelper;
using ItaliaTreni.Data;
using ItaliaTreni.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;

namespace ItaliaTreni.Interfaces
{
    public class Interfaces
    {
        //MENU INIZIALE
        public void startOptions()
        {
            bool endapp = false;
            using ItaliaTreniContext contex = new ItaliaTreniContext();
            
            int maximport = 10000; //valore massimo di importazioni per chiamata (1 misurazione = 1mm => 10 000 misurazioni = 10m)
            List<Measurement> measurements = new List<Measurement>(); //per immagazzinare tutti i dati ricevuti dal db

            while (!endapp)//il menù viene riproposto fino a che non si digita un comando valido oppure '4' per uscire
            {
                Console.WriteLine("ItaliaTreni <Analisi Binari>, operazioni disponibili");
                Console.WriteLine(" 1) Importazione delle misurazioni effettuate");
                Console.WriteLine(" 2) Consultazione delle misurazioni registrate");
                Console.WriteLine(" 3) Consultazione dei valori fuori soglia registrati");
                Console.WriteLine(" 4) Analisi dei dati");
                Console.WriteLine(" 5) Esci");
                Console.Write("Digita il numero corrispondente all'operazione che vorresti effettuare: ");
                int op = validOption();
                switch (op)
                {
                    case 1://lettura csv e importazione sul db
                        {
                            using var transaction = contex.Database.BeginTransaction();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Measurement ON"); //poichè la chiave (mm) viene inserita manualmente da me (caso eccezionale solo per semplificare l'esercitazione) 1/2
                            importNewContents(contex, maximport);
                            contex.SaveChanges();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Measurement OFF"); //chiusura 2/2
                            transaction.Commit();
                        }
                        break;

                    case 2://consultazione dei valori presenti sul db
                        {
                            using var transaction = contex.Database.BeginTransaction();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Measurement ON"); 
                            measurements = rangeOption(contex, maximport, 2);
                            contex.SaveChanges();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Measurement OFF"); 
                            transaction.Commit();
                        }
                        break;

                    case 3://consultazione errori
                        {
                            using var transaction = contex.Database.BeginTransaction();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Measurement ON");
                            measurements = rangeOption(contex, maximport, 3);
                            contex.SaveChanges();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Measurement OFF"); 
                            transaction.Commit();
                        }
                        break;

                    case 4://analisi dei dati
                        { 
                            using var transaction = contex.Database.BeginTransaction();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT OutOfScaleMeasurement ON"); 
                            measurements = rangeOption(contex, maximport, 4);
                            contex.SaveChanges();
                            contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT OutOfScaleMeasurement OFF"); 
                            transaction.Commit();
                        }
                        break;

                    case 5://uscita
                        Console.WriteLine("\nGrazie per aver utilizzato l'applicazione!");
                        endapp = true;
                        break;

                    default://numero digitato non valido
                        Console.WriteLine($"\n{op} non è un'opzione valida!\n");
                        break;
                }
            }

        }

        //MENU PER LETTURA O ANALISI
        private List<Measurement> rangeOption(ItaliaTreniContext contex, int maximport, int action)
        {
            List<Measurement> measurements = new List<Measurement>();
            Console.WriteLine("\nSeleziona un'opzione di lettura dei dati");
            Console.WriteLine(" 1) Tutti i dati presenti");
            Console.WriteLine(" 2) Dati in un certo range");
            Console.Write("Digita il numero corrispondente all'operazione che vorresti effettuare: ");
            int op = validOption();
            switch (op)
            {
                case 1://tutti i dati presenti
                    if (action == 3)//lettura di valori fuori scala registrate sul db
                    {
                        outOfScaleMeasurementRequest(contex, 0, contex.Measurement.Count(), maximport, action);
                    }
                    else//lettura delle misurazioni registrate sul db
                    {
                        measurementRequest(contex, 0, contex.Measurement.Count(), maximport, action);
                    }
                    break;
                case 2://selezionare un range
                    Console.Write("\nDigita il numero corrispondente al valore iniziale del mm: ");
                    int min = validOption();
                    Console.Write("Digita il numero corrispondente al valore finale del mm: ");
                    int max = checkMax(min);
                    if (action == 3)//lettura di valori fuori scala registrate sul db
                    {
                        outOfScaleMeasurementRequest(contex, min, max, maximport, action);
                    }
                    else//lettura delle misurazioni registrate sul db
                    {
                        measurementRequest(contex, min, max, maximport, action);
                    }
                    break;
                default:
                    Console.WriteLine("Scelta non valida!\n");
                    break;
            }
            return measurements;
        }


        //VERIFICA SUL NUMERO DIGITATO (UTILIZZATO QUANDO VIENE RICHIESTO DI DIGITARE UN NUMERO)
        public int validOption()
        {
            bool opgiven = false;
            int op = 0;
            while (!opgiven)
            {
                try
                {
                    op = Convert.ToInt32(Console.ReadLine());
                    opgiven = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nErrore: {ex.Message}\n");
                    Console.Write("Digitare un numero valido: ");
                }
            }
            return op;
        }

        //VERIFICA CHE IL NUMERO INSERITO SIA MAGGIORE RISPETTO AD UN VALORE INSERITO PRECEDENTEMENTE
        public int checkMax(int value1) 
        {
            bool flag_value = false;
            int value2 = 0;
            while (!flag_value)
            { 
                value2 = validOption();
                
                if (value2 < value1)
                {
                    Console.WriteLine("Il valore digitato non può essere minore del precedente!");
                }
                else 
                {
                    flag_value = true;
                }

            }
            return value2;
        }
       

        //LETTURA CSV E IMPORTAZIONE SU DB (10M ALLA VOLTA) /////////AGGIUNGERE CONTROLLO DI DATI GIÀ PRESENTI (E SOVRASCRITTURA)
        private void importNewContents(ItaliaTreniContext contex, int maximport)
        {
            
            Console.WriteLine("\nLettura dei valori misurati dal treno in corso...");
            //inizializzazione lettura del csv e interfaccia con il db
            using (var streamReader = new StreamReader(@"D:\progettiC#\ItaliaTreni\ItaliaTreni\Measurements\ff3f3add-7b1d-4f08-ba27-7a9c24fbcd34.csv"))
            {
                using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                {
                    //Caricamento dei valori dal csv alla lista
                    List<Measurement> measurementlist = new List<Measurement>();
                    csvReader.Read();
                    while (csvReader.Read())
                    {
                        measurementlist.Add
                        (
                            new Measurement
                            (
                                Int32.Parse(csvReader.GetField(0)),
                                float.Parse(csvReader.GetField(1), CultureInfo.InvariantCulture),
                                float.Parse(csvReader.GetField(2), CultureInfo.InvariantCulture),
                                float.Parse(csvReader.GetField(3), CultureInfo.InvariantCulture),
                                float.Parse(csvReader.GetField(4), CultureInfo.InvariantCulture)
                            )
                        );
                    }


                    List<Measurement> measurementListDB = new List<Measurement>();
                    //interfacciamento con il db, inizio della transazione
                    Console.WriteLine("\nImportazione dei valori su DB in corso...");
                    int importedM = 0; // con importedM si tiene conto dei metri totali

                    //il for permette di importare sul db tutti i valori ricavati dal csv anche quando questi sono più di 10m
                    for (int i = 0; i < measurementlist.Count; importedM++)
                    {
                        measurementListDB = measurementRequest(contex, i, (i + maximport), maximport, 1);
                        DateTime rtt = DateTime.Now; //per calcolare il tempo necessario nella trasmissione di 10m di dati
                        Console.WriteLine($"\nImportazione misurazioni sul m: {importedM} - START mm: {measurementlist[i].mmId} - {rtt.ToString()}");//TEST - duarata 10m di import 1/2

                        //per effettuare 10m di chiamate per volta (j è utilizzato per verificare se venogno raggiunti i 10m di dati importati)
                        
                        int j = 0;
                        while (j < maximport & measurementlist.Count > i)
                        {
                           
                            if (measurementListDB.Find(x => x.mmId == measurementlist[i].mmId) != null)
                            {
                                var pastMeasurment = contex.Measurement.Find(measurementlist[i].mmId);
                                contex.Measurement.Remove(pastMeasurment);
                                contex.Measurement.Add(measurementlist[i]);
                            }
                            else
                            {
                                contex.Measurement.Add(measurementlist[i]);
                            }
                            i++; //con i si tiene il conto dei mm totali (varia da 0 al totale dei valori nel csv)
                            j++; //con j si tiene conto dei mm trattati da inizio chiamata (varia da 0 a maximport)
                        }

                        Console.WriteLine($"Importazione misurazioni sul m: {importedM} - END mm {measurementlist[i - 1].mmId} - {DateTime.Now.ToString()} - Tempo impiegato: {(DateTime.Now - rtt).TotalSeconds} secondi\n");//TEST - duarata 10m di import 2/2
                    }
                    //fine della transazione (dopo tutto l'import)
                }
            }
        }


        //LETTURA MISURAZIONI DAL DB (10M ALLA VOLTA)
        private List<Measurement> measurementRequest(ItaliaTreniContext contex, int min, int max, int maximport, int action)
        {
            if (max > contex.Measurement.Count()) { max = contex.Measurement.Count(); } //Per evitare overflow
            List<Measurement> measurementList = new List<Measurement>();

            for (int i = min; i < max;) //interazione con il database dal minimo al massimo specificato (in mm)
            {
                //dati necessari per l'analisi
                bool flag_last = false;
                bool flag_long = false;
                int first_mm = 0;
                int s1 = 1;
                int s2 = 2;
                int s3 = 3;
                if (action == 4) //l'impostazione delle soglie serve solo quando effettivamente si esegue l'analisi
                {
                    Console.Write("\nDigita il numero corrispondente alla soglia \"lieve\": ");
                    s1 = validOption();
                    Console.Write("\nDigita il numero corrispondente alla soglia \"moderata\": ");
                    s2 = checkMax(s1);
                    Console.Write("\nDigita il numero corrispondente alla soglia \"grave\": ");
                    s3 = checkMax(s2);
                }
                double[] long_errors = [0, 0, 0, 0, 0, 0, 0, 0];
                List<OutOfScaleMeasurement> pastAnalisysList = new List<OutOfScaleMeasurement>();
                if (action == 4) //lista con tutte le misure fuori scala presenti sul db (serve per l'analisi)
                {
                    pastAnalisysList = outOfScaleMeasurementRequest(contex, min, max, maximport, 4);
                }


                DateTime rtt = DateTime.Now;    //per il calcolo della durata
                //Console.WriteLine($"\nInizio operazione sul mm: {i} - Timestamp: {rtt}\n");//Duarata lettura 10m 1/2


                if (max - i > maximport) //legge 10m di dati
                {
                    var page = contex.Measurement
                          .Where(m => m.mmId >= i & m.mmId < i + maximport)
                          .OrderBy(m => m.mmId);

                    int final_mm = page.Last().mmId;
                    //contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT OutOfScaleMeasurement ON"); //poichè la chiave (mm) viene inserita manualmente da me (caso eccezionale solo per semplificare l'esercitazione) 1/2
                    foreach (var p in page) //per ognuna di esse esegue la stampa su schermo oppure  effettua l'analisi
                    {
                        if (action == 1) 
                        {
                            measurementList.Add(p);
                        }
                        else if (action == 4)//così esegue l'analisi ogni 10m
                        {
                            if (p.mmId == final_mm) { flag_last = true; }
                            var analized_mis = analisi(p, flag_long, flag_last, first_mm, long_errors, s1, s2, s3, contex, pastAnalisysList);
                            flag_long = analized_mis.Item1;
                            first_mm = analized_mis.Item2;
                            long_errors = analized_mis.Item3;
                        }
                        else
                        {

                            p.outOnScreen();
                        }
                    }
                    if (action != 1) 
                    {
                        Console.WriteLine($"\nOperazione dal mm: {i} al mm: {i + maximport} completata - Durata totale: {(DateTime.Now - rtt).TotalSeconds}");//Duarata operazione 10m 2/2
                        Console.WriteLine("Premere INVIO per procedere...");
                        Console.ReadLine();
                    }
                    //contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT OutOfScaleMeasurement OFF"); //Chiusura 2/2
                    i += maximport;
                }
                else //legge meno di 10m di dati
                {
                    var page = contex.Measurement
                                .Where(m => m.mmId >= i & m.mmId < max)
                                .OrderBy(m => m.mmId);

                    int final_mm = page.Last().mmId;
                    //contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT OutOfScaleMeasurement ON"); //poichè la chiave (mm) viene inserita manualmente da me (caso eccezionale solo per semplificare l'esercitazione) 1/2
                    foreach (var p in page) //azioni
                    {
                        if (action == 1)
                        {
                            measurementList.Add(p);
                        }
                        else if (action == 4)
                        {
                            if (p.mmId == final_mm) { flag_last = true; }
                            var analized_mis = analisi(p, flag_long, flag_last, first_mm, long_errors, s1, s2, s3, contex, pastAnalisysList);
                            flag_long = analized_mis.Item1;
                            first_mm = analized_mis.Item2;
                            long_errors = analized_mis.Item3;
                        }
                        else
                        {
                            p.outOnScreen();
                        }
                    }
                    if (action != 1)
                    {
                        Console.WriteLine($"\nOperazione dal mm: {i} al mm: {i + maximport} completata - Durata totale: {(DateTime.Now - rtt).TotalSeconds}");//Duarata operazione 10m 2/2
                        Console.WriteLine("Premere INVIO per procedere...");
                        Console.ReadLine();
                    }
                    //contex.Database.ExecuteSqlRaw("SET IDENTITY_INSERT OutOfScaleMeasurement OFF"); //Chiusura 2/2
                    i = max;
                }
      
            }
            return measurementList;
        }

        //LETTURA MISURAZIONI FUORI SCALA DAL DB (10M ALLA VOLTA)
        private List<OutOfScaleMeasurement> outOfScaleMeasurementRequest(ItaliaTreniContext contex, int min, int max, int maximport, int action)
        {
            if (max > contex.Measurement.Count()) { max = contex.Measurement.Count(); } //Per evitare overflow 
            List<OutOfScaleMeasurement> pastAnalisyslist = new List<OutOfScaleMeasurement>();

            for (int i = min; i < max;) //interazione con il database dal minimo al massimo specificato (in mm)
            {
                DateTime rtt = DateTime.Now;    //per il calcolo della durata
                //Console.WriteLine($"\nInizio operazione sul mm: {i} - Timestamp: {rtt}\n");//Duarata lettura 10m 1/2

                if (max - i > maximport) //legge 10m di dati
                {
                    var page = contex.OutOfScaleMeasurement
                          .Where(m => m.mmId >= i & m.mmId < i + maximport)
                          .OrderBy(m => m.mmId);

                    if (action == 3) //lettura dei valori fuori scala
                    {
                        Console.WriteLine($"\nOperazione dal mm: {i} al mm: {i+maximport} completata - Durata totale: {(DateTime.Now - rtt).TotalSeconds}\n");//Duarata operazione 10m 2/2
                        Console.WriteLine("\nPremere INVIO per procedere...");
                        Console.ReadLine();

                        foreach (var p in page) //per ognuna di esse esegue la stampa su schermo oppure  effettua l'analisi
                        {
                            Console.Write("\nErorre registrato -> ");
                            p.outOnScreen();
                        }
                    }
                    else
                    {
                        foreach (var p in page) //per ognuna di esse esegue la stampa su schermo oppure  effettua l'analisi
                        {
                            pastAnalisyslist.Add(p);
                        }
                    }

                    i += maximport;
                }
                else //legge meno di 10m di dati (gli ultimi rimasti)
                {
                    var page = contex.OutOfScaleMeasurement
                                .Where(m => m.mmId >= i & m.mmId < max)
                                .OrderBy(m => m.mmId);

                    if (action == 3)
                    {
                        Console.WriteLine($"\nOperazione dal mm: {i} al mm: {max} completata - Durata totale: {(DateTime.Now - rtt).TotalSeconds}\n");//Duarata operazione 10m 2/2
                        Console.WriteLine("\nPremere INVIO per procedere...");
                        Console.ReadLine();

                        foreach (var p in page) //per ognuna di esse esegue la stampa su schermo oppure  effettua l'analisi
                        {
                            Console.Write("\nErorre registrato -> ");
                            p.outOnScreen();
                        }
                    }
                    else
                    {
                        foreach (var p in page) //per ognuna di esse esegue la stampa su schermo oppure  effettua l'analisi
                        {
                            pastAnalisyslist.Add(p);
                        }
                    }
                    i += max;
                }

            }
            return pastAnalisyslist;
        }


        //ANALISI DI UNA SINGOLA MISURAZIONE E INSERIMENTO SUL DB (SE NON GIÀ PRESENTE)
        private (bool, int, double[]) analisi(Measurement m, bool flag_long, bool flag_last, int first_mm, double[] long_errors, int s1, int s2, int s3, ItaliaTreniContext contex, List<OutOfScaleMeasurement> pastAnalisysList)
        {

            //array che contiene gli errori con la relativa soglia
            double[] errors = [m.p1, 0, m.p2, 0, m.p3, 0, m.p4, 0];
            for (int i = 0; i < 4; i += 2)
            {
                if (errors[i] > 0)
                {
                    if (errors[i] >= s3)
                    {
                        errors[i] = errors[i] - s3;
                        errors[i + 1] = s3;
                    }
                    else if (errors[i] >= s2)
                    {
                        errors[i] = errors[i] - s2;
                        errors[i + 1] = s2;
                    }
                    else if (errors[i] >= s1)
                    {
                        errors[i] = errors[i] - s1;
                        errors[i + 1] = s1;
                    }
                    else
                    {
                        errors[i] = 0;
                        errors[i + 1] = 0;
                    }
                }
                else
                {
                    if (errors[i] <= -s3)
                    {
                        errors[i] = errors[i] + s3;
                        errors[i + 1] = s3;
                    }
                    else if (errors[i] <= -s2)
                    {
                        errors[i] = errors[i] + s2;
                        errors[i + 1] = s2;
                    }
                    else if (errors[i] <= -s1)
                    {
                        errors[i] = errors[i] + s1;
                        errors[i + 1] = s1;
                    }
                    else
                    {
                        errors[i] = 0;
                        errors[i + 1] = 0;
                    }
                }
                
            }
            // flag per segnalare la presenza di errori (ad ogni misurazione viene impostato a false di default)
            bool error_flag = false;
            //verifica l'array appena calcolate e se presente un errore "attiva" il flag, altrimenti resta false (come di default)
            for (int j = 0; j < 4; j++)
            {
                if (errors[j] != 0)
                {
                    error_flag = true;
                }
            }

            //c'è un errore nella misurazione
            if (error_flag == true)
            {
                //anche le misurazioni precedenti contenevano errori
                if (flag_long == true)
                {
                    //questa è l'ultima misurazione dell'analisi (dei 10m o del totale)
                    if (flag_last == true)
                    {
                        //Questa è l'ultima di una serie di misurazioni che riportano errori, dopo questa è finita completamente l'analisi, si procede con l'agigornamento con il db
                        OutOfScaleMeasurement newAnalisys = new OutOfScaleMeasurement
                                (
                                    mmId: first_mm,
                                    mm_fine: m.mmId,
                                    e1: long_errors[0],
                                    s1: long_errors[1],
                                    e2: long_errors[2],
                                    s2: long_errors[3],
                                    e3: long_errors[4],
                                    s3: long_errors[5],
                                    e4: long_errors[6],
                                    s4: long_errors[7]
                                );
                        compareAnalisys(contex, newAnalisys, pastAnalisysList);
                        return (flag_long = false, first_mm = first_mm, long_errors = long_errors);
                    }
                    //ci sono altre misurazioni
                    else
                    {
                        //Questa misurazione fa parte di una serie in cui sono già stati rilevati gli errori, bisogna procedere con l'analisi per valutare l'inserimento sul db.
                        return (flag_long = true, first_mm = first_mm, long_errors = long_errors);
                    }
                }
                //le misure precedenti non contenevano errori
                else
                {
                    //questa è l'ultima misurazione dell'analisi (dei 10m o del totale)
                    if (flag_last == true)
                    {
                        //Questa è la prima misurazione a contenere errori, tuttavia essonedo l'ultima da analizzare va inserita sul db
                        OutOfScaleMeasurement newAnalisys = new OutOfScaleMeasurement
                                (
                                     mmId: m.mmId,
                                     mm_fine: m.mmId,
                                     e1: errors[0],
                                     s1: errors[1],
                                     e2: errors[2],
                                     s2: errors[3],
                                     e3: errors[4],
                                     s3: errors[5],
                                     e4: errors[6],
                                     s4: errors[7]
                                 );
                        compareAnalisys(contex, newAnalisys, pastAnalisysList);
                        return (flag_long = false, first_mm = m.mmId, long_errors = errors);
                    }
                    //ci sono altre misurazioni
                    else
                    {
                        //la misurazione contiene errori, bisogna procedere con l'analisi per vedere se è l'unica o meno
                        return (flag_long = true, first_mm = m.mmId, long_errors = errors);
                    }
                }
            }
            //in questa misurazione non sono stati rilevati errori
            else
            {
                //anche le misurazioni precedenti contenevano errori
                if (flag_long == true)
                {
                    //nessun errore rilevato, la serie di errori precedenti è terminata con la misurazione precedente, sia che l'analisi termini qui o proceda è uguale
                    OutOfScaleMeasurement newAnalisys = new OutOfScaleMeasurement
                            (
                                mmId: first_mm,
                                mm_fine: m.mmId-1,
                                e1: long_errors[0],
                                s1: long_errors[1],
                                e2: long_errors[2],
                                s2: long_errors[3],
                                e3: long_errors[4],
                                s3: long_errors[5],
                                e4: long_errors[6],
                                s4: long_errors[7]
                            );
                    compareAnalisys(contex, newAnalisys, pastAnalisysList);
                    return (flag_long = false, first_mm = first_mm, long_errors = long_errors);

                }
                //le misurazioni precedenti non contenevano errori
                else
                {
                    //anche le misurazioni precedenti contenevano errori
                    if (flag_last == true)
                    {
                        //nessun errore rilevato, precedentemente non sono stati rilevati errori, l'analisi termina
                        return (flag_long = false, first_mm = first_mm, long_errors = long_errors);
                    }
                    //ci sono altre misurazioni
                    else
                    {
                        //nessun errore rilevato, precedentemente non sono stati rilevati errori, l'analisi continua
                        return (flag_long = false, first_mm = first_mm, long_errors = long_errors);
                    }
                }

            }
        }

        //PER VERIFICARE SE L'ERRORE RILEVATO NELL'ANALISI SIA GIÀ PRESENTE NEL DB E NEL CASO SE SOSTITUIRLO A QUESTO O MENO
        private void compareAnalisys(ItaliaTreniContext contex, OutOfScaleMeasurement newAnalisys, List<OutOfScaleMeasurement> pastAnalisysList) 
        {
            if (pastAnalisysList.Find(x => x.mmId == newAnalisys.mmId) != null)
            {
                var pastAnalisys = contex.OutOfScaleMeasurement.Find(newAnalisys.mmId);

                if (pastAnalisys.s1 == newAnalisys.s1 & pastAnalisys.s2 == newAnalisys.s2 & pastAnalisys.s3 == newAnalisys.s3 & pastAnalisys.s4 == newAnalisys.s4) //soglie uguali
                {
                    if (pastAnalisys.e1 == newAnalisys.e1 & pastAnalisys.e2 == newAnalisys.e2 & pastAnalisys.e3 == newAnalisys.e3 & pastAnalisys.e4 == newAnalisys.e4) //errori uguali
                    {
                        if (pastAnalisys.mm_fine == newAnalisys.mm_fine) //estensione uguale
                        {
                            //Console.WriteLine("\nErrore già presente sul db gia presente sul db!"); non serve avvisare semplicemente si passa oltre
                        }
                        else //estensione diversa
                        {
                            Console.WriteLine($"\nATTENZIONE: L'errore presente al mm: {pastAnalisys.mmId} ora si estende fino al mm: {newAnalisys.mm_fine} e non più fino al mm:{pastAnalisys.mm_fine}");
                            contex.OutOfScaleMeasurement.Remove(pastAnalisys);
                            contex.OutOfScaleMeasurement.Add(newAnalisys);
                        }
                    }
                    else //errori diversi
                    {
                        if (pastAnalisys.mm_fine == newAnalisys.mm_fine) //estensione identica
                        {
                            Console.WriteLine($"\n\nATTENZIONE: Errore rilevato al mm: {newAnalisys.mmId} già registrato sul DB ma con errori differenti (soglie ed estensione risultano costanti)!");
                            Console.WriteLine(" \n-----ERRORE REGISTRATO SUL DB-----\n");
                            pastAnalisys.outOnScreen();
                            Console.WriteLine(" \n-----NUOVO ERRORE RILEVATO-----\n");
                            newAnalisys.outOnScreen();
                            contex.OutOfScaleMeasurement.Remove(pastAnalisys);
                            contex.OutOfScaleMeasurement.Add(newAnalisys);
                        }
                        else //estensione uguale
                        {
                            Console.WriteLine($"\n\nATTENZIONE: Errore rilevato al mm: {newAnalisys.mmId} già registrato sul DB ma con errori ed estensione differenti (le soglie risultano costanti)!");
                            Console.WriteLine(" \n-----ERRORE REGISTRATO SUL DB-----\n");
                            pastAnalisys.outOnScreen();
                            Console.WriteLine(" \n-----NUOVO ERRORE RILEVATO-----\n");
                            newAnalisys.outOnScreen();
                            contex.OutOfScaleMeasurement.Remove(pastAnalisys);
                            contex.OutOfScaleMeasurement.Add(newAnalisys);
                        }
                    }
                }
                else //soglie diverse
                {
                    if (pastAnalisys.mm_fine == newAnalisys.mm_fine) //estensione uguale (se variano le soglie non serve controllare se gli errori sono variati!)
                    {
                        Console.WriteLine($"\n\nATTENZIONE: Errore rilevato al mm: {newAnalisys.mmId} già registrato sul DB ma con soglie differenti (l'estensione risulta costante)!");
                        Console.WriteLine(" \n-----ERRORE REGISTRATO SUL DB-----\n");
                        pastAnalisys.outOnScreen();
                        Console.WriteLine(" \n-----NUOVO ERRORE RILEVATO-----\n");
                        newAnalisys.outOnScreen();
                        Console.Write("Conservare questa nuova misurazione? (Digitare 1 per \"Si\" e qualsiasi altro tasto per \"No\": ");
                        int op = validOption();
                        if (op == 1)
                        {
                            contex.OutOfScaleMeasurement.Remove(pastAnalisys);
                            contex.OutOfScaleMeasurement.Add(newAnalisys);
                        }
                    }
                    else //estensione diversa
                    {
                        Console.WriteLine($"\n\nATTENZIONE: Errore rilevato al mm: {newAnalisys.mmId} già registrato sul DB ma con soglie ed estensione differenti!");
                        Console.WriteLine(" \n-----ERRORE REGISTRATO SUL DB-----\n");
                        pastAnalisys.outOnScreen();
                        Console.WriteLine(" \n-----NUOVO ERRORE RILEVATO-----\n");
                        newAnalisys.outOnScreen();
                        Console.Write("Conservare questa nuova misurazione? (Digitare 1 per \"Si\" e qualsiasi altro tasto per \"No\": ");
                        int op = validOption();
                        if (op == 1)
                        {
                            contex.OutOfScaleMeasurement.Remove(pastAnalisys);
                            contex.OutOfScaleMeasurement.Add(newAnalisys);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"\nNuovo erorre rilevato -> ");
                newAnalisys.outOnScreen();
                contex.OutOfScaleMeasurement.Add(newAnalisys);
            }
            
        }


    }
}
