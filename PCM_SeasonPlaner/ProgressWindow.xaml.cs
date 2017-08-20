using System.ComponentModel;
using System.Windows;
using System;

namespace PCM_SeasonPlaner
{
   /// <summary>
   /// Interaktionslogik für ProgressWindow.xaml
   /// </summary>
   public partial class ProgressWindow : Window, INotifyPropertyChanged
   {
      private IProgressOperation _operation;

      public ProgressWindow()
      {
         InitializeComponent();
      }

      public void StartLongJob(string strJob, string strPath)
      {
         _operation = new DBLoader();
         _operation.ProgressStatusChanged += new EventHandler(Operation_ProgressStatusChanged);
         _operation.ProgressCurrentChanged += new EventHandler(Operation_ProgressCurrentChanged);
         _operation.ProgressMaximumChanged += new EventHandler(Operation_ProgressMaximumChanged);
         _operation.ProgressCompleted += new EventHandler(Operation_ProgressCompleted);
         _operation.ProgressStart(strJob, strPath);
         this.ShowDialog();
      }

      private void Operation_ProgressStatusChanged(object sender, EventArgs e)
      {
         this.OnPropertyChanged("Status");
      }

      private void Operation_ProgressCurrentChanged(object sender, EventArgs e)
      {
         this.OnPropertyChanged("Current");
      }

      private void Operation_ProgressMaximumChanged(object sender, EventArgs e)
      {
         this.OnPropertyChanged("Maximum");
      }

      private void Operation_ProgressCompleted(object sender, EventArgs e)
      {
         this.Close();
         //_operation.ProgressStatusChanged -= new EventHandler(Operation_ProgressStatusChanged);
         //_operation.ProgressCurrentChanged -= new EventHandler(Operation_ProgressCurrentChanged);
         //_operation.ProgressMaximumChanged -= new EventHandler(Operation_ProgressMaximumChanged);
         //_operation.ProgressCompleted -= new EventHandler(Operation_ProgressCompleted);
         //_operation = null;
         //_blnOperationIsRunning = false;
         //this.sbStatusInfo.Content = string.Empty;
         //this.sbProgress.Visibility = Visibility.Hidden;
         //this.sbProgress.Value = 0;
      }

      private void CancelClick(object sender, RoutedEventArgs e)
      {
         _operation.ProgressCancelAsync();
      }

      public string Status
      {
         get { return _operation.ProgressStatus; }
      }

      public int Current
      {
         get { return _operation.ProgressCurrent; }
      }

      public int Maximum
      {
         get { return _operation.ProgressMaximum; }
      }

      #region INotifyPropertyChanged Members

      /// <summary>
      /// Notify property changed
      /// </summary>
      /// <param name="propertyName">Property name</param>
      protected void OnPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }

      public event PropertyChangedEventHandler PropertyChanged;

      #endregion
   }
}
