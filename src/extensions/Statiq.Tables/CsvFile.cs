﻿using System.Collections.Generic;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace Statiq.Tables
{
    internal static class CsvFile
    {
        public static IEnumerable<IEnumerable<string>> GetAllRecords(Stream stream, string delimiter = null)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                return GetAllRecords(reader, delimiter);
            }
        }

        public static IEnumerable<IEnumerable<string>> GetAllRecords(TextReader reader, string delimiter = null)
        {
            List<IEnumerable<string>> records = new List<IEnumerable<string>>();
            Configuration configuration = delimiter is null ? new Configuration { HasHeaderRecord = false } : new Configuration { HasHeaderRecord = false, Delimiter = delimiter };

            using (CsvReader csv = new CsvReader(reader, configuration))
            {
                while (csv.Read())
                {
                    string[] currentRecord = csv.Context.Record;
                    records.Add(currentRecord);
                }
            }

            return records;
        }

        public static void WriteAllRecords(IEnumerable<IEnumerable<string>> records, Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream, leaveOpen: true);
            WriteAllRecords(records, writer);
            writer.Flush();
        }

        public static void WriteAllRecords(IEnumerable<IEnumerable<string>> records, TextWriter writer)
        {
            if (records is null)
            {
                return;
            }

            CsvWriter csv = new CsvWriter(writer, new Configuration { QuoteAllFields = true });
            {
                foreach (IEnumerable<string> row in records)
                {
                    foreach (string cell in row)
                    {
                        csv.WriteField(cell ?? string.Empty);
                    }
                    csv.NextRecord();
                }
            }
        }
    }
}
