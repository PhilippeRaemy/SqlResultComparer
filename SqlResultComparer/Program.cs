using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Mannex;
using SimpleCommandlineParser;

namespace SqlResultComparer
{
    using Parm = Parser.Parm;

    static class Program
    {

        private static int Main(string[] args)
        {
            var leftConnStr    = string.Empty;
            var rightConnStr   = string.Empty;
            var leftQuery      = string.Empty;
            var rightQuery     = string.Empty;
            var helpRequested  = false;
            var verbose        = false;
            var outputFormat   = "console";
            var pause          = 0;

            var parser = new Parser
            {
                ApplicationDescription = "SqlResultsComparer",
                ApplicationName        = "SqlResultsComparer",
                ErrorWriter            = Console.Error.WriteLine,
                HelpWriter             = Console.WriteLine
            };
            string[]  parameters =null;
            parser.AddRange(new[]
            {
                new Parm { Optional=true, Name="Connection"     , Lamda = s=>leftConnStr  = rightConnStr=s,                   Example = "Data Source=server;Initial Catalog=db;Trusted_Connection=true;", Help=  "The connection string for both queries." },
                new Parm { Optional=true, Name="Srv"            , Lamda = s=>leftConnStr  = rightConnStr=ParseSrv(s),         Example = "server.db",                                                      Help = "The server and database for both queries. Uses a trusted connection. This is an alternative to Connection parameter." },
                new Parm { Optional=true, Name="Query"          , Lamda = s=>leftQuery    = rightQuery  =s,                   Example = "select Foo from Bar",                                            Help = "The query to be executed on both connection."},
                new Parm { Optional=true, Name="QueryFile"      , Lamda = s=>rightQuery   = leftQuery   =File.ReadAllText(s), Example = "c:\\FooBar.sql'",                                                Help = "The file containing the query to be executed on both connection. This is an alternative to Query parameter"},
                new Parm { Optional=true, Name="ConnectionLeft" , Lamda = s=>leftConnStr  = s,                                Example = "Data Source=server;Initial Catalog=db;Trusted_Connection=true;", Help = "The left connection string." },
                new Parm { Optional=true, Name="ConnectionRight", Lamda = s=>rightConnStr = s,                                Example = "Data Source=server;Initial Catalog=db;Trusted_Connection=true;", Help = "The right connection string." },
                new Parm { Optional=true, Name="SrvLeft"        , Lamda = s=>leftConnStr  = ParseSrv(s),                      Example = "server.db",                                                      Help = "The left server and database. Uses a trusted connection." },
                new Parm { Optional=true, Name="SrvRight"       , Lamda = s=>rightConnStr = ParseSrv(s),                      Example = "server.db",                                                      Help = "The right server and database. Uses a trusted connection." },
                new Parm { Optional=true, Name="QueryLeft"      , Lamda = s=>leftQuery    = s,                                Example = "select Foo from BarLeft",                                        Help = "The query to be executed on the left connection."},
                new Parm { Optional=true, Name="QueryRight"     , Lamda = s=>rightQuery   = s,                                Example = "select Foo from BarRight",                                       Help = "The query to be executed on the right connection."},
                new Parm { Optional=true, Name="QueryFileLeft"  , Lamda = s=>leftQuery    = File.ReadAllText(s),              Example = "c:\\FooBarLeft.sql'",                                            Help = "The file containing the query to be executed on the left connection."},
                new Parm { Optional=true, Name="QueryFileRight" , Lamda = s=>rightQuery   = File.ReadAllText(s),              Example = "c:\\FooBarRight.sql'",                                           Help = "The file containing the query to be executed on the right connection."},
                new Parm { Optional=true, Name="Parameters"     , Lamda = s=>parameters   = File.ReadAllText(s).SplitIntoNonBlankLines().ToArray(),Example = "c:\\FooBarRight.csv'",                     Help = "A comma-delimited values file containing the parameters to be passed to the queries. THe first row will contain the parameter names. If this file is present, the queries will be executed on both connections once per parameterset, i.e. once per data line in the file. The lines starting with # are excluded from the test run."},
                new Parm { Optional=true, Name="ofmt"           , Lamda = s=>outputFormat = s,                                Example = "CONSOLE or HTML or BROWSER",                                     Help = "The output format. Default is `CONSOLE`. `HTML` outpus in HTML file. Additionnaly, `BROWSER` automatically opens the generated files in the default browser."},
                new Parm { Optional=true, Name="Verbose"        , Action = ()=> verbose=true,                                                                                                             Help = "Echo processing statistics to the console."},
                new Parm { Optional=true, Name="Pause"          , Lamda = s=>int.TryParse(s, out pause),                      Example = "30",                                                             Help = "the time to wait (in seconds) between each execution of the test queries. The default is 0, meaning no wait. a value of -1 indicates wait for a key to be pressed."},
                new Parm { Optional=true, Name="Help"           , Action = ()=> helpRequested=true,                                                                                                       Help = "Get this help."},
                new Parm { Optional=true, Name="Debug"          , Action = ()=> Debugger.Launch(),                                                                                                         Help = "Launch debugger."},
            });

            parser.ParseParameters(args);
            parser.EchoParameters();
            parser.RunLamdas();
            if (helpRequested)
            {
                Console.WriteLine(parser.GetHelp());
                return 0;
            }

            try
            {

                var startTime = DateTime.Now;
                Action<string> ts;
                if (verbose) ts = s => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}, {(DateTime.Now - startTime).TotalMilliseconds:000000}[ms]: {s}");
                else ts = _ => { };

                ts("Starting");

                string[] parmNames;
                if (parameters == null)
                {
                    parmNames = new string[] {};
                    parameters= new [] {null, string.Empty};
                }
                else { 
                    parmNames = parameters.FirstOrDefault()?.Split(',');
                }

                Action<string[], string[], string[]> resultWriter = null;
                if ("console".Equals(outputFormat, StringComparison.InvariantCultureIgnoreCase))
                {
                    resultWriter=ConsoleRenderer.Render;
                }
                if ("html"   .Equals(outputFormat, StringComparison.InvariantCultureIgnoreCase)
                ||  "browser".Equals(outputFormat, StringComparison.InvariantCultureIgnoreCase))
                {
                    resultWriter = new HtmlRenderer {AutoOpen="browser".Equals(outputFormat, StringComparison.InvariantCultureIgnoreCase)}.Render;
                }

                Runner.Run(leftConnStr, leftQuery, rightConnStr, rightQuery, ts, parameters, loopcount=> Pauser(loopcount, pause), parmNames, resultWriter);
                ts("Done");
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                //Console.Error.WriteLine(e.StackTrace);
                if (e.InnerException != null)
                    Console.Error.WriteLine(e.InnerException.Message);

                return 1;
            }
        }

        private static void Pauser(int loopCount, int pause)
        {
            if (pause == 0) return;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Hit any key to continue...");
            if (pause > 0)
                try
                {
                    ConsoleReader.ReadLine(1000 * pause);
                }
                catch (TimeoutException)
                {
                }
            else Console.ReadKey(intercept: true);
            Console.WriteLine($"Going on with loop # {loopCount}....");
            Console.ResetColor();
        }

        private static string ParseSrv(string s)
        {
            var parsed = s.Split('.');
            var srv = parsed[0];
            var db = parsed.Length > 1 ? $"Initial Catalog ={parsed[1]};" : string.Empty;
            return $"Data Source={srv};{db};Trusted_Connection=true;";
        }

    }
}
