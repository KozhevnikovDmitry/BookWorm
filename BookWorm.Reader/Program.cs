using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BookWorm.Messages;
using EasyNetQ;

namespace BookWorm.Reader
{
    class Program
    {
        private const int Amount = 10000;
        private static readonly Dictionary<string, int> _frequencies = new Dictionary<string, int>();
        private static int _sentMsgs;
        private static int _receivedMsgs;

        static void Main()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();


            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                bus.Subscribe<FrequenciesMsg>("server", OnFrequenciesCome);

                using (var reader = new StreamReader(new FileStream("tolstoy.txt", FileMode.Open)))
                {
                    while (reader.Peek() >= 0)
                    {
                        var buffer = new char[Amount];
                        reader.ReadBlock(buffer, 0, Amount);
                        bus.Send("analysis", new TextMsg { Text = new string(buffer) });
                        _sentMsgs++;
                    }
                }

                while (_sentMsgs != _receivedMsgs)
                {

                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                var result = new ResultsMsg
                {
                    Frequencies = _frequencies.OrderByDescending(t => t.Value)
                        .Where(t => t.Key.Length > 4)
                        .Take(10)
                        .ToDictionary(t => t.Key, t => t.Value),
                    ProcessTime = ts.TotalMilliseconds
                };

                bus.Publish(result);
                OnResultComes(result);
            }
        }
        
        static void OnFrequenciesCome(FrequenciesMsg frequenciesMsg)
        {
            _receivedMsgs++;
            foreach (var frequency in frequenciesMsg.Frequencies)
            {
                if (_frequencies.ContainsKey(frequency.Key))
                {
                    _frequencies[frequency.Key] += frequency.Value;
                }
                else
                {
                    _frequencies[frequency.Key] = frequency.Value;
                }
            }
        }

        static void OnResultComes(ResultsMsg resultsMsg)
        {
            Console.Clear();
            foreach (var frequency in resultsMsg.Frequencies)
            {
                Console.WriteLine("{0} apears {1} times", frequency.Key, frequency.Value);
            }

            Console.WriteLine("Processing takes {0} msec", resultsMsg.ProcessTime);
        }
    }
}
