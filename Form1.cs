using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime;
using System.Diagnostics;


namespace HTMLTest_3
{
    public partial class Form1 : Form
    {
        /*static string path = "http://127.0.0.1:5500/%D0%A2%D0%B5%D1%81%D1%82%D0%BE%D0%B2%D0%BE%D0%B5%20%D0%A2%D0%B5%D0%BB%D0%B5%D0%BC%D0%B0%D1%82%D0%B8%D0%BA%D0%B0%20(Hi,%20Rockits!).html";*/
        public Form1()
        {
            InitializeComponent();
            if (WB.Document == null)
            {
                WB.Navigate(System.IO.Directory.GetCurrentDirectory() + @"\Тестовое Телематика (Hi, Rockits!).html");
            }
        }

        private void WB_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            HtmlDocument Doc = WB.Document;
            HtmlElement StartBtn = Doc.GetElementById("Start"); 

            if (StartBtn != null)
            {
                StartBtn.AttachEventHandler("onclick", new EventHandler(CalculationStart));
            }
        }

        private void CalculationStart(object sender, EventArgs e)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            //FileStream fs = new FileStream("BankPage.html", FileMode.Open);
            HtmlDocument Doc = WB.Document;

            int Money = GetMoney(Doc);
            int Count = GetCount(Doc);
            bool[] IsWorking = GetIsWorking(Doc, Count);

            int[] Values;
            int[] Capacity;
            int[] Numbers = Sort(GetVal(Doc, Count), GetCapacity(Doc, Count), IsWorking, out Values, out Capacity);

            HtmlElement ElementOut = Doc.GetElementById("Ans");
            if (IsPossible(Money, Values, Capacity))
            {
                int[,] Issue = HowToIssue(Money, Values, Capacity, Numbers);
                
                if (Issue != null) ElementOut.SetAttribute("value", MakeOutString(Issue, Values));
                else ElementOut.SetAttribute("value", "Невозможно выдать такую сумму");
            }
            else ElementOut.SetAttribute("value", "Невозможно выдать такую сумму");

