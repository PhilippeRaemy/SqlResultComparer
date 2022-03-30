
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ResultsComparer;

namespace SqlResultComparer
{
    static class Runner
    {
        public static void Run(string leftConnStr, string leftQuery, string rightConnStr, string rightQuery, Action<string> consoleWriter , string[] parameters, Action<int> pauser,
    string[] parmNames, Action<string[],string[],string[]> resultsWriter)
        {
            var loopCount = 0;
            using (var leftConn = new SqlConnection(leftConnStr))
            using (var leftCmd = new SqlCommand(leftQuery, leftConn))
            using (var rightConn = new SqlConnection(rightConnStr))
            using (var rightCmd = new SqlCommand(rightQuery, rightConn))
            {
                leftConn.Open();
                consoleWriter("Left Connection opened");
                rightConn.Open();
                consoleWriter("Right Connection opened");

                leftCmd.Prepare();
                rightCmd.Prepare();

                leftCmd.CommandTimeout = rightCmd.CommandTimeout = 3000;
                foreach (var parmLine in parameters.Skip(1).Where(l => !l.StartsWith("#")))
                {
                    if (loopCount > 0)
                    {
                        leftCmd.Parameters.Clear();
                        rightCmd.Parameters.Clear();
                        pauser(loopCount);
                    }
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var parmValues = parmLine.Split(',');
                    var lines = RunOne(consoleWriter, parmNames, resultsWriter, parmValues, leftCmd, rightCmd);
                    consoleWriter($"Processed {lines.Length} output lines in loop {++loopCount}.");
                }
            }
        }

        static string[] RunOne(Action<string> consoleWriter, string[] parmNames, Action<string[], string[], string[]> resultsWriter, string[] parmValues, SqlCommand leftCmd, SqlCommand rightCmd)
        {
            foreach (var p in parmNames.Zip(
                parmValues,
                (n, v) => new {lp = new SqlParameter(n, SqlDbType.VarChar) {Value = v}, rp = new SqlParameter(n, SqlDbType.VarChar) {Value = v}}
                )
            )
            {
                leftCmd.Parameters.Add(p.lp);
                rightCmd.Parameters.Add(p.rp);
            }

            var arLeft = leftCmd.BeginExecuteReader(null, null);
            consoleWriter("Left query Launched");
            var arRight = rightCmd.BeginExecuteReader(null, null);
            consoleWriter("Right query Launched");

            var left =
                new
                {
                    NameProper = "Query Left",
                    NameLower = "query left",
                    AsyncResult = arLeft,
                    Command = leftCmd,
                    Index = 0,
                    type = "qry",
                    task = (Task<Results>) null
                };
            var right =
                new
                {
                    NameProper = "Query Right",
                    NameLower = "query right",
                    AsyncResult = arRight,
                    Command = rightCmd,
                    Index = 1,
                    type = "qry",
                    task = (Task<Results>) null
                };
            var flights = new[] {left, right}.ToList();
            var resultsRefs = new Results[flights.Count];
            var readerRefs = new SqlDataReader[flights.Count];
            var readers = new List<SqlDataReader>();
            string[] lines;

            using (readerRefs[left.Index])
            using (readerRefs[right.Index])
            { 
                while (flights.Count > 0)
                {
                    var index = WaitHandle.WaitAny(flights.Select(ar => ar.AsyncResult.AsyncWaitHandle).ToArray());
                    var landing = flights[index];
                    consoleWriter($"{landing.NameProper} is back");
                    if (landing.type == "qry")
                    {
                        var reader = readerRefs[landing.Index] = landing.Command.EndExecuteReader(landing.AsyncResult);
                        readers.Add(reader); // keep it for closing later
                        try
                        {
                            var task = ResultsFromReader(reader);
                            flights.Add(new
                            {
                                NameProper = "Reader " + landing.NameProper,
                                NameLower  = "reader " + landing.NameLower,
                                AsyncResult = (IAsyncResult) task,
                                landing.Command,
                                Index = landing.Index + 2,
                                type = "rdr",
                                task
                            });
                        }
                        catch (Exception e)
                        {
                            consoleWriter($"Error in {landing.NameLower}: {e.Message}");
                        }
                    }
                    else
                    {
                        resultsRefs[landing.Index - 2] = landing.task.Result;
                    }
                    flights.RemoveAt(index);
                }

                consoleWriter("Comparing results...");
                var lrc = resultsRefs[left.Index];
                var rrc = resultsRefs[right.Index];

                lines = lrc.Diff(rrc)
                    .ToString()
                    .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            }

            resultsWriter(lines, parmNames, parmValues);

            foreach (var reader in readers)
            {
                reader.Close();
                reader.Dispose();
            }
            return lines;
        }

        static async Task<Results> ResultsFromReader(SqlDataReader reader)
        {
            var results = new Results();
            await Task.Run(() => results.AddReader(reader));
            return results; //  new Task<Results>(() => results);
        }
    }
}
