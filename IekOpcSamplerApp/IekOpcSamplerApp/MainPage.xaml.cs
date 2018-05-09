﻿using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IekOpcSamplerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region "View"
        double ThisStep { get; set; }
        double LastStep { get; set; }
        double LimSup = 5;
        double LimInf = -5;

        ObservableCollection<Models.Point> MainLineCollection = new ObservableCollection<Models.Point>();
        ObservableCollection<Models.Point> UpperBoundCollection = new ObservableCollection<Models.Point>();
        ObservableCollection<Models.Point> LowerBoundCollection = new ObservableCollection<Models.Point>();
        ObservableCollection<Models.Point> MainLineCollectionH = new ObservableCollection<Models.Point>();

        DispatcherTimer timer = new DispatcherTimer();
        int x = 0;
        Line line;
        #endregion

        #region "Services"
        Services.OpcSocketServer _OpcClient = new Services.OpcSocketServer();
        Services.DatabaseService _DBClient = new Services.DatabaseService();
        #endregion

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            MainLineSeries.DataContext = MainLineCollection;
            UpperBoundSeries.DataContext = UpperBoundCollection;
            LowerBoundSeries.DataContext = LowerBoundCollection;
            LineChartH.DataContext = MainLineCollectionH;

            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += (object sender, object e) =>
            {
                if (MainLineCollection.Count == 100)
                {
                    MainLineCollection.Add(MainLineCollection[0]);
                    MainLineCollection.RemoveAt(0);
                }
                x++;
                MainLineCollection.Add(new Models.Point(x, Math.Sin(x)));
                UpperBoundCollection.Add(new Models.Point(x, LimSup));
                LowerBoundCollection.Add(new Models.Point(x, LimInf));

                if (MainLineCollection.Count > 0)
                {
                    labelActual.Text = Math.Sin(x).ToString("00.000");
                    labelPromedio.Text = MainLineCollection.Average(x => x.Y).ToString("00.000");
                }

                //if (Grid1.Children.Contains(line))
                //{
                //    Grid1.Children.Remove(line);
                //}

                //var axisy = LineChart.ActualAxes[1] as LinearAxis;
                //var linearaximaximum = Convert.ToDouble(axisy.ActualMaximum);
                //var linearaximinimum = Convert.ToDouble(axisy.ActualMinimum);
                //double perinterval = Convert.ToDouble(axisy.ActualHeight / (linearaximaximum - linearaximinimum));

                //var lineY = perinterval * (0.8 - linearaximinimum);

                //var ttv = MainLineSeries.TransformToVisual(Window.Current.Content);
                //Point screenCoords = ttv.TransformPoint(new Point(0, 0));

                //line = new Line();
                //line.X1 = screenCoords.X;
                //line.X2 = LineChart.ActualWidth;
                //line.Y1 = axisy.ActualHeight - lineY;
                //line.Y2 = axisy.ActualHeight - lineY;
                //line.Stroke = new SolidColorBrush(Colors.Gray);
                //line.StrokeThickness = 1;
                //Grid1.Children.Add(line);
            };

            _OpcClient.ConnectionStatusChanged += _OpcClient_ConnectionStatusChanged;
            _OpcClient.TagValueChanged += _OpcClient_TagValueChanged;

            StartServices();

            limSupNum.Text = LimSup.ToString();
            limInfNum.Text = LimInf.ToString();
            labelLimites.Text = "[" + LimSup + "," + LimInf + "]";

            timer.Start();

        }

        ~MainPage()
        {
        }

        private async void StartServices()
        {
            OpcStatus.Text = "CONNECTING";
            try
            {
                await _OpcClient.ConnectAsync();
                await _OpcClient.SendAsync("HOLA");
            }
            catch (Exception)
            {
            }
        }

        #region Events

        private void _OpcClient_ConnectionStatusChanged(object sender, Enums.OpcSocketClientStatus status)
        {
            switch (status)
            {
                case Enums.OpcSocketClientStatus.Good:
                    OpcStatus.Text = "ONLINE";
                    break;
                case Enums.OpcSocketClientStatus.Bad:
                    OpcStatus.Text = "FAILED";
                    break;
                default:
                    break;
            }
        }

        private async void _OpcClient_TagValueChanged(object sender, Models.Tag tag)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    labelActual.Text = double.Parse(tag.Value).ToString("00.000");
                    if (MainLineCollection.Count == 100)
                    {
                        MainLineCollectionH.Add(MainLineCollection[0]);
                        MainLineCollection.RemoveAt(0);
                        UpperBoundCollection.RemoveAt(0);
                        LowerBoundCollection.RemoveAt(0);
                    }
                    MainLineCollection.Add(new Models.Point(x, double.Parse(tag.Value)));
                    UpperBoundCollection.Add(new Models.Point(x, LimSup));
                    LowerBoundCollection.Add(new Models.Point(x, LimInf));
                    x++;
                    labelPromedio.Text = MainLineCollection.Average(x => x.Y).ToString("00.000");
                });
        }
        #endregion

        private void Limit_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter)
            {
                return;
            }
            double.TryParse(limSupNum.Text, out LimSup);
            double.TryParse(limInfNum.Text, out LimInf);

            limSupNum.Text = LimSup.ToString();
            limInfNum.Text = LimInf.ToString();

            for (var i = 0; i < UpperBoundCollection.Count; i++)
            {
                UpperBoundCollection[i] = new Models.Point(UpperBoundCollection[i].X, LimSup);
            }
            for (var i = 0; i < LowerBoundCollection.Count; i++)
            {
                LowerBoundCollection[i] = new Models.Point(LowerBoundCollection[i].X, LimInf);
            }
            labelLimites.Text = "[" + LimSup + "," + LimInf + "]";
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButton.Icon = PlayButton.Label == "Iniciar" ? new SymbolIcon(Symbol.Pause) : new SymbolIcon(Symbol.Play);
            PlayButton.Label = PlayButton.Label == "Iniciar" ? "Pausar" : "Iniciar";
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    //try
                    //{
                    //    await _OpcClient.SendAsync(
                    //        JsonConvert.SerializeObject(new
                    //        {
                    //            handle = 1,
                    //            name = "B_BERTHA",
                    //            value = 1
                    //        }));
                    //    await Task.Delay(500);
                    //    await _OpcClient.SendAsync(
                    //        JsonConvert.SerializeObject(new
                    //        {
                    //            handle = 1,
                    //            name = "B_BERTHA",
                    //            value = 0
                    //        }));
                    //}
                    //catch (Exception ex)
                    //{
                    //    throw;
                    //}
                    MainLineCollection.Clear();
                });
        }

        private void SaveOrderButton_Click(object sender, RoutedEventArgs e)
        {
            flyoutEdit.Hide();
            labelOrderSku.Text = tbOrderSku.Text;
            labelOrderCustName.Text = tbOrderCliente.Text;
            labelOrderArea.Text = tbOrderArea.Text;
            labelOrderAdhesivo.Text = tbOrderAdhesivo.Text;
            labelOrderLongitud.Text = tbOrderLongitud.Text;
            labelOrderObservaciones.Text = tbOrderObservaciones.Text;
            labelOrderOperador.Text = tbOrderOperador.Text;
            labelOrderTurno.Text = ((ComboBoxItem)tbOrderTurno.SelectedItem)?.Content?.ToString() ?? "";
        }

    }
}
