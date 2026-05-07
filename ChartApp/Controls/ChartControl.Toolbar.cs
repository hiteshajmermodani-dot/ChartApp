using System.Windows.Input;

namespace ChartApp.Controls
{
    public partial class ChartControl
    {
        private readonly Stack<ViewState> _redoStack = new();

        // Undo/Redo state
        private readonly Stack<ViewState> _undoStack = new();
        private bool _suppressStateCapture;

        private ViewState CaptureViewState()
        {
            return new ViewState(ZoomTransform.ScaleX, ZoomTransform.ScaleY,
                                 ZoomTransform.CenterX, ZoomTransform.CenterY,
                                 PanTransform.X, PanTransform.Y,
                                 _xAxisOffset, _yAxisOffset);
        }

        private void ApplyViewState(ViewState state)
        {
            _suppressStateCapture = true;

            ViewportManager.RestoreState(state.ScaleX, state.ScaleY,
                                         state.CenterX, state.CenterY,
                                         state.PanX, state.PanY,
                                         state.XAxisOffset, state.YAxisOffset);

            SyncTransformsFromManager();

            DrawChart();
            _suppressStateCapture = false;
        }

        private void PushUndoState()
        {
            if (_suppressStateCapture)
            {
                return;
            }

            _undoStack.Push(CaptureViewState());
            _redoStack.Clear();
            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void ClearUndoRedoHistory()
        {
            // Cancel any pending debounced zoom-undo capture so it cannot
            // push a stale state after the history has been cleared.
            _zoomUndoTimer?.Stop();

            _undoStack.Clear();
            _redoStack.Clear();
            UpdateUndoRedoButtons();
        }

        // Execute methods — called by commands (external toolbar, menus, etc.)

        private void ExecuteToggleTracker()
        {
            ShowTrackerLine = !ShowTrackerLine;
            NotifyToolbarStateChanged();
        }

        private void ExecuteToggleLegend()
        {
            ShowLegend = !ShowLegend;
            NotifyToolbarStateChanged();
        }

        private void ExecuteToggleMajorGrid()
        {
            ShowMajorGridLines = !ShowMajorGridLines;
            DrawChart();
            NotifyToolbarStateChanged();
        }

        private void ExecuteToggleMinorGrid()
        {
            ShowMinorGridLines = !ShowMinorGridLines;
            DrawChart();
            NotifyToolbarStateChanged();
        }

        private void ExecuteResetView()
        {
            ResetView();
            ClearUndoRedoHistory();
        }

        private void ExecuteToggleLock()
        {
            IsLocked = !IsLocked;
            NotifyToolbarStateChanged();
        }

        private void ExecuteToggleDoubleClickReset()
        {
            DoubleClickResetEnabled = !DoubleClickResetEnabled;
            NotifyToolbarStateChanged();
        }

        /// <summary>Restores the previous viewport state from the undo stack.</summary>
        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            _redoStack.Push(CaptureViewState());
            ApplyViewState(_undoStack.Pop());
            UpdateUndoRedoButtons();
        }

        /// <summary>Re-applies the last undone viewport state from the redo stack.</summary>
        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            _undoStack.Push(CaptureViewState());
            ApplyViewState(_redoStack.Pop());
            UpdateUndoRedoButtons();
        }

        /// <summary>Immutable snapshot of the chart viewport used for undo/redo history.</summary>
        private record ViewState(
            double ScaleX,
            double ScaleY,
            double CenterX,
            double CenterY,
            double PanX,
            double PanY,
            double XAxisOffset,
            double YAxisOffset);

        /// <summary>
        /// Simple ICommand implementation for chart toolbar commands.
        /// </summary>
        private sealed class ChartCommand(Action execute, Func<bool>? canExecute = null) : ICommand
        {
            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object? parameter)
            {
                return canExecute?.Invoke() ?? true;
            }

            public void Execute(object? parameter)
            {
                execute();
            }
        }

        #region Bindable Commands

        /// <summary>Command to toggle the tracker line on/off.</summary>
        public ICommand ToggleTrackerCommand { get; }

        /// <summary>Command to toggle the legend on/off.</summary>
        public ICommand ToggleLegendCommand { get; }

        /// <summary>Command to toggle major grid lines on/off.</summary>
        public ICommand ToggleMajorGridCommand { get; }

        /// <summary>Command to toggle minor grid lines on/off.</summary>
        public ICommand ToggleMinorGridCommand { get; }

        /// <summary>Command to reset the chart view (zoom, pan, offsets).</summary>
        public ICommand ResetViewCommand { get; }

        /// <summary>Command to undo the last view change.</summary>
        public ICommand UndoCommand { get; }

        /// <summary>Command to redo the last undone view change.</summary>
        public ICommand RedoCommand { get; }

        /// <summary>Command to toggle the lock state on/off.</summary>
        public ICommand ToggleLockCommand { get; }

        /// <summary>Command to toggle double-click reset on/off.</summary>
        public ICommand ToggleDoubleClickResetCommand { get; }

        #endregion
    }
}