﻿using System;
using System.Windows;
using System.Timers;
using SDRVisualizer.Helpers;
using Timer = System.Timers.Timer;

namespace SDRVisualizer
{
    public partial class MainWindow : Window
    {
        private Timer updateTimer;
        private FFTDataGenerator _fftDataGenerator;

        public MainWindow()
        {
            _fftDataGenerator = new FFTDataGenerator();

            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            updateTimer = new Timer(50); // Update every 50ms (~20 FPS)
            updateTimer.Elapsed += (s, e) => Dispatcher.Invoke(UpdateVisualization);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            updateTimer.Start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            updateTimer.Stop();
        }

        private void UpdateVisualization()
        {
            List<int> newData = _fftDataGenerator.GetRow();
            SpectrumControl.UpdateSpectrumData(newData);
            WaterfallControl.AddSpectrumSnapshot(newData);
        }
    }

}