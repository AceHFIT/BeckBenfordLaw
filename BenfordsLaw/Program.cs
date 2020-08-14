using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Drawing;
using System.Text.RegularExpressions;
namespace BenfordsLaw
{
    class Program
    {
        public static List<InvoiceData> invoicedata = new List<InvoiceData>();
        public static FileStream fs;
        static void Main(string[] args)
        {
            FileInfo invoiceworksheet = GetFileInfo("extract\\invoices.csv", false);
            string zipPath = @".\invoices.zip";
            string extractPath = @".\extract";
            List<InvoiceData> vendorInvoices = new List<InvoiceData>();
            if (File.Exists(@".\results.txt")) File.Delete(@".\results.txt");
            fs = File.Create(@".\results.txt");
            try
            {
                if (File.Exists(invoiceworksheet.FullName))
                    File.Delete(invoiceworksheet.FullName);

                ZipFile.ExtractToDirectory(zipPath, extractPath);
                string invoiceAmts = ParsecsvFile(invoiceworksheet.FullName);
                ProcessBenfordLaw(invoiceAmts, null);
                ProcessVendorInvoice();
                Console.WriteLine("Your Results file have been created.");
                Console.WriteLine("Please, Input a single line of new invoice data");
                Console.WriteLine("Enter Vendor:");
                string nVendor = Console.ReadLine();
                Console.WriteLine("Enter your Invoice Date:");
                string nInvoiceDate = Console.ReadLine();
                Console.WriteLine("Enter your Job #:");
                string nJobno = Console.ReadLine();
                Console.WriteLine("Enter your Invoice Amount:");
                string nInvoiceAmt = Console.ReadLine();
                var newMatch = invoicedata.
                                Where(c => (c.sVendor == nVendor) && (c.sJobNo == nJobno) && (c.sInvoiceAmt == Convert.ToDecimal(nInvoiceAmt))).
                                SelectMany(c => c.sVendor, (c, o) => new { c, o })
                                .Select(co => new { co.c.sVendor, co.c.sInvoiceAmt, co.c.sInvoiceDate, co.c.sJobNo }).ToList();
                if (newMatch.Count > 0)
                {
                    Console.WriteLine("New invoice entry matches the same pattern as existing invoices for vendor\n");
                    Console.WriteLine("A score of 1 was issued.");
                }
                else
                {
                    Console.WriteLine("New invoice entry does not match the same pattern as existing invoices for vendor\n");
                    Console.WriteLine("A score of 0 was issued.");
                }
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                
            }
        }
        public static FileInfo GetFileInfo(string file, bool deleteIfExists = true)
        {
            var fi = new FileInfo(file);
            if (deleteIfExists && fi.Exists)
            {
                fi.Delete();  // ensures we create a new workbook
            }
            return fi;
        }

