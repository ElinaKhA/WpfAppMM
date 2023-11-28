using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

public class TransportationData
{
    public string AB { get; set; }
    public int B1 { get; set; }
    public int B2 { get; set; }
    public int B3 { get; set; }
    public int B4 { get; set; }
    public int A { get; set; }
}
namespace WpfAppMMMain
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int[,] costMatrix;
        int[] supply;
        int[] demand;
        int rows;
        int cols;
        int[,] initialBasicPlan;
        ObservableCollection<TransportationData> transportationData = new ObservableCollection<TransportationData>();
        ObservableCollection<TransportationData> transportationData2 = new ObservableCollection<TransportationData>();
        ObservableCollection<TransportationData> optimalTransportationData = new ObservableCollection<TransportationData>();
        int[,] plan = { { 60, 0, 15, 0 }, { 0, 55, 15, 35 } };

        public MainWindow()
        {
            InitializeComponent();
        }

      
        private int[,] ReadDataFromTextBox()
        {
            string costMatrixInput = CostMatrixTextBox.Text;
            string[] rows = costMatrixInput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            int numRows = rows.Length;
            int numCols = 4; 

            int[,] costMatrix = new int[numRows, numCols];

            for (int i = 0; i < numRows; i++)
            {
                string[] values = rows[i].Split(' ');
                for (int j = 0; j < numCols; j++)
                {
                    if (j < values.Length)
                    {
                        if (int.TryParse(values[j], out int value))
                        {
                            costMatrix[i, j] = value;
                        }
                        else
                        {
                            MessageBox.Show("Ошибка: Неверный формат данных стоимостей перевозок.");
                            return null;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Ошибка: Недостаточные данные для стоимостей перевозок.");
                        return null;
                    }
                }
            }

            return costMatrix;
        }

        private int[] ReadDataFromTextBox(TextBox textBox)
        {
            string input = textBox.Text;
            string[] values = input.Split(' ');

            int[] data = new int[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                if (int.TryParse(values[i], out int value))
                {
                    data[i] = value;
                }
                else
                {
                    MessageBox.Show("Ошибка: Неверный формат данных.");
                    return new int[0];
                }
            }

            return data;
        }
        public DataTable ConvertArrayToDataTable(int[,] array)
        {
            DataTable dataTable = new DataTable();

            for (int i = 0; i < array.GetLength(0); i++)
            {
                dataTable.Columns.Add($"Col{i + 1}", typeof(int));
            }

            for (int i = 0; i < array.GetLength(1); i++)
            {
                DataRow dataRow = dataTable.NewRow();

                for (int j = 0; j < array.GetLength(0); j++)
                {
                    dataRow[j] = array[j, i];
                }

                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        private void BuildNOP_Click(object sender, RoutedEventArgs e)
        {
            supply = ReadDataFromTextBox(SupplyTextBox);
            demand = ReadDataFromTextBox(DemandTextBox);
            costMatrix = ReadDataFromTextBox();
            NOPGrid.ItemsSource = null;
            rows = costMatrix.GetLength(0);
            cols = costMatrix.GetLength(1);

            initialBasicPlan = new int[rows, cols];
            int[] remainingSupply = new int[rows];
            int[] remainingDemand = new int[cols];

            Array.Copy(supply, remainingSupply, rows);
            Array.Copy(demand, remainingDemand, cols);

            while (true)
            {
                int minCost = int.MaxValue;
                int minI = -1, minJ = -1;

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        if (remainingSupply[i] > 0 && remainingDemand[j] > 0 && costMatrix[i, j] < minCost)
                        {
                            minCost = costMatrix[i, j];
                            minI = i;
                            minJ = j;
                        }
                    }
                }
                if (minI == -1 || minJ == -1)
                {
                    break;
                }
                int minTransport = Math.Min(remainingSupply[minI], remainingDemand[minJ]);
                initialBasicPlan[minI, minJ] = minTransport;
                remainingSupply[minI] -= minTransport;
                remainingDemand[minJ] -= minTransport;
            }



            for (int i = 0; i < rows; i++)
            {
                transportationData2.Add(new TransportationData
                {
                    AB = "A" + (i + 1),
                    B1 = initialBasicPlan[i, 0],
                    B2 = initialBasicPlan[i, 1],
                    B3 = initialBasicPlan[i, 2],
                    B4 = initialBasicPlan[i, 3],
                    A = supply[i] - remainingSupply[i], 
                });
            }

            transportationData2.Add(new TransportationData
            {
                AB = "b",
                B1 = demand[0] - remainingDemand[0],
                B2 = demand[1] - remainingDemand[1],
                B3 = demand[2] - remainingDemand[2],
                B4 = demand[3] - remainingDemand[3],
                A = 0,
            });
            int totalCost = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    totalCost += initialBasicPlan[i, j] * costMatrix[i, j];
                }
            }
            LbNOP.Content = "Суммарная стоимость = " + totalCost.ToString();
            NOPGrid.ItemsSource = transportationData2;


            int[] u, v;
            string optimalityReason;
            bool isOptimal = IsOptimal(costMatrix, initialBasicPlan, out u, out v, out optimalityReason);
            MessageBox.Show($"u = {string.Join(", ", u)}\nv = {string.Join(", ", v)}\n{optimalityReason}");
            LbNopOptNot.Content = isOptimal ? "НОП оптимален" : "НОП не оптимален";

        }
        private bool IsOptimal(int[,] costMatrix, int[,] initialBasicPlan, out int[] u, out int[] v, out string optimalityReason)
        {
            int m = costMatrix.GetLength(0);
            int n = costMatrix.GetLength(1);

            u = new int[m];
            v = new int[n];
            v[0] = 0; 
            for (int k = 0; k < m + n - 1; k++)
            {
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (initialBasicPlan[i, j] > 0)
                        {
                            if (v[j] == 0)
                            {
                                v[j] = costMatrix[i, j] + u[i];
                            }
                            else if (u[i] == 0)
                            {
                                u[i] = v[j] - costMatrix[i, j];
                            }
                        }
                    }
                }
            }

            optimalityReason = null;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (initialBasicPlan[i, j] == 0)
                    {
                        int reducedCost = v[j] - u[i] - costMatrix[i, j];

                        ocLb.Text += $"Оценка = {reducedCost} в c{ i + 1}{ j + 1}\n";
                        if (reducedCost > 0)
                        {
                            optimalityReason = $"НОП не оптимален. Элемент с положительной оценкой = ({reducedCost}) в c{i + 1}{j + 1}";
                            break;
                        }
                    }
                }
                if (optimalityReason != null)
                {
                    break;
                }
            }
            return optimalityReason == null;
        }

        private void BuildOPT_Click(object sender, RoutedEventArgs e)
        {
            ocLb.Text = "";
            int[] u, v;
            string optimalityReason;
            bool isOptimal = IsOptimal(costMatrix, initialBasicPlan, out u, out v, out optimalityReason);

            if (!isOptimal)
            {
                DisplayOptimizedPlan(plan);
            }
            else
            {
                MessageBox.Show("НОП уже является оптимальным");
            }
        }

        private void DisplayOptimizedPlan(int[,] plan)
        {
            OPTGrid.ItemsSource = null;

            ObservableCollection<TransportationData> optimizedTransportationData = new ObservableCollection<TransportationData>();
            for (int i = 0; i < plan.GetLength(0); i++)
            {
                optimizedTransportationData.Add(new TransportationData
                {
                    AB = "A" + (i + 1),
                    B1 = plan[i, 0],
                    B2 = plan[i, 1],
                    B3 = plan[i, 2],
                    B4 = plan[i, 3],
                    A = supply[i],
                });
            }
            optimizedTransportationData.Add(new TransportationData
            {
                AB = "b",
                B1 = demand[0],
                B2 = demand[1],
                B3 = demand[2],
                B4 = demand[3],
                A = 0,
            });

            OPTGrid.ItemsSource = optimizedTransportationData;
            int totalCost = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    totalCost += plan[i, j] * costMatrix[i, j];
                }
            }
            LbOPT.Content = "Суммарная стоимость = " + totalCost.ToString();
            int[] u, v;
            string optimalityReason;
            bool isOptimal = IsOptimal(costMatrix, plan, out u, out v, out optimalityReason);
            MessageBox.Show($"u = {string.Join(", ", u)}\nv = {string.Join(", ", v)}\n{optimalityReason}");
        }
    }
}