            timer.Stop();
            ElementOut = Doc.GetElementById("Time");
            ElementOut.SetAttribute("value", Convert.ToString(timer.ElapsedMilliseconds));
        }

        /// <summary>
        /// Формирование строки вывода
        /// </summary>
        /// <param name="Issue"></param>
        /// <param name="Values"></param>
        /// <returns></returns>
        static string MakeOutString(int[,] Issue, int[] Values)
        {
            string Out = "Потребуеся: ";

            for (int i = 0; i < Issue.Length/2; i++)
            {
                if (Issue[0,i] != 0)
                Out += $"{Convert.ToString(Issue[0,i])} из кассеты {Convert.ToString(Issue[1, i])}({Convert.ToString(Values[i])}-х) ";
            }
            return Out;
        }

        /// <summary>
        /// Вычисление продолжительности работы
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        static double Duration(DateTime begin, DateTime end)
        {
            TimeSpan Duration = new TimeSpan(end.Ticks-begin.Ticks);
            return Duration.TotalMilliseconds;
        }

        /// <summary>
        /// Прверка возможности выдачи денег
        /// </summary>
        /// <param name="Doc"></param>
        /// <param name="Money"></param>
        /// <returns></returns>
        static bool IsPossible(int Money, int[] Values, int[] Capacity)
        {
            bool IsPossible = false;
            int Sum = 0;

            for (int i = 0; i < Capacity.Length; i++)
            {
                Sum += Capacity[i] * Values[i];
                
                if (Money <= Sum)
                {
                    IsPossible = true;
                    return IsPossible;
                }
            }

            return IsPossible;
        }
        
        /// <summary>
        /// Поиск способа выдачи суммы
        /// </summary>
        /// <param name="Money"></param>
        /// <param name="Values"></param>
        /// <param name="Capacity"></param>
        /// <returns></returns>
        static int[,] HowToIssue(int Money, int[] Values, int[] Capacity, int[] Numbers)
        {
            int[,] Issue = new int[2,Values.Length];
            for (int i = Values.Length-1; i >= 0; i--)
            {
                if (Capacity[i] >= Money / Values[i])
                {
                    if (Money/Values[i] != 0)
                    {
                        Issue[0, i] = Money / Values[i];
                        Issue[1, i] = Numbers[i];
                        Money -= Issue[0, i] * Values[i];
                    }
                }
            }
            if (Money > 0) Issue = null;
            return Issue;
        }

        /// <summary>
        /// Сортировка массивов номиналов и остатков
        /// </summary>
        /// <param name="Values"></param>
        /// <param name="Capacity"></param>
        /// <param name="IsWorking"></param>
        /// <param name="OutValues"></param>
        /// <param name="OutCapacity"></param>
        static int[] Sort(int[] Values, int[] Capacity, bool[] IsWorking, out int[] OutValues, out int[] OutCapacity)
        {
            int[] Numbers = new int[Values.Length]; // массив с исходными номерами

            for (int i = 0; i < Numbers.Length; i++)
            {
                Numbers[i] = i + 1;
            }

            // сортировка выбором
            for (int i = 0; i < Values.Length - 1; i++)
            {
                int Min = i;
                for (int j = i + 1; j < Values.Length; j++)
                {
                    if (Values[j] < Values[Min])
                    {
                        Min = j;
                    }
                }

                SwitchInt(Values, i, Min);

                SwitchInt(Capacity, i, Min);

                SwitchInt(Numbers, i, Min);

                SwitchBool(IsWorking, i, Min);

            }
            
            // массив с номерами, очищенный от неработающих ячеек
            int[] NumbersCut = Clear(Values, Capacity, Numbers, IsWorking, out OutValues, out OutCapacity);

            return NumbersCut;
        }

        /// <summary>
        /// Очистка массивов номиналов и вместимости от неработающих ячеек
        /// </summary>
        /// <param name="Values"></param>
        /// <param name="Capacity"></param>
        /// <param name="IsWorking"></param>
        /// <param name="ClValues"></param>
        /// <param name="ClCapacity"></param>
        static int[] Clear(int[] Values, int[] Capacity, int[] Numbers, bool[] IsWorking, out int[] ClValues, out int[] ClCapacity)
        {
            int[] ClNumbers;
            // число работающих
            int n = 0;
            for (int i = 0; i < IsWorking.Length; i++)
            {
                if (IsWorking[i]) n++;
            }

            if (IsWorking.Length == n)
            {
                ClValues = Values;
                ClCapacity = Capacity;
                return ClNumbers = Numbers;
            }
            else
            {
                ClValues = new int[n];
                ClCapacity = new int[n];
                ClNumbers = new int[n];

                int j = 0;
                for (int i = 0; i < IsWorking.Length; i++)
                {
                    if (IsWorking[i])
                    {
                        ClValues[j] = Values[i];
                        ClCapacity[j] = Capacity[i];
                        ClNumbers[j] = Numbers[i];
                        j++;
                    }
                }
            }

            return ClNumbers;
        }

        #region Switches
        /// <summary>
        /// Меняем местами элементы массива
        /// </summary>
        /// <param name="Mas"></param>
        /// <param name="i"></param>
        /// <param name="Min"></param>
        static void SwitchInt(int[] Mas, int i, int Min)
        {
            int VTemp = Mas[Min];
            Mas[Min] = Mas[i];
            Mas[i] = VTemp;
        }

        /// <summary>
        /// Меняем местами элементы массива
        /// </summary>
        /// <param name="Mas"></param>
        /// <param name="i"></param>
        /// <param name="Min"></param>
        static void SwitchBool(bool[] Mas, int i, int Min)
        {
            bool IWTemp = Mas[Min];
            Mas[Min] = Mas[i];
            Mas[i] = IWTemp;
        }
        #endregion

        #region Get's
        /// <summary>
        /// Считывание количества кассет
        /// </summary>
        /// <param name="Doc"></param>
        /// <returns></returns>
        static int GetCount(HtmlDocument Doc)
        {
            HtmlElement El = Doc.GetElementById("Count");
            int Count = Convert.ToInt32(El.GetAttribute("value"));
            return Count;
        }

        /// <summary>
        /// Считывание номиналов кассет
        /// </summary>
        /// <param name="Doc"></param>
        /// <returns></returns>
        static int[] GetVal(HtmlDocument Doc, int Count)
        {
            int[] Val = new int[Count];
            for (int i = 1; i <= Count; i++)
            {
                HtmlElement El = Doc.GetElementById("Val " + i);
                Val[i - 1] = Convert.ToInt32(El.GetAttribute("value"));
            }

            return Val;
        }

        /// <summary>
        /// Считывание остатка купюр в кассете
        /// </summary>
        /// <param name="Doc"></param>
        /// <returns></returns>
        static int[] GetCapacity(HtmlDocument Doc, int Count)
        {
            int[] Capacity = new int[Count];
            for (int i = 1; i <= Count; i++)
            {
                HtmlElement El = Doc.GetElementById("Capacity " + i);
                Capacity[i - 1] = Convert.ToInt32(El.GetAttribute("value"));
            }

            return Capacity;
        }

        static bool[] GetIsWorking(HtmlDocument Doc, int Count)
        {
            bool[] IsWorking = new bool[Count];
            for (int i = 1; i <= Count; i++)
            {
                HtmlElement El = Doc.GetElementById("IsWorking " + i);
                IsWorking[i - 1] = Convert.ToBoolean(El.GetAttribute("checked"));
            }

            return IsWorking;
        }

        static int GetMoney(HtmlDocument Doc)
        {
            HtmlElement El = Doc.GetElementById("Money");
            int Money = Convert.ToInt32(El.GetAttribute("value"));
            return Money;
        }
        #endregion

    }
}