        private static string ParsecsvFile(string filepath)
        {
            int aColumnCount = 0;
            string invoiceAmt = "";
            try
            {
                string[] lines = System.IO.File.ReadAllLines(filepath);
                foreach (string line in lines)
                {
                    aColumnCount = aColumnCount + 1;
                    string[] columns = line.Split(',');
                    // If count greater than 1 then not on row with column names
                    if (aColumnCount > 1)
                    {
                        if (String.IsNullOrEmpty(columns[3])) { columns[3] = "0"; }
                        invoiceAmt += columns[3] + ",";
                        invoicedata.Add(new InvoiceData
                        {
                            sVendor = columns[0],
                            sInvoiceDate = columns[1],
                            sJobNo = columns[2],
                            sInvoiceAmt = Convert.ToDecimal(columns[3]) 
                        });
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return invoiceAmt;
        }

        private static void ProcessBenfordLaw(string invoicedata, string vendorName = "")
        {
            try
            {
                printOutAllResult(countDigits(extractDigitsFromText(invoicedata)), vendorName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
            }
        }

        static void printOutAllResult(Dictionary<int, int> countedNumbers, string vendorName = "")
        {
            bool IsBenfordLaw = true;
            int numberOfNumbers = sumValues(countedNumbers);
            int tmpPOM = 0;
            for (int i = 1; i < 10; i++)
            {
                int pom = howMuchProcent(countedNumbers[i], numberOfNumbers);
                //Console.WriteLine("Digit {0} occurred {1}\t times, which is {2}\t procent.", i, countedNumbers[i], pom);
                AddText(fs, "Digit " + i.ToString() + " occurred " + countedNumbers[i].ToString() + " times, which is " + pom.ToString() + " procent.\n");
                switch (i)
                {
                    case 1:
                        tmpPOM = pom;
                        break;
                    default:
                        if (tmpPOM >= pom)
                            tmpPOM = pom;
                        else
                            IsBenfordLaw = false;
                        break;
                }
            }
            if (String.IsNullOrEmpty(vendorName))
            { if (IsBenfordLaw) AddText(fs, "These invoices follow the Traditional Benford's Law.\n\n"); else AddText(fs, "These invoices do not follow the Traditional Benford's Law.\n\n"); }
            else
            { if (IsBenfordLaw) AddText(fs, vendorName.ToString() + " follows the Traditional Benford's Law.\n\n"); else AddText(fs, vendorName.ToString() + " does not follow the Traditional Benford's Law.\n\n"); }
        }

        static int howMuchProcent(int part, int all)
        {
            return part * 100 / all;
        }

        static Dictionary<int, int> countDigits(LinkedList<int> numbers)
        {
            Dictionary<int, int> result = new Dictionary<int, int>();
            for (int i = 0; i < 10; i++)
            {
                result.Add(i, 0);
            }
            foreach (int i in numbers)
            {
                result[i] = result[i] + 1;
            }
            return result;
        }

        static int highestDigit(int anyNumber)
        {
            if (anyNumber < 0)
            {
                anyNumber *= -1;
            }
            int result = 0;
            while (anyNumber != 0)
            {
                result = anyNumber;
                anyNumber /= 10;
            }
            return result;
        }

        static LinkedList<int> extractDigitsFromText(string text)
        {
            LinkedList<int> result = new LinkedList<int>();
            Regex r = new Regex(@"\d+", RegexOptions.IgnoreCase);
            Match m = r.Match(text);
            try
            {
                while (m.Success)
                {
                    Group g = m.Groups[0];
                    int netDigit = highestDigit(Int32.Parse(g.Value));
                    result.AddFirst(netDigit);
                    m = m.NextMatch();
                }
            }
            catch (Exception e)
            {

            }
            return result;
        }

        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }
        static int sumValues(Dictionary<int, int> digitsAndHowManyTimesTheyOccured)
        {
            int result = 0;
            for (int i = 0; i < 10; i++)
            {
                result += digitsAndHowManyTimesTheyOccured[i];
            }
            return result;
        }

        public static void ProcessVendorInvoice()
        {
            List<InvoiceData> newVendorList = new List<InvoiceData>();
            string vendInvAmt = "";
            var invMatch = from invdata in invoicedata.GroupBy(invdata => invdata.sVendor)
                                select new { sInvoiceAmt = invdata.First().sInvoiceAmt, cnt = invdata.Count(), sVendor = invdata.First().sVendor };

            foreach(var sInvoice in invMatch)
            {
                if (sInvoice.cnt >= 30)
                {
                    var newMatch = invoicedata.
                                Where(c => c.sVendor == sInvoice.sVendor).
                                SelectMany(c => c.sVendor, (c, o) => new { c, o })
                                .Select(co => new { co.c.sVendor, co.c.sInvoiceAmt }).ToList();

                    foreach (var sInv in newMatch)
                    {
                        vendInvAmt = sInv.sInvoiceAmt.ToString();
                    }
                    ProcessBenfordLaw(vendInvAmt, sInvoice.sVendor.ToString());
                }
            }
        }
    }
}
