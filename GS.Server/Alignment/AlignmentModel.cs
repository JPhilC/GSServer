using ASCOM.DeviceInterface;
using GS.Server.SkyTelescope;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace GS.Server.Alignment
{
    public enum NotificationType
    {
        Information,
        Data,
        Warning,
        Error,
        Debug
    }


    public class NotificationEventArgs : EventArgs
    {
        public NotificationType NotificationType { get; set; }
        public string Method { get; set; }

        public int Thread { get; set; }
        public string Message { get; set; }


        public NotificationEventArgs(NotificationType notificationType, string method, string message)
        {
            this.NotificationType = notificationType;
            this.Method = method;
            this.Message = message;
            this.Thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }
    }

    public sealed class AlignmentModel
    {
        #region Events ...

        public event EventHandler<NotificationEventArgs> Notification = delegate { };

        private void RaiseNotification(NotificationType notificationType, string method, string message)
        {
            Volatile.Read(ref Notification).Invoke(this, new NotificationEventArgs(notificationType, method, message));
        }

        #endregion

        public bool IsAlignmentOn { get; set; }

        public AlignmentModes AlignmentMode { get; }

        public IPointingModel Model { get; private set; }

        public AlignmentPointCollection Points { get; }

        public bool IsFitted => Model?.IsFitted ?? false;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly object _accessLock = new object();

        private readonly string _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AlignmentModel\Points.config");

        private readonly string _timeStampFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AlignmentModel\TimeStamp.config");

        private readonly List<string> _exceptionMessages = new List<string>();

        public AlignmentModel(AlignmentModes alignmentMode)
        {
            AlignmentMode = alignmentMode;
            Points = new AlignmentPointCollection();

            ResetModel();

        }

        /// <summary>
        /// Loads previously saved alignment points if appropriate.
        /// Points are loaded when:
        ///   - the user has not requested clearing them on startup, OR
        /// After loading, the model is fitted if enough points exist.
        /// </summary>
        public void Initialise(bool clearPointsOnStartup)
        {
            // 1. If the user did NOT request clearing points, load them
            if (!clearPointsOnStartup)
            {
                Load();   // loads points from _configFile
                Fit();    // fit if >= 3 points
            }
        }

        /// <summary>
        /// Add a new alignment point and refit the model.
        /// </summary>
        public void AddPoint(AlignmentPoint p)
        {
            Points.Add(p);
            Fit();
        }

        /// <summary>
        /// Refit the pointing model using all alignment points.
        /// </summary>
        public void Fit()
        {
            if (Points.Count < 4)
                return; // Not enough points yet

            Model.Fit(Points);
        }


        public AxisPosition Apply(AxisPosition ideal, double hourAngle)
        {
            if (!IsFitted)
                return ideal;

            return Model.Apply(ideal, hourAngle);
        }

        public AxisPosition ApplyReverse(AxisPosition corrected, double hourAngle)
        {
            if (!IsFitted)
                return corrected;

            return Model.ApplyReverse(corrected, hourAngle);
        }


        /// <summary>
        /// Removes an alignment point from the model and refits the pointing model.
        /// If fewer than three points remain, the model is reset.
        /// </summary>
        /// <param name="pointToDelete">The alignment point to remove.</param>
        /// <returns>
        /// True if the point was removed; false if it was not found or an error occurred.
        /// </returns>
        public bool RemovePoint(AlignmentPoint pointToDelete)
        {
            try
            {
                bool removed = Points.Remove(pointToDelete);

                if (removed)
                {
                    // If fewer than 3 points remain, reset the model
                    if (Points.Count < 4)
                    {
                        // Reset the model instance
                        ResetModel();
                    }
                    else
                    {
                        // Refit the model with the remaining points
                        Fit();
                    }

                    // Persist the updated point list
                    Save();
                }

                return removed;
            }
            catch (Exception ex)
            {
                LogException(ex, true);
                return false;
            }
        }


        /// <summary>
        /// Clear all alignment points and reset the model.
        /// </summary>
        public void Clear()
        {
            Points.Clear();

            ResetModel();

        }



        public void Save()
        {
            var dir = Path.GetDirectoryName(_configFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            Save(_configFile);
            ReportAlignmentPoints();
        }


        /// <summary>
        /// Optional: save model parameters + points to disk.
        /// </summary>
        public void Save(string path)
        {
            // Serialize Points + Model parameters
            File.WriteAllText(path, JsonConvert.SerializeObject(Points, Formatting.Indented));
        }


        private void Load()
        {
            var dir = Path.GetDirectoryName(_configFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(_configFile))
            {
                Load(_configFile);
            }
            ReportAlignmentPoints();
        }

        /// <summary>
        /// Optional: load model parameters + points from disk.
        /// </summary>
        public void Load(string path)
        {
            // Deserialize Points + Model parameters
            Points.Clear();
            using (var file = File.OpenText(path))
            {
                var serializer = new JsonSerializer();
                try
                {
                    var loaded =
                        (AlignmentPointCollection)serializer.Deserialize(file, typeof(AlignmentPointCollection));
                    if (loaded != null)
                    {
                        foreach (var alignmentPoint in loaded)
                        {
                            Points.Add(alignmentPoint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex);
                }
            }

            // Fit the model
            Fit();
        }



        public void ClearAlignmentPoints()
        {
            try
            {
                Points.Clear();
                Save();
            }
            catch (Exception ex)
            {
                LogException(ex, true);
            }
        }

        private void ResetModel()
        {
            switch (AlignmentMode)
            {
                case AlignmentModes.algGermanPolar:
                case AlignmentModes.algPolar:
                    Model = new LinearEquatorialPointingModel();
                    break;

                case AlignmentModes.algAltAz:
                    Model = new LinearAltAzPointingModel();
                    break;
            }
        }

        #region Helper methods ...
        private void ReportAlignmentPoints()
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendLine("=============== Alignment points ===============");
            _stringBuilder.AppendLine("ID \tUnsynced A1/A2         \tIdeal A1/A2   \tObserved time");
            foreach (var pt in Points)
            {
                _stringBuilder.AppendLine(
                    $"{pt.Id:D3}\t{pt.Unsynced.A1}/{pt.Unsynced.A2}\t{pt.Ideal.A1}/{pt.Ideal.A2}\t{pt.AlignTime}");
            }
            RaiseNotification(NotificationType.Data, MethodBase.GetCurrentMethod()?.Name, _stringBuilder.ToString());
            _stringBuilder.Clear();
        }

        private void LogException(Exception ex, bool allowDuplicates = false)
        {
            if (allowDuplicates || !_exceptionMessages.Contains(ex.Message))
            {
                _exceptionMessages.Add(ex.Message);
                string message = $"{ex.Message}|{ex.StackTrace}";
                RaiseNotification(NotificationType.Error, MethodBase.GetCurrentMethod()?.Name, message);
            }
        }

        #endregion

    }

}
