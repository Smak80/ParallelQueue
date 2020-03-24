using System;
using System.Collections.Generic;
using System.Threading;

namespace ParallelQueue
{
    class CommonData
    {
        public static readonly int maxSize = 4;
        private Queue<int>[] vals =
        {
            new Queue<int>(),
            new Queue<int>(),
            new Queue<int>()
        };
        public void Add(int index, int value)
        {
            index = Math.Abs(index % 3);
            var q = vals[index];
            Monitor.Enter(q);
            try
            {
                Console.WriteLine("До вставки {3}: ({0}, {1}, {2})", 
                        vals[0].Count, vals[1].Count, vals[2].Count, index+1
                    );
                while (q.Count >= maxSize)
                {
                    Console.WriteLine("Нельзя добавить производителю {0}", index+1);
                    Monitor.Wait(q);
                }
                q.Enqueue(value);
                Console.WriteLine("После вставки {3}: ({0}, {1}, {2})", 
                    vals[0].Count, vals[1].Count, vals[2].Count, index+1
                );
                Monitor.PulseAll(q);
            }
            catch (Exception e) { }
            finally
            {
                Monitor.Exit(q);
            }
        }

        public int[] GetNextTriplet()
        {
            int[] res = {0, 0, 0};
            for (int i = 0; i < vals.Length; i++)
            {
                var q = vals[i];
                Monitor.Enter(q);
                try
                {
                    Console.WriteLine("До получения {3}: ({0}, {1}, {2})", 
                        vals[0].Count, vals[1].Count, vals[2].Count, i+1
                    );
                    while (q.Count == 0)
                    {
                        Console.WriteLine("Нет данных от производителя {0}", i+1);
                        Monitor.Wait(q);
                    }
                    res[i] = q.Dequeue();
                    Console.WriteLine("После получения {3}: ({0}, {1}, {2})", 
                        vals[0].Count, vals[1].Count, vals[2].Count, i+1
                    );
                    Monitor.PulseAll(q);
                }
                catch (Exception e)
                {
                }
                finally
                {
                    Monitor.Exit(q);
                }
            }
            return res;
        }
    }

    class Producer
    {
        public static readonly int valNum = 3;
        private int valIndex;
        private Thread t;
        private Random r;
        private CommonData d;

        public Producer(CommonData d, int valIndex)
        {
            this.valIndex = Math.Abs(valIndex % valNum);
            r = new Random((int)DateTime.Now.Ticks);
            this.d = d;
            Start();
        }

        void Produce()
        {
            int i = 0;
            while (true)
            {
                var waitTime = r.Next(
                    500 * (1+Math.Abs(valIndex-1)),
                    3000* (1+Math.Abs(valIndex-1))
                );
                Console.WriteLine("Производитель {0}: жду {1} мс.", valIndex+1, waitTime);
                Thread.Sleep(waitTime);
                var value = r.Next(0, 1000);
                Console.WriteLine("Производитель {0} сгененрировал число #{1}={2}", valIndex+1, ++i, value);
                d.Add(valIndex, value);
            }
        }
        private void Start()
        {
            ThreadStart th = new ThreadStart(Produce);
            t = new Thread(th);
            t.Start();
        }

        public void Abort()
        {
            try
            {
                t.Abort();
                t.Join();
            } catch (Exception e){ }
        }
    }

    class Consumer
    {
        private Thread t;
        private CommonData d;
        private static Consumer cons = null;

        private Consumer(CommonData d)
        {
            this.d = d;
            Start();
        }

        public static Consumer getInstance(CommonData d)
        {
            if (cons == null) cons = new Consumer(d);
            return cons;
        }

        private void Start()
        {
            ThreadStart th = new ThreadStart(Consume);
            t = new Thread(th);
            t.Start();
        }

        private void Consume()
        {
            int i = 0;
            while (true)
            {
                var value = d.GetNextTriplet();
                var s = 0;
                foreach (var v in value)
                {
                    s += v;
                }
                Console.WriteLine("Consumer result #{0} = {1}", ++i, s);
            }
        }

        public void Abort()
        {
            try
            {
                t.Abort();
                t.Join();
            }
            catch (Exception e)
            {
            }
            finally
            {
                cons = null;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadKey();
            CommonData d = new CommonData();
            Consumer c = Consumer.getInstance(d);
            Producer []p = new Producer[3];
            for (int i = 0; i < Producer.valNum; i++)
            {
                p[i] = new Producer(d, i);
            }
            Thread.Sleep(30000);
            c.Abort();
            for (int i = 0; i < Producer.valNum; i++)
            {
                p[i].Abort();
            }

            Console.ReadKey();
        }
    }
}
