﻿using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Function.Snip;
using Function.Util;
using WPFCustomMessageBox;
using FormsControl = System.Windows.Forms.Control;

namespace SuperSnipper {
    /// <summary>
    /// Interaction logic for ScreenshotTaker.xaml
    /// </summary>
    public partial class ScreenshotTaker : Window {

        public Action<Snip> Enqueue;

        private readonly Timer _timer;
        private readonly Snip _snip;
        private readonly Border _center;

        private BoxF _cropRegion;

        public ScreenshotTaker(Snip snip) {
            InitializeComponent();

            for (var r = 0; r < MainGrid.RowDefinitions.Count; ++r) {
                for (var c = 0; c < MainGrid.ColumnDefinitions.Count; ++c) {
                    var border = new Border {
                        Opacity = 0.5,
                        Background = new SolidColorBrush(Colors.White),
                        BorderBrush = new SolidColorBrush(Colors.Red),
                        BorderThickness = new Thickness(0)
                    };
                    border.SetValue(Grid.RowProperty, r);
                    border.SetValue(Grid.ColumnProperty, c);
                    
                    if (r == 1 && c == 1)
                        _center = border;

                    MainGrid.Children.Add(border);
                }
            }

            _snip = snip;

            BackgroundImage.Source = _snip.BitmapImageScreenshot;

            MainGrid.Cursor = Cursors.Cross;

            _timer = new Timer(1) {
                Enabled = false,
                AutoReset = true,
            };
            _timer.Elapsed += UpdateRect;

            WindowState = WindowState.Maximized;

            Activate();
        }

        /// <summary>
        /// For some reason the box is drawn 8 pixels too far up and left;
        /// this variable accounts for that error in placement.
        /// </summary>
        private const int TopLeftOffset = 8;

        private void UpdateRect(object sender, ElapsedEventArgs e) {
            Application.Current.Dispatcher.Invoke(
                delegate {
                    var mousePos = FormsControl.MousePosition;

                    _cropRegion.X2 = mousePos.X;
                    _cropRegion.Y2 = mousePos.Y;

                    Row0.SetValue(RowDefinition.HeightProperty, new GridLength(_cropRegion.TopLeft.Y + TopLeftOffset, GridUnitType.Pixel));
                    Row1.SetValue(RowDefinition.HeightProperty, new GridLength(_cropRegion.BottomRight.Y - _cropRegion.TopLeft.Y, GridUnitType.Pixel));
                    Row2.SetValue(RowDefinition.HeightProperty, new GridLength(1, GridUnitType.Star));
                    Col0.SetValue(ColumnDefinition.WidthProperty, new GridLength(_cropRegion.TopLeft.X + TopLeftOffset, GridUnitType.Pixel));
                    Col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(_cropRegion.BottomRight.X - _cropRegion.TopLeft.X, GridUnitType.Pixel));
                    Col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
                }
            );
        }

        private void StartDrawRect(object sender, MouseButtonEventArgs e) {
            _center.BorderThickness = new Thickness(1);
            _center.Opacity = 1;
            _center.Background = new SolidColorBrush(Colors.Transparent);

            var mousePos = e.GetPosition(this);

            _cropRegion.X1 = (float) mousePos.X;
            _cropRegion.Y1 = (float) mousePos.Y;
            _timer.Enabled = true;
        }

        private void StopDrawRect(object sender, MouseButtonEventArgs e) {
            _timer.Enabled = false;

            if (_cropRegion.Width > 0 && _cropRegion.Height > 0) {
                _snip.Crop((Box) _cropRegion);
                Enqueue(_snip);

                Close();
            } else {
                Close();

                if (_cropRegion.Width <= 0)
                    CustomMessageBox.ShowOK("Your snip had a width of 0, so it was not taken.", "Error", "OK");
                else // height is <= 0
                    CustomMessageBox.ShowOK("Your snip had a height of 0, so it was not taken.", "Error", "OK");
            }
        }
    }
}
