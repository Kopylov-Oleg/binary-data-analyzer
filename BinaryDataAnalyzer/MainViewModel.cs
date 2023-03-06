using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Input;

namespace BinaryDataAnalyzer
{
    internal class MainViewModel : ViewModelBase
    {
        private MainModel _model;

        private string _filePath;
        private DataTable _statisticsTable;
        private readonly DelegateCommand _analyzeFileCommand;
        private Random rnd = new Random();

        public MainViewModel() 
        {
            _model = new MainModel();

            _filePath = String.Empty;
            _analyzeFileCommand = new DelegateCommand(OnAnalyzeFile);
            CreateGrid();
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public ICommand AnalyzeFileCommand => _analyzeFileCommand;

        public DataTable StatisticsTable
        {
            get => _statisticsTable;
            set => SetProperty(ref _statisticsTable, value);
        }

        private void OnAnalyzeFile(object commandParameter)
        {            
            FillGrid(_model.AnalyzeFile(FilePath));
        }

        private void CreateGrid()
        {
            var columnNames = new string[]
            {
                "Название кадра",
                "Количество кадров",
                "Ошибок нумерации",
                "Ошибок CRC"
            };

            _statisticsTable = new DataTable();
            for (int i = 0; i < 4; i++)
            {
                _statisticsTable.Columns.Add(columnNames[i]);
            }
            for (int i = 0; i < _model.GetFrameTypesCount() + 1; i++)
            {
                _statisticsTable.Rows.Add();
            }
        }

        private void FillGrid(IEnumerable<FrameTypeStatistics> frameTypesStatistics)
        {
            int totalFramesCount = 0;
            int totalNumberingErrorsCount = 0;
            int totalCrcErrorsCount = 0;

            int i = 0;
            foreach(var frameTypeStatistics in frameTypesStatistics)
            {
                _statisticsTable.Rows[i][0] = frameTypeStatistics.FrameName;
                _statisticsTable.Rows[i][1] = frameTypeStatistics.FramesCount.ToString();
                totalFramesCount += frameTypeStatistics.FramesCount;
                _statisticsTable.Rows[i][2] = frameTypeStatistics.NumberingErrorsCount.ToString();
                totalNumberingErrorsCount += frameTypeStatistics.NumberingErrorsCount;
                _statisticsTable.Rows[i][3] = frameTypeStatistics.CrcErrorsCount.ToString();
                totalCrcErrorsCount += frameTypeStatistics.CrcErrorsCount;
                i++;
            }

            _statisticsTable.Rows[i][0] = "ИТОГО:";
            _statisticsTable.Rows[i][1] = totalFramesCount.ToString();
            _statisticsTable.Rows[i][2] = totalNumberingErrorsCount.ToString();
            _statisticsTable.Rows[i][3] = totalCrcErrorsCount.ToString();
        }
    }
}
