using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml.Xsl;
using WpfParserContur.Models;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;


namespace WpfParserContur
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _inputFilePath;
        private string _outputFolderPath;
        private Pay _payData;
        private List<Employee> _employees;

        /// <summary>
        /// Инициализация основного окна.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ConvertButton.IsEnabled = false;
            AddButton.IsEnabled = false;
        }

        /// <summary>
        /// Выбор папки для сохранения результатов
        /// </summary>
        private void SelectOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку для сохранения преобразования";
                dialog.SelectedPath = _outputFolderPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _outputFolderPath = dialog.SelectedPath;
                    OutputFolderTextBox.Text = _outputFolderPath;
                }
            }
        }

        /// <summary>
        /// Обработчик загрузки файла.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "XML файлы (*.xml)|*.xml",
                Multiselect = true,
                Title = "Выберите XML-файл с зарплатными данными"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _inputFilePath = openFileDialog.FileName;
                ConvertButton.IsEnabled = true;
                StatusText.Text = "Файл загружен: " + System.IO.Path.GetFileName(_inputFilePath);

                await LoadXmlDataAsync();

                ConvertButton.IsEnabled = true;
                AddButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Загружает данные из исходного файла.
        /// </summary>
        /// <returns></returns>
        private async Task LoadXmlDataAsync()
        {
            try
            {
                StatusText.Text = "Загрузка данных...";

                await Task.Run(() =>
                {
                    _payData = LoadPayData(_inputFilePath);
                });

                //Синхронно обновляем.
                UpdateTables();

            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка загрузки";
            }
        }

        private void UpdateTables()
        {
            Dispatcher.Invoke(() =>
            {
                DisplayDataInUI();
                StatusText.Text = "Данные загружены. Нажмите 'Преобразовать' для обработки.";
            });
        }

        /// <summary>
        /// Отображает загруженные данные после парсинга.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void DisplayDataInUI()
        {
            var employees = _payData.Items
                .GroupBy(item => new { item.Name, item.Surname })
                .Select(g => new
                {
                    Name = g.Key.Name,
                    Surname = g.Key.Surname,
                    Total = g.Sum(i => i.Amount)
                })
                .ToList();

            EmployeesGrid.ItemsSource = employees;


            var monthlySums = _payData.Items
            .GroupBy(i => i.Mount)
            .Select(g => new
            {
                Month = g.Key,
                Total = g.Sum(i => i.Amount)
            })
            .ToList();

            MonthlySumsGrid.ItemsSource = monthlySums;
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Преобразовать"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (_outputFolderPath  == null) 
            {
                MessageBox.Show("пожалуйста, выберите директорию для сохранения файла");
                return;
            }

            try
            {
                ConvertButton.IsEnabled = false;
                StatusText.Text = "Запускаем преобразование";

                await Task.Run(() =>
                {
                    string employeesPath = System.IO.Path.Combine(
                 _outputFolderPath,
                 $"Employees_{DateTime.Now:yyyyMMdd_HHmmss}.xml"
                 );

                    TransformXml(_inputFilePath, employeesPath);

                    AddTotals(employeesPath);

                    AddTotalsToInputFile(_inputFilePath);
                });

                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка преобразования: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Ошибка преобразования";
            }
            finally
            {
                ConvertButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Сохраняет в исходный файл total в корень по всем сотрудникам.
        /// </summary>
        /// <param name="inputFilePath"></param>
        private void AddTotalsToInputFile(string inputFilePath)
        {
            var doc = XDocument.Load(inputFilePath);

            

            if (_payData != null 
                || !_payData.Items.Any()) 
            {
                var total = _payData.Items.Sum(i => i.Amount);
                if (doc.Root.Attribute("total") == null 
                    || doc.Root.Attribute("total").ToString() != total.ToString())
                {
                    doc.Root.SetAttributeValue("total", total.ToString());
                }
            }

            doc.Save(inputFilePath);

            
        }

        /// <summary>
        /// Считает общую сумму по каждому сотруднику и записывает её в output-документ. 
        /// </summary>
        /// <param name="employeesPath"></param>
        private void AddTotals(string employeesPath)
        {
            var doc = XDocument.Load(employeesPath);

            foreach(var emp in doc.Root.Elements("Employee"))
            {
                decimal total = 0;

                foreach(var item in emp.Elements("salary"))
                {
                    var amount = item.Attribute("amount")?.Value;
                    total += ParseAmount(amount);
                }

                emp.SetAttributeValue("total", total.ToString());
            }

            doc.Save(employeesPath);
        }

        /// <summary>
        /// Загружает коллекцию данных Pay из документа.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private Pay LoadPayData(string filePath)
        {
            var pay = new Pay();

            var doc = XDocument.Load(filePath);

            foreach (var item in doc.Root.Descendants("item"))
            {
                pay.Items.Add(new Item
                {
                    Name = (string)item.Attribute("name"),
                    Surname = (string)item.Attribute("surname"),
                    Amount = ParseAmount((string)item.Attribute("amount")),
                    Mount = (string)item.Attribute("mount")
                });
            }

            return pay;
        }

        /// <summary>
        /// Парсит десятичные числа, независимо от локали (точка и запятая)
        /// </summary>
        /// <param name="amountStr"></param>
        /// <returns></returns>
        private decimal ParseAmount(string amountStr)
        {
            // Обработка разных десятичных разделителей. Приводим к русской локали (запятая)
            amountStr = amountStr.Replace('.', ',');
            return decimal.TryParse(amountStr, out decimal result) ? result : 0;
        }

        /// <summary>
        /// Преобразует (создаёт новый) файл типа Employees.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        private void TransformXml(string input, string output)
        {
            try
            {
                string xsltPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "TransformScaffolds",
                    "transformEmployee.xslt"
                );

                var transform = new XslCompiledTransform();
                transform.Load(xsltPath);
                transform.Transform(input, output);

                MessageBox.Show("Файл успешно преобразован");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка XSLT-преобразования: {ex.Message}");
                StatusText.Text = "Ошибка поиска файла преобразования";
            }
        }

        /// <summary>
        /// Обработчик кнопки доабвления новой записи в файл.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.Text != string.Empty 
                && SurnameTextBox.Text != string.Empty
                && AmountTextBox.Text != string.Empty
                && MountTextBox.Text != string.Empty)
            {
                if (!decimal.TryParse(AmountTextBox.Text.Replace(',', '.'),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out _))
                {
                    MessageBox.Show("Некорректная сумма!");
                    return;
                }

                await AddNewItemAsync();

                _payData = LoadPayData(_inputFilePath);
                //Синхронно обновляем.
                UpdateTables();

            }

        }

        /// <summary>
        /// асинхронно добавляет новый элемент в файл.
        /// </summary>
        /// <returns></returns>
        private async Task AddNewItemAsync()
        {
            try
            {
                var doc = XDocument.Load(_inputFilePath);

                var newItem = new XElement("item",
                   new XAttribute("name", NameTextBox.Text.Trim()),
                   new XAttribute("surname", SurnameTextBox.Text.Trim()),
                   new XAttribute("amount", AmountTextBox.Text.Trim()),
                   new XAttribute("mount", MountTextBox.Text.Trim().ToLower()));

                doc.Root.Add(newItem);
                doc.Save(_inputFilePath);
            }
            catch (Exception ex) 
            {
                MessageBox.Show("Ошибка при добавлении:" + ex.Message);
            }
        }
    }
}
