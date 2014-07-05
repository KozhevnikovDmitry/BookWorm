using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BookWorm.Messages;
using EasyNetQ;

namespace BookWorm.Analyse
{
    class Program
    {
        private static int _count;

        static void Main()
        {
            using (var bus = RabbitHutch.CreateBus(Settings.Default.host))
            {
                bus.Subscribe<ResultsMsg>(Guid.NewGuid().ToString(), OnResultComes);
                bus.Receive<TextMsg>("analysis", t => bus.Publish(new FrequenciesMsg { Frequencies = Analyse(t) }));
 
                Console.WriteLine("Waiting for a text to analyse...");
                Console.ReadLine();
            }
        }


        private static void OnResultComes(ResultsMsg resultsMsg)
        {
            Console.Clear();
            foreach (var frequency in resultsMsg.Frequencies)
            {
                Console.WriteLine("{0} apears {1} times", frequency.Key, frequency.Value);
            }

            Console.WriteLine("{0} fragments was analysed", _count);
            Console.WriteLine("Processing takes {0} msec", resultsMsg.ProcessTime);
        }

        private static Dictionary<string, int> Analyse(TextMsg textMsg)
        {
            _count++;
            Thread.Sleep(100);
            var result = new Dictionary<string, int>();
            var words = textMsg.Text.Split(new[] { ' ', '.', ',', ':', '!', '?', '(', ')', '[', ']', '-', '\n', '\r' }).Select(t => t.Trim().ToUpper());
            foreach (var word in words)
            {
                if (result.ContainsKey(word))
                {
                    result[word]++;
                }
                else
                {
                    result[word] = 1;
                }
            }

            Console.WriteLine("Text fragment {0} was analyse succefully", _count);
            return result;
        }

    }
}
