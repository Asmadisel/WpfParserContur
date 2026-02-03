using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Linq;
using WpfParserContur.Models;


namespace WpfParserContur
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _inputFilePath;
        private Pay _payData;
        private List<Employee> _employees;

        public MainWindow()
        {
            InitializeComponent();
            ConvertButton.IsEnabled = false;
        }

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

                // Асинхронная загрузка данных
                await LoadXmlDataAsync();
            }
        }

        private async Task LoadXmlDataAsync()
        {
            StatusText.Text = "Загрузка данных...";

            await Task.Run(() =>
            {
                _payData = LoadPayData(_inputFilePath);
            });

            StatusText.Text = "Данные загружены. Нажмите 'Преобразовать' для обработки.";
        }

        private async void ConvertButton_Click(object sender, RoutedEventArgs e) { }

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
        private decimal ParseAmount(string amountStr)
        {
            // Обработка разных десятичных разделителей
            amountStr = amountStr.Replace(',', '.');
            return decimal.TryParse(amountStr, out decimal result) ? result : 0;
        }


    }
}